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
using Google.Solutions.Apis;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// A user's OS Login profile.
    /// </summary>
    public interface IOsLoginProfile
    {
        /// <summary>
        /// Upload an a public key to authorize it.
        /// </summary>
        Task<PlatformCredential> AuthorizeKeyAsync(
            ZoneLocator zone,
            ulong instanceId,
            OsLoginSystemType os,
            ServiceAccountEmail? attachedServiceAccount,
            IAsymmetricKeySigner key,
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

    [Service(typeof(IOsLoginProfile))]
    public sealed class OsLoginProfile : IOsLoginProfile
    {
        private static readonly ProjectLocator WellKnownProject
            = new ProjectLocator("windows-cloud");

        private readonly IOsLoginClient client;
        private readonly IAuthorization authorization;

        internal static string LookupUsername(LoginProfile loginProfile)
        {
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
                throw new InvalidOsLoginProfileException(
                    "The login profile does not contain a suitable POSIX account",
                    HelpTopics.TroubleshootingOsLogin);
            }

            return account.Username;
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public OsLoginProfile(
            IOsLoginClient adapter,
            IAuthorization authorization)
        {
            this.client = adapter.ExpectNotNull(nameof(adapter));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
        }

        //---------------------------------------------------------------------
        // IOsLoginService.
        //---------------------------------------------------------------------

        public async Task<PlatformCredential> AuthorizeKeyAsync(
            ZoneLocator zone,
            ulong instanceId,
            OsLoginSystemType os,
            ServiceAccountEmail? attachedServiceAccount,
            IAsymmetricKeySigner key,
            TimeSpan validity,
            CancellationToken token)
        {
            Precondition.ExpectNotNull(zone, nameof(zone));
            Precondition.ExpectNotNull(key, nameof(key));

            if (os != OsLoginSystemType.Linux)
            {
                throw new ArgumentException(
                    "The OS is not supported",
                    nameof(os));
            }

            if (validity.TotalSeconds <= 0)
            {
                throw new ArgumentException(
                    "Validity cannot be zero",
                    nameof(validity));
            }

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(zone))
            {
                //
                // If OS Login is enabled for a project, we have to use
                // the Posix username from the OS Login login profile.
                //
                // Note that the Posix account managed by OS login can 
                // differ based on the project that we're trying to access.
                // Therefore, we specify the project when importing or
                // certifying the key.
                //
                // OS Login auto-generates a username for us. Again, this
                // username might differ based on project/organization.
                //
                var publicKey = key.PublicKey.ToString(PublicKey.Format.OpenSsh);

                if (this.authorization.Session is IWorkforcePoolSession)
                {
                    //
                    // Authorize the key by signing it. This may fail for
                    // multiple reasons:
                    //
                    // - Missing OS Login permissions
                    // - Missing actAs permission on the attached service account(if any)
                    // - The user doesn't have a POSIX profile yet
                    //
                    // Note that we have no control over how long the
                    // certified key remains valid.
                    //
                    string certifiedKey;
                    try
                    {
                        certifiedKey = await this.client
                            .SignPublicKeyAsync(
                                zone,
                                instanceId,
                                attachedServiceAccount,
                                publicKey,
                                token)
                            .ConfigureAwait(false);
                    }
                    catch (ResourceNotFoundException)
                    {
                        //
                        // Crate a POSIX profile and try again.
                        //
                        await this.client
                            .ProvisionPosixProfileAsync(zone.Region, token)
                            .ConfigureAwait(false);

                        certifiedKey = await this.client
                            .SignPublicKeyAsync(
                                zone,
                                instanceId,
                                attachedServiceAccount,
                                publicKey,
                                token)
                            .ConfigureAwait(false);
                    }

                    var certificateSigner = new OsLoginCertificateSigner(
                        key,
                        certifiedKey);

                    return new PlatformCredential(
                        certificateSigner,
                        KeyAuthorizationMethods.Oslogin,
                        certificateSigner.Username);
                }
                else
                {
                    //
                    // Authorize the key by importing it.
                    //
                    // NB. It's cheaper to unconditionally push the key than
                    // to check for previous keys first.
                    // 

                    var loginProfile = await this.client
                        .ImportSshPublicKeyAsync(
                            new ProjectLocator(zone.ProjectId),
                            publicKey,
                            validity,
                            token)
                        .ConfigureAwait(false);

                    return new PlatformCredential(
                        key,
                        KeyAuthorizationMethods.Oslogin,
                        LookupUsername(loginProfile));
                }
            }
        }

        public async Task<IEnumerable<IAuthorizedPublicKey>> ListAuthorizedKeysAsync(
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                //
                // NB. The OS Login profile (in particular, the username
                // and UID/GID) depends on the project, and the organization
                // it resides in. However, the SSH public keys are independent
                // of that -- therefore, we can query the list of public keys
                // using any project.
                //
                // 
                var loginProfile = await this.client
                    .GetLoginProfileAsync(WellKnownProject, cancellationToken)
                    .ConfigureAwait(false);

                return loginProfile
                    .SshPublicKeys
                    .EnsureNotNull()
                    .Select(k => AuthorizedPublicKey.TryParse(k.Value))
                    .Where(k => k != null)
                    .Select(k => k!)
                    .ToList();
            }
        }

        public async Task DeleteAuthorizedKeyAsync(
            IAuthorizedPublicKey key,
            CancellationToken cancellationToken)
        {
            Debug.Assert(key is AuthorizedPublicKey);
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(key))
            {
                await this.client.DeleteSshPublicKeyAsync(
                        ((AuthorizedPublicKey)key).Fingerprint,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
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
                this.Fingerprint = fingerprint.ExpectNotNull(nameof(fingerprint));
                this.Email = email.ExpectNotEmpty(nameof(email));
                this.KeyType = keyType.ExpectNotEmpty(nameof(keyType));
                this.PublicKey = publicKey.ExpectNotEmpty(nameof(publicKey));
                this.ExpireOn = expiresOn;
            }

            public static AuthorizedPublicKey? TryParse(
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

    public class InvalidOsLoginProfileException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public InvalidOsLoginProfileException(
            string message,
            IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }

    public class OsLoginSkNotSupportedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public OsLoginSkNotSupportedException(
            string message,
            IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }
}
