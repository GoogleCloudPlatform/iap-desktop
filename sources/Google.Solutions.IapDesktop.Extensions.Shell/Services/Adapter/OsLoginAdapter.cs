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
using Google.Solutions.Apis;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.Ssh.Auth;
using System;
using System.Diagnostics;
using System.Linq;
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
            ISshKeyPair key,
            TimeSpan validity,
            CancellationToken token);

        /// <summary>
        /// Read user's profile and published SSH keys.
        /// </summary>
        Task<LoginProfile> GetLoginProfileAsync(
           ProjectLocator project,
           CancellationToken token);

        /// <summary>
        /// Delete existing authorized key.
        /// </summary>
        Task DeleteSshPublicKey(
            string fingerprint,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IOsLoginAdapter))]
    public class OsLoginAdapter : IOsLoginAdapter
    {
        private const string MtlsBaseUri = "https://oslogin.mtls.googleapis.com/";

        private readonly IAuthorization authorization;
        private readonly CloudOSLoginService service;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public OsLoginAdapter(IAuthorization authorization)
        {
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.service = new CloudOSLoginService(
                new AuthorizedClientInitializer(
                    authorization.Credential,
                    authorization.DeviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (authorization.DeviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == this.service.IsDeviceCertificateAuthenticationEnabled());
        }

        //---------------------------------------------------------------------
        // IOsLoginAdapter.
        //---------------------------------------------------------------------

        public async Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            ISshKeyPair key,
            TimeSpan validity,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                var expiryTimeUsec = new DateTimeOffset(DateTime.UtcNow.Add(validity))
                    .ToUnixTimeMilliseconds() * 1000;

                var userEmail = this.authorization.Email;
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

                    //
                    // Creating the profile succeeded (if it didn't exist
                    // yet -- but we still need to check if the key was actually
                    // added.
                    //
                    // If the 'Allow users to manage their SSH public keys
                    // via the OS Login API' policy is disabled (in Cloud Identity),
                    // then adding the key won't work.
                    //
                    if (response.LoginProfile.SshPublicKeys
                        .EnsureNotNull()
                        .Any(kvp => kvp.Value.Key.Contains(key.PublicKeyString)))
                    {
                        return response.LoginProfile;
                    }
                    else
                    {
                        //
                        // Key wasn't added.
                        //
                        throw new ResourceAccessDeniedException(
                            "You do not have sufficient permissions to publish an SSH " +
                            "key to OS Login",
                            HelpTopics.ManagingOsLogin,
                            new GoogleApiException("oslogin", response.Details ?? String.Empty));
                    }
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
                    $"users/{this.authorization.Email}");
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

        public async Task DeleteSshPublicKey(
            string fingerprint,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(fingerprint))
            {
                try
                {
                    var userEmail = this.authorization.Email;
                    Debug.Assert(userEmail != null);

                    await this.service.Users.SshPublicKeys
                        .Delete($"users/{userEmail}/sshPublicKeys/{fingerprint}")
                        .ExecuteAsync(cancellationToken)
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
