//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Apis.CloudOSLogin.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    public interface IOsLoginService : IDisposable
    {
        /// <summary>
        /// Upload an a public key to authorize it.
        /// </summary>
        Task<AuthorizedKeyPair> AuthorizeKeyPairAsync(
            ProjectLocator project,
            OsLoginSystemType os,
            ISshKeyPair key,
            TimeSpan validity,
            CancellationToken token);

        /// <summary>
        /// List existing authorized keys.
        /// </summary>
        Task<IEnumerable<IAuthorizedPublicKey>> ListAuthorizedKeysAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Delete authorized key.
        /// </summary>
        Task DeleteAuthorizedKeyAsync(
            IAuthorizedPublicKey key,
            CancellationToken cancellationToken);
    }

    public enum OsLoginSystemType
    {
        Linux
    }

    [Service(typeof(IOsLoginService))]
    public sealed class OsLoginService : IOsLoginService
    {
        private static readonly ProjectLocator WellKnownProject 
            = new ProjectLocator("windows-cloud");
        
        private readonly IOsLoginAdapter adapter;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal OsLoginService(IOsLoginAdapter adapter)
        {
            this.adapter = adapter;
        }

        public OsLoginService(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IOsLoginAdapter>())
        {
        }

        //---------------------------------------------------------------------
        // IOsLoginService.
        //---------------------------------------------------------------------

        public async Task<AuthorizedKeyPair> AuthorizeKeyPairAsync(
            ProjectLocator project,
            OsLoginSystemType os,
            ISshKeyPair key,
            TimeSpan validity,
            CancellationToken token)
        {
            Utilities.ThrowIfNull(project, nameof(project));
            Utilities.ThrowIfNull(key, nameof(key));

            if (os != OsLoginSystemType.Linux)
            {
                throw new ArgumentException(nameof(os));
            }

            if (validity.TotalSeconds <= 0)
            {
                throw new ArgumentException(nameof(validity));
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                //
                // If OS Login is enabled for a project, we have to use
                // the Posix username from the OS Login login profile.
                //
                // Note that the Posix account managed by OS login can 
                // differ based on the project that we're trying to access.
                // Therefore, make sure to specify the project when
                // importing the key.
                //
                // OS Login auto-generates a username for us. Again, this
                // username might differ based on project/organization.
                //

                //
                // Import the key for the given project.
                //

                var loginProfile = await this.adapter.ImportSshPublicKeyAsync(
                        project,
                        key,
                        validity,
                        token)
                    .ConfigureAwait(false);

                //
                // Although rare, there could be multiple POSIX accounts.
                //
                var account = loginProfile.PosixAccounts
                    .EnsureNotNull()
                    .FirstOrDefault(a => a.Primary == true &&
                                            a.OperatingSystemType == "LINUX");

                if (account == null)
                {
                    // 
                    // This is strange, the account should have been created.
                    //
                    throw new OsLoginSshKeyImportFailedException(
                        "Imported SSH key to OSLogin, but no POSIX account was created",
                        HelpTopics.TroubleshootingOsLogin);
                }

                return AuthorizedKeyPair.ForOsLoginAccount(key, account);
            }
        }

        public async Task<IEnumerable<IAuthorizedPublicKey>> ListAuthorizedKeysAsync(
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                //
                // NB. The OS Login profile (in particular, the username
                // and UID/GID) depends on the project, and the organization
                // it resides in. However, the SSH public keys are independent
                // of that -- therefore, we can query the list of public keys
                // using any project.
                //
                // 
                var loginProfile = await this.adapter
                    .GetLoginProfileAsync(WellKnownProject, cancellationToken)
                    .ConfigureAwait(false);

                return loginProfile
                    .SshPublicKeys
                    .EnsureNotNull()
                    .Select(k => AuthorizedPublicKey.TryParse(k.Value))
                    .Where(k => k != null)
                    .ToList();
            }
        }

        public async Task DeleteAuthorizedKeyAsync(
            IAuthorizedPublicKey key,
            CancellationToken cancellationToken)
        {
            Debug.Assert(key is AuthorizedPublicKey);
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(key))
            {
                await this.adapter.DeleteSshPublicKey(
                        ((AuthorizedPublicKey)key).Fingerprint,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            this.adapter?.Dispose();
        }

        internal class AuthorizedPublicKey : IAuthorizedPublicKey
        {
            internal string Fingerprint { get; }

            public string Email { get; }

            public string KeyType { get; }

            public string PublicKey { get; }

            public DateTime? ExpireOn { get; }

            private AuthorizedPublicKey(
                string fingerprint,
                string email,
                string keyType,
                string publicKey,
                DateTime? expiresOn)
            {
                this.Fingerprint = fingerprint.ThrowIfNull(nameof(fingerprint));
                this.Email = email.ThrowIfNullOrEmpty(nameof(email));
                this.KeyType = keyType.ThrowIfNullOrEmpty(nameof(keyType));
                this.PublicKey = publicKey.ThrowIfNullOrEmpty(nameof(publicKey));
                this.ExpireOn = expiresOn;
            }

            public static AuthorizedPublicKey TryParse(
                SshPublicKey osLoginKey)
            {
                //
                // The key should be formatted as:
                //
                //   <type> <key>
                //
                // But the API doesn't enforce that format,
                // so we might be encountering garbage.
                //
                var keyParts = osLoginKey.Key.Trim().Split(' ');

                //
                // The name should be formatted as:
                //
                //   users/<email>/sshPublicKeys/<fingerprint>
                //
                var nameParts = osLoginKey.Name.Split('/');

                if (keyParts.Length == 2 &&
                    nameParts.Length == 4 &&
                    nameParts[0] == "users" &&
                    nameParts[2] == "sshPublicKeys")
                {
                    Debug.Assert(nameParts[3] == osLoginKey.Fingerprint);

                    return new AuthorizedPublicKey(
                        osLoginKey.Fingerprint,
                        nameParts[1],
                        keyParts[0],
                        keyParts[1],
                        osLoginKey.ExpirationTimeUsec != null
                            ? (DateTime?)DateTimeOffsetExtensions
                                .FromUnixTimeMicroseconds(osLoginKey.ExpirationTimeUsec.Value)
                                .Date
                            : null);
                }
                else
                {
                    return null;
                }
            }

            public override string ToString() => this.Fingerprint;
        }
    }

    public class OsLoginSshKeyImportFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public OsLoginSshKeyImportFailedException(
            string message,
            IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }
}
