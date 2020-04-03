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
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Google.Apis.Http;

namespace Google.Solutions.Compute.Auth
{
    public interface IAuthorization
    {
        ICredential Credential { get; }

        Task RevokeAsync();

        Task ReauthorizeAsync();
    }

    public class OAuthAuthorization : IAuthorization
    {
        public const string StoreUserId = "oauth";

        private readonly GoogleAuthorizationCodeFlow.Initializer initializer;
        private readonly string closePageReponse;
        private readonly IDataStore credentialStore;

        // The OAuth credential change after each reauth. Therefore, use
        // a SwappableCredential as indirection.
        private readonly SwappableCredential credential;

        internal OAuthAuthorization(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse,
            IDataStore credentialStore,
            ICredential initialCredential)
        {
            this.initializer = initializer;
            this.closePageReponse = closePageReponse;
            this.credentialStore = credentialStore;
            this.credential = new SwappableCredential(initialCredential);
        }

        public ICredential Credential => this.credential;

        public Task RevokeAsync()
        {
            return this.credentialStore.DeleteAsync<object>(StoreUserId);
        }

        public static async Task<OAuthAuthorization> TryLoadExistingAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            using (var flow = new GoogleAuthorizationCodeFlow(initializer))
            {
                var installedApp = new AuthorizationCodeInstalledApp(
                    flow,
                    new LocalServerCodeReceiver(closePageReponse));

                var existingTokenResponse = await flow.LoadTokenAsync(
                    OAuthAuthorization.StoreUserId,
                    CancellationToken.None);

                if (!installedApp.ShouldRequestAuthorizationCode(existingTokenResponse))
                {
                    // N.B. Do not dispose the GoogleAuthorizationCodeFlow as it might
                    // be needed for re-auth later.
                    return new OAuthAuthorization(
                        initializer,
                        closePageReponse,
                        initializer.DataStore,
                        new UserCredential(
                            new GoogleAuthorizationCodeFlow(initializer),
                            OAuthAuthorization.StoreUserId,
                            existingTokenResponse));
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<OAuthAuthorization> CreateAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            // N.B. Do not dispose the GoogleAuthorizationCodeFlow as it might
            // be needed for re-auth later.
            var installedApp = new AuthorizationCodeInstalledApp(
                new GoogleAuthorizationCodeFlow(initializer),
                new LocalServerCodeReceiver(closePageReponse));

            return new OAuthAuthorization(
                initializer,
                closePageReponse,
                initializer.DataStore,
                await installedApp.AuthorizeAsync(
                    OAuthAuthorization.StoreUserId,
                    CancellationToken.None));
        }

        public async Task ReauthorizeAsync()
        {
            // As this is a 3p OAuth app, we do not support Gnubby/Password-based
            // reauth. Instead, we simply trigger a new authorization (code flow).
            var installedApp = new AuthorizationCodeInstalledApp(
                new GoogleAuthorizationCodeFlow(this.initializer),
                new LocalServerCodeReceiver(this.closePageReponse));

            this.credential.SwapCredential(await installedApp.AuthorizeAsync(
                OAuthAuthorization.StoreUserId,
                CancellationToken.None));
        }

        private class SwappableCredential : ICredential
        {
            private ICredential currentCredential;

            public SwappableCredential(ICredential curentCredential)
            {
                this.currentCredential = curentCredential;
            }

            public void SwapCredential(ICredential credential)
            {
                this.currentCredential = credential;
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
}
