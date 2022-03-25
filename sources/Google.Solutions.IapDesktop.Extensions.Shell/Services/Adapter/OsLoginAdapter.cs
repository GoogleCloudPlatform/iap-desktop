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

using Google.Apis.CloudOSLogin.v1;
using Google.Apis.CloudOSLogin.v1.Data;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Net;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter
{
    public interface IOsLoginAdapter
    {
        /// <summary>
        /// Import user's public key to OS Login.
        /// </summary>
        Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token);

        /// <summary>
        /// Read user's profile and published SSH keys.
        /// </summary>
        Task<LoginProfile> GetLoginProfileAsync(
           ProjectLocator project,
           CancellationToken token);
    }

    [Service(typeof(IOsLoginAdapter))]
    public class OsLoginAdapter : IOsLoginAdapter
    {
        private const string MtlsBaseUri = "https://oslogin.mtls.googleapis.com/";

        private readonly IAuthorizationSource authorizationSource;
        private readonly CloudOSLoginService service;


        internal bool IsDeviceCertiticateAuthenticationEnabled
            => this.service.IsMtlsEnabled() && this.service.IsClientCertificateProvided();

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public OsLoginAdapter(IAuthorizationSource authorizationSource)
        {
            this.authorizationSource = authorizationSource;
            this.service = new CloudOSLoginService(
                ClientServiceFactory.ForMtlsEndpoint(
                    authorizationSource.Authorization.Credential,
                    authorizationSource.Authorization.DeviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (authorizationSource.Authorization.DeviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == IsDeviceCertiticateAuthenticationEnabled);
        }

        public OsLoginAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationSource>())
        {
        }

        //---------------------------------------------------------------------
        // IOsLoginAdapter.
        //---------------------------------------------------------------------

        public async Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                var expiryTimeUsec = new DateTimeOffset(DateTime.UtcNow.Add(validity))
                    .ToUnixTimeMilliseconds() * 1000;

                var userEmail = this.authorizationSource.Authorization.Email;
                Debug.Assert(userEmail != null);

                var request = this.service.Users.ImportSshPublicKey(
                    new SshPublicKey()
                    {
                        Key = $"{key.Type} {key.PublicKeyString}",
                        ExpirationTimeUsec = expiryTimeUsec
                    },
                    $"users/{userEmail}");
                request.ProjectId = project.ProjectId;

                try
                {
                    var response = await request
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);

                    return response.LoginProfile;
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    //
                    // Likely reason: The user account is a consumer account or
                    // an administrator has disabled POSIX account/SSH key information
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

        public async Task<LoginProfile> GetLoginProfileAsync(
            ProjectLocator project,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                var request = this.service.Users.GetLoginProfile(
                    $"users/{this.authorizationSource.Authorization.Email}");
                request.ProjectId = project.ProjectId;

                try
                {
                    return await request
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to use OS Login: " +
                        e.Error?.Message ?? "access denied",
                        HelpTopics.ManagingOsLogin,
                        e);
                }
            }
        }
    }
}
