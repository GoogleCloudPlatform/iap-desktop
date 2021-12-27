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
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Common.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1034 // Class nesting

namespace Google.Solutions.Common.Auth
{
    public class GoogleAuthAdapter : IAuthAdapter
    {
        // Scope required to query email from UserInfo endpoint.
        public const string EmailScope = "https://www.googleapis.com/auth/userinfo.email";

        private const string ConfigurationEndpoint =
            "https://accounts.google.com/.well-known/openid-configuration";

        public const string StoreUserId = "oauth";

        private readonly GoogleAuthorizationCodeFlow.Initializer initializer;
        private readonly GoogleAuthorizationCodeFlow flow;
        private readonly AuthorizationCodeInstalledApp installedApp;

        public GoogleAuthAdapter(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            // Add email scope.
            this.initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = initializer.ClientSecrets,
                Scopes = initializer.Scopes.Concat(new[] { GoogleAuthAdapter.EmailScope }),
                DataStore = initializer.DataStore
            };

            this.flow = new GoogleAuthorizationCodeFlow(this.initializer);
            this.installedApp = new AuthorizationCodeInstalledApp(
                this.flow,
                new LocalServerCodeReceiver(closePageReponse));
        }

        public IEnumerable<string> Scopes => this.initializer.Scopes;

        public Task<TokenResponse> GetStoredRefreshTokenAsync(CancellationToken token)
        {
            return this.flow.LoadTokenAsync(
                StoreUserId,
                token);
        }

        public Task DeleteStoredRefreshToken()
        {
            return this.initializer.DataStore.DeleteAsync<TokenResponse>(StoreUserId);
        }

        public ICredential AuthorizeUsingRefreshToken(TokenResponse tokenResponse)
        {
            return new UserCredential(
                this.flow,
                StoreUserId,
                tokenResponse);
        }

        public async Task<ICredential> AuthorizeUsingBrowserAsync(CancellationToken token)
        {
            try
            {
                var userCredential = await this.installedApp.AuthorizeAsync(
                        StoreUserId,
                        token)
                    .ConfigureAwait(true);

                //
                // NB. If an admin changes the access level for the app (in the Admin Console),
                // then it's possible that some of the requested scopes haven't been granted.
                //
                var grantedScopes = userCredential.Token.Scope?.Split(' ');
                if (this.initializer.Scopes.Any(
                    requestedScope => !grantedScopes.Contains(requestedScope)))
                {
                    throw new AuthorizationFailedException(
                        "Authorization failed because you have denied access to a " +
                        "required resource. Sign in again and make sure " +
                        "to grant access to all requested resources.");
                }

                return userCredential;
            }
            catch (PlatformNotSupportedException)
            {
                // Convert this into an exception with more actionable information.
                throw new AuthorizationFailedException(
                    "Authorization failed because the HTTP Server API is not enabled " +
                    "on your computer. This API is required to complete the OAuth authorization flow.\n\n" +
                    "To enable the API, open an elevated command prompt and run 'sc config http start= auto'.");
            }
        }

        public bool IsRefreshTokenValid(TokenResponse tokenResponse)
        {
            return !this.installedApp.ShouldRequestAuthorizationCode(tokenResponse);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.flow.Dispose();
            }
        }

        public static async Task<OpenIdConfiguration> QueryOpenIdConfigurationAsync(
            CancellationToken token)
        {
            return await new RestClient().GetAsync<OpenIdConfiguration>(
                ConfigurationEndpoint,
                token).ConfigureAwait(false);
        }

        public async Task<UserInfo> QueryUserInfoAsync(
            ICredential credential,
            CancellationToken token)
        {
            var configuration = await QueryOpenIdConfigurationAsync(token).ConfigureAwait(false);

            var client = new RestClient(credential);

            return await client.GetAsync<UserInfo>(
                configuration.UserInfoEndpoint,
                token).ConfigureAwait(false);
        }

        public class OpenIdConfiguration
        {
            [JsonProperty("userinfo_endpoint")]
            public string UserInfoEndpoint { get; set; }
        }
    }

    public class AuthorizationFailedException : Exception
    {
        public AuthorizationFailedException(string message) : base(message)
        {
        }
    }
}
