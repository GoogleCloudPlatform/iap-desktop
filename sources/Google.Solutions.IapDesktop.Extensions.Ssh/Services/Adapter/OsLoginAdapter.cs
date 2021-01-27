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

using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudOSLogin.v1;
using Google.Apis.CloudOSLogin.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth;
using Google.Solutions.Ssh;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter
{
    public interface IOsLoginAdapter
    {
        Task<AuthorizedKey> ImportSshPublicKeyAsync(
            string projectId,
            OsLoginSystemType os,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token);
    }

    public enum OsLoginSystemType
    {
        Linux
    }

    [Service(typeof(IOsLoginAdapter))]
    public class OsLoginAdapter : IOsLoginAdapter
    {
        private const string MtlsBaseUri = "https://oslogin.mtls.googleapis.com/";

        private readonly IAuthorizationAdapter authorizationAdapter;
        private readonly CloudOSLoginService service;


        internal bool IsDeviceCertiticateAuthenticationEnabled
            => this.service.IsMtlsEnabled() && this.service.IsClientCertificateProvided();

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public OsLoginAdapter(IAuthorizationAdapter authAdapter)
        {
            this.authorizationAdapter = authAdapter;
            this.service = new CloudOSLoginService(
                ClientServiceFactory.ForMtlsEndpoint(
                    authAdapter.Authorization.Credential,
                    authAdapter.DeviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (authAdapter.DeviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == IsDeviceCertiticateAuthenticationEnabled);
        }

        public OsLoginAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationAdapter>())
        {
        }

        //---------------------------------------------------------------------
        // IOsLoginAdapter.
        //---------------------------------------------------------------------

        public async Task<AuthorizedKey> ImportSshPublicKeyAsync(
            string projectId,
            OsLoginSystemType os,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token)
        {
            Utilities.ThrowIfNullOrEmpty(projectId, nameof(projectId));
            Utilities.ThrowIfNull(key, nameof(key));

            if (os != OsLoginSystemType.Linux)
            {
                throw new ArgumentException(nameof(os));
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters())
            {
                var expiry = DateTime.UtcNow.Add(validity);
                var userEmail = this.authorizationAdapter.Authorization.Email;
                Debug.Assert(userEmail != null);

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
                // TODO: check for policy-denied issues.
                // TODO: check for consumer account issue.
                //
                var request = this.service.Users.ImportSshPublicKey(
                    new SshPublicKey()
                    {
                        Key = $"{key.Type} {key.PublicKeyString}",
                        ExpirationTimeUsec = new DateTimeOffset(expiry).ToUnixTimeMilliseconds() * 1000
                    },
                    $"users/{userEmail}");
                request.ProjectId = projectId;

                try
                {
                    var response = await request
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);

                    //
                    // Although rare, there could be multiple POSIX accounts.
                    //
                    var account = response.LoginProfile.PosixAccounts
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

                    return AuthorizedKey.ForOsLoginAccount(key, account);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    //
                    // Likely reason: The user account is a consumer account or
                    // an administrator has disable POSIX account/SSH key information
                    // updates in the Admin Console.
                    //
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to use OS Login: " +
                        e.Error?.Message ?? "access denied",
                        HelpTopics.ManagingOsLogin,
                        e);
                }
            }
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
