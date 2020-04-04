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
using Google.Apis.Util.Store;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Google.Apis.Http;
using Google.Solutions.Compute;
using System.Diagnostics;
using System;

namespace Google.Solutions.Compute.Auth
{
    public interface IAuthorization
    {
        ICredential Credential { get; }

        Task RevokeAsync();

        Task ReauthorizeAsync(CancellationToken token);
    }

    public class OAuthAuthorization : IAuthorization
    {
        public const string StoreUserId = "oauth";

        /// <summary>
        /// Scope required to query email from UserInfo endpoint
        /// </summary>
        private const string EmailScope = "https://www.googleapis.com/auth/userinfo.email";

        private readonly GoogleAuthorizationCodeFlow.Initializer initializer;
        private readonly string closePageReponse;
        private readonly IDataStore credentialStore;

        // The OAuth credential change after each reauth. Therefore, use
        // a SwappableCredential as indirection.
        private readonly SwappableCredential credential;

        private OAuthAuthorization(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse,
            IDataStore credentialStore,
            ICredential initialCredential,
            UserInfo userInfo)
        {
            this.initializer = initializer;
            this.closePageReponse = closePageReponse;
            this.credentialStore = credentialStore;
            this.credential = new SwappableCredential(initialCredential, userInfo);
        }

        public ICredential Credential => this.credential;

        public Task RevokeAsync()
        {
            return this.credentialStore.DeleteAsync<object>(StoreUserId);
        }

        private static async Task<UserInfo> QueryUserInfoAsync(ICredential credential, CancellationToken token)
        {
            var configuration = await IdpConfiguration.QueryMetadataAsync(token);
            return await UserInfo.QueryUserInfoAsync(
                configuration,
                credential,
                CancellationToken.None);
        }

        public static async Task<OAuthAuthorization> TryLoadExistingAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse,
            CancellationToken token)
        {
            // Make sure we can use the UserInfo endpoint later.
            initializer.AddScope(EmailScope);

            using (var flow = new GoogleAuthorizationCodeFlow(initializer))
            {
                var installedApp = new AuthorizationCodeInstalledApp(
                    flow,
                    new LocalServerCodeReceiver(closePageReponse));

                var existingTokenResponse = await flow.LoadTokenAsync(
                    OAuthAuthorization.StoreUserId,
                    token);

                if (!installedApp.ShouldRequestAuthorizationCode(existingTokenResponse))
                {
                    TraceSources.Compute.TraceVerbose("Found existing credentials");

                    var scopesOfExistingTokenResponse = existingTokenResponse.Scope.Split(' ');
                    if (!scopesOfExistingTokenResponse.ContainsAll(initializer.Scopes))
                    {
                        TraceSources.Compute.TraceVerbose(
                            "Dropping existing credential as it lacks one or more scopes");

                        // The existing auth might be fine, but it lacks a scope.
                        // Delete it so that it does not cause harm later.
                        await initializer.DataStore.DeleteAsync<object>(OAuthAuthorization.StoreUserId);
                        return null;
                    }
                    else
                    {
                        // N.B. Do not dispose the GoogleAuthorizationCodeFlow as it might
                        // be needed for re-auth later.
                        var credential = new UserCredential(
                            new GoogleAuthorizationCodeFlow(initializer),
                                OAuthAuthorization.StoreUserId,
                                existingTokenResponse);

                        var userInfo = await QueryUserInfoAsync(
                            credential,
                            token);

                        return new OAuthAuthorization(
                            initializer,
                            closePageReponse,
                            initializer.DataStore,
                            credential,
                            userInfo);
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<OAuthAuthorization> CreateAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse,
            CancellationToken token)
        {
            // Make sure we can use the UserInfo endpoint later.
            initializer.AddScope(EmailScope);

            // N.B. Do not dispose the GoogleAuthorizationCodeFlow as it might
            // be needed for re-auth later.
            var installedApp = new AuthorizationCodeInstalledApp(
                new GoogleAuthorizationCodeFlow(initializer),
                new LocalServerCodeReceiver(closePageReponse));

            TraceSources.Compute.TraceVerbose("Authorizing");

            // Pop up browser window.
            var credential = await installedApp.AuthorizeAsync(
                OAuthAuthorization.StoreUserId,
                token);

            var userInfo = await QueryUserInfoAsync(
                credential,
                token);

            return new OAuthAuthorization(
                initializer,
                closePageReponse,
                initializer.DataStore,
                credential,
                userInfo);
        }

        public async Task ReauthorizeAsync(CancellationToken token)
        {
            // As this is a 3p OAuth app, we do not support Gnubby/Password-based
            // reauth. Instead, we simply trigger a new authorization (code flow).
            var installedApp = new AuthorizationCodeInstalledApp(
                new GoogleAuthorizationCodeFlow(this.initializer),
                new LocalServerCodeReceiver(this.closePageReponse));

            var newCredential = await installedApp.AuthorizeAsync(
                OAuthAuthorization.StoreUserId,
                token);

            // The user might have changed to a different user account,
            // so we have to re-fetch user information.
            var newUserInfo = await QueryUserInfoAsync(
                credential,
                token);

            this.credential.SwapCredential(newCredential, newUserInfo);
        }

        private class SwappableCredential : ICredential
        {
            private ICredential currentCredential;
            private UserInfo currentUserInfo;

            public SwappableCredential(ICredential curentCredential, UserInfo currentUserInfo)
            {
                this.currentCredential = curentCredential;
                this.currentUserInfo = currentUserInfo;
            }

            public void SwapCredential(ICredential newCredential, UserInfo newUserInfo)
            {
                this.currentCredential = newCredential;
                this.currentUserInfo = newUserInfo;
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
