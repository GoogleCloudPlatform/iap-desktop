//
// Copyright 2023 Google LLC
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

using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudSecurityToken.v1;
using Google.Apis.Json;
using Google.Solutions.Apis.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Auth
{
    internal class TemporaryWorkforcePoolSubject : TemporaryPrincipal
    {
        private TemporaryWorkforcePoolSubject(
            CloudResourceManagerService crmService,
            string poolId,
            string providerId,
            string username,
            string subjectToken)
            : base(crmService, username)
        {
            this.PoolId = poolId;
            this.ProviderId = providerId;
            this.SubjectToken = subjectToken;
        }

        internal override string PrincipalId =>
            "principal://iam.googleapis.com/locations/global" +
            $"/workforcePools/{this.PoolId}/subject/{this.Username}";

        public string PoolId { get; }
        public string ProviderId { get; }
        public string SubjectToken { get; }

        public async Task<IAuthorization> ImpersonateAsync(
            CancellationToken cancellationToken)
        {
            var response = await new CloudSecurityTokenService()
                .V1
                .Token(
                    new Google.Apis.CloudSecurityToken.v1.Data.GoogleIdentityStsV1ExchangeTokenRequest()
                    {
                        GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
                        Audience = "//iam.googleapis.com/locations/global/workforcePools" +
                            $"/{this.PoolId}/providers/{this.ProviderId}",
                        Scope = "https://www.googleapis.com/auth/cloud-platform",
                        RequestedTokenType = "urn:ietf:params:oauth:token-type:access_token",
                        SubjectToken = this.SubjectToken,
                        SubjectTokenType = "urn:ietf:params:oauth:token-type:id_token"
                    })
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return new TemporaryAuthorization(
                new Enrollment(),
                new TemporaryWorkforcePoolSession(
                    this,
                    new TemporaryCredential(response.AccessToken)));
        }

        //---------------------------------------------------------------------
        // Factory.
        //---------------------------------------------------------------------

        public static async Task<TemporaryWorkforcePoolSubject> CreateAsync(
            CloudResourceManagerService crmService,
            IdentityPlatformService identityPlatformService,
            TemporaryServiceAccount trustedServiceAccount,
            string workforcePoolId,
            string workforceProviderId,
            string username,
            CancellationToken cancellationToken)
        {
            //
            // We can't use Gaia as "external" IdP for workforce identity,
            // but we can do so indirectly with GCIP in between.
            //

            //
            // Create a custom token using the "trusted" service account
            // (i.e. a service account that's in the same project as the
            // GCIP tenant.
            //
            var customToken = await trustedServiceAccount
                .SignJwtAsync(
                    new Dictionary<string, object>
                    {
                        { "iss", trustedServiceAccount.Username },
                        { "sub", trustedServiceAccount.Username },
                        { "aud", "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit" },
                        { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                        { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() },
                        { "uid", username }
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            //
            // Use the custom token to impersonate a GCIP user.
            //
            var gcipIdToken = await identityPlatformService
                .SignInWithCustomTokenAsync(customToken, cancellationToken)
                .ConfigureAwait(false);

            //
            // Use the GCIP ID token to start a workforce identity session.
            //
            return new TemporaryWorkforcePoolSubject(
                crmService,
                workforcePoolId,
                workforceProviderId,
                username,
                gcipIdToken);
        }

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        internal class IdentityPlatformService
        {
            private readonly string apiKey;

            public IdentityPlatformService(string apiKey)
            {
                this.apiKey = apiKey;
            }

            public async Task<string> SignInWithCustomTokenAsync(
                string customToken,
                CancellationToken cancellationToken)
            {
                var payload = new Dictionary<string, string>
                {
                    { "token", customToken },
                    { "returnSecureToken", "true" }
                };

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key={this.apiKey}")
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json")
                })
                using (var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content
                        .ReadAsStreamAsync()
                        .ConfigureAwait(false))
                    {
                        return NewtonsoftJsonSerializer
                            .Instance
                            .Deserialize<Dictionary<string, string>>(stream)
                            ["idToken"];
                    }
                }
            }
        }
    }
}
