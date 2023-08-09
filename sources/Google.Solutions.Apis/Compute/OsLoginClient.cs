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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Compute
{
    /// <summary>
    /// Client for OS Login API.
    /// </summary>
    public interface IOsLoginClient : IClient
    {
        /// <summary>
        /// Import user's public key to OS Login.
        /// </summary>
        /// <param name="keyType">Key type (for ex, 'ssh-rsa')</param>
        /// <param name="keyBlob">SSH1/Base64-encoded public key</param>
        Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            string keyType,
            string keyBlob,
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
        Task DeleteSshPublicKeyAsync(
            string fingerprint,
            CancellationToken cancellationToken);
    }

    public class OsLoginClient : ApiClientBase, IOsLoginClient
    {
        private readonly IAuthorization authorization;
        private readonly CloudOSLoginService service;

        public OsLoginClient(
            ServiceEndpoint<OsLoginClient> endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
            : base(endpoint, authorization, userAgent)
        {
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.service = new CloudOSLoginService(this.Initializer);
        }

        public static ServiceEndpoint<OsLoginClient> CreateEndpoint(
                ServiceRoute route = null)
        {
            return new ServiceEndpoint<OsLoginClient>(
                route ?? ServiceRoute.Public,
                "https://oslogin.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IOsLoginAdapter.
        //---------------------------------------------------------------------

        public async Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            string keyType,
            string keyBlob,
            TimeSpan validity,
            CancellationToken token)
        {
            project.ExpectNotNull(nameof(project));
            keyType.ExpectNotEmpty(nameof(keyType));
            keyBlob.ExpectNotEmpty(nameof(keyBlob));

            Debug.Assert(!keyType.Contains(' '));

            var gaiaSession = this.authorization.Session as IGaiaOidcSession
                ?? throw new NotSupportedForWorkloadIdentityException();

            using (ApiTraceSources.Default.TraceMethod().WithParameters(project))
            {
                var expiryTimeUsec = new DateTimeOffset(DateTime.UtcNow.Add(validity))
                    .ToUnixTimeMilliseconds() * 1000;

                var userEmail = gaiaSession.Email;
                Debug.Assert(userEmail != null);

                var request = this.service.Users.ImportSshPublicKey(
                    new SshPublicKey()
                    {
                        Key = $"{keyType} {keyBlob}",
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
                        .Any(kvp => kvp.Value.Key.Contains(keyBlob)))
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
                            new GoogleApiException("oslogin", response.Details ?? string.Empty));
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
            using (ApiTraceSources.Default.TraceMethod().WithParameters(project))
            {
                var gaiaSession = this.authorization.Session as IGaiaOidcSession 
                    ?? throw new NotSupportedForWorkloadIdentityException();

                var request = this.service.Users.GetLoginProfile(
                    $"users/{gaiaSession.Email}");
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

        public async Task DeleteSshPublicKeyAsync(
            string fingerprint,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSources.Default.TraceMethod().WithParameters(fingerprint))
            {
                var gaiaSession = this.authorization.Session as IGaiaOidcSession
                    ?? throw new NotSupportedForWorkloadIdentityException();

                try
                {
                    var userEmail = gaiaSession.Email;
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
