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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter
{
    public interface IOsLoginAdapter
    {
        Task<LoginProfile> ImportSshPublicKeyAsync(
            string projectId,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token);
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

        public async Task<LoginProfile> ImportSshPublicKeyAsync(
            string projectId,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                var expiryTimeUsec = new DateTimeOffset(DateTime.UtcNow.Add(validity))
                    .ToUnixTimeMilliseconds() * 1000;

                var userEmail = this.authorizationAdapter.Authorization.Email;
                Debug.Assert(userEmail != null);

                var request = this.service.Users.ImportSshPublicKey(
                    new SshPublicKey()
                    {
                        Key = $"{key.Type} {key.PublicKeyString}",
                        ExpirationTimeUsec = expiryTimeUsec
                    },
                    $"users/{userEmail}");
                request.ProjectId = projectId;

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
}
