//
// Copyright 2019 Google LLC
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
using Google.Apis.Auth.OAuth2.Flows;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Http;

namespace Google.Solutions.Compute.Auth
{
    public interface IAuthorization
    {
        ICredential Credential { get; }

        Task RevokeAsync();

        Task ReauthorizeAsync(CancellationToken token);

        string Email { get; }
    }

    public class OAuthAuthorization : IAuthorization
    {
        // Scope required to query email from UserInfo endpoint.
        private const string EmailScope = "https://www.googleapis.com/auth/userinfo.email";

        private readonly IAuthAdapter adapter;

        // The OAuth credential changes after each reauth. Therefore, use
        // a SwappableCredential as indirection.
        private readonly SwappableCredential credential;

        private OAuthAuthorization(
            IAuthAdapter adapter,
            ICredential initialCredential,
            UserInfo userInfo)
        {
            this.adapter = adapter;
            this.credential = new SwappableCredential(initialCredential, userInfo);
        }

        public ICredential Credential => this.credential;
        public string Email => this.credential.UserInfo.Email;

        public Task RevokeAsync()
        {
            return this.adapter.DeleteStoredRefreshToken();
        }

        public static async Task<OAuthAuthorization> TryLoadExistingAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse,
            CancellationToken token)
        {
            // Make sure we can use the UserInfo endpoint later.
            initializer.AddScope(EmailScope);

            // N.B. Do not dispose the adapter (and embedded GoogleAuthorizationCodeFlow)
            // as it might be needed for token refreshes later.
            var oauthAdapter = new GoogleAuthAdapter(initializer, closePageReponse);
            
            var existingTokenResponse = await oauthAdapter.GetStoredRefreshTokenAsync(token);

            if (oauthAdapter.IsRefreshTokenValid(existingTokenResponse))
            {
                TraceSources.Compute.TraceVerbose("Found existing credentials");

                var scopesOfExistingTokenResponse = existingTokenResponse.Scope.Split(' ');
                if (!scopesOfExistingTokenResponse.ContainsAll(initializer.Scopes))
                {
                    TraceSources.Compute.TraceVerbose(
                        "Dropping existing credential as it lacks one or more scopes");

                    // The existing auth might be fine, but it lacks a scope.
                    // Delete it so that it does not cause harm later.
                    await oauthAdapter.DeleteStoredRefreshToken();
                    return null;
                }
                else
                {
                    var credential = oauthAdapter.AuthorizeUsingRefreshToken(existingTokenResponse);

                    var userInfo = await oauthAdapter.QueryUserInfoAsync(
                        credential,
                        token);

                    return new OAuthAuthorization(
                        oauthAdapter,
                        credential,
                        userInfo);
                }
            }
            else
            {
                return null;
            }
        }

        public static async Task<OAuthAuthorization> CreateAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse,
            CancellationToken token)
        {
            // Make sure we can use the UserInfo endpoint later.
            initializer.AddScope(EmailScope);

            // N.B. Do not dispose the adapter (and embedded GoogleAuthorizationCodeFlow)
            // as it might be needed for token refreshes later.
            var oauthAdapter = new GoogleAuthAdapter(initializer, closePageReponse);

            TraceSources.Compute.TraceVerbose("Authorizing");

            // Pop up browser window.
            var credential = await oauthAdapter.AuthorizeUsingBrowserAsync(token);

            var userInfo = await oauthAdapter.QueryUserInfoAsync(
                credential,
                token);

            return new OAuthAuthorization(
                oauthAdapter,
                credential,
                userInfo);
        }

        public async Task ReauthorizeAsync(CancellationToken token)
        {
            // As this is a 3p OAuth app, we do not support Gnubby/Password-based
            // reauth. Instead, we simply trigger a new authorization (code flow).
            var newCredential = await this.adapter.AuthorizeUsingBrowserAsync(token);

            // The user might have changed to a different user account,
            // so we have to re-fetch user information.
            var newUserInfo = await this.adapter.QueryUserInfoAsync(
                newCredential,
                token);

            this.credential.SwapCredential(newCredential, newUserInfo);
        }

        private class SwappableCredential : ICredential
        {
            private ICredential currentCredential;

            public UserInfo UserInfo { get; private set; }

            public SwappableCredential(ICredential curentCredential, UserInfo currentUserInfo)
            {
                this.currentCredential = curentCredential;
                this.UserInfo = currentUserInfo;
            }

            public void SwapCredential(ICredential newCredential, UserInfo newUserInfo)
            {
                this.currentCredential = newCredential;
                this.UserInfo = newUserInfo;
            }

            public void Initialize(ConfigurableHttpClient httpClient)
            {
                this.currentCredential.Initialize(httpClient);
            }

            public Task<string> GetAccessTokenForRequestAsync(
                string authUri = null, 
                CancellationToken cancellationToken = default(CancellationToken))
            {
                return this.currentCredential.GetAccessTokenForRequestAsync(authUri, cancellationToken);
            }
        }
    }

    internal static class AuthorizationCodeFlowInitializerExtensions
    {
        public static void AddScope(this AuthorizationCodeFlow.Initializer initializer, string scope)
        {
            var allScopes = initializer.Scopes.ToHashSet();
            allScopes.Add(scope);
            initializer.Scopes = allScopes;
        }
    }
}
