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
using Google.Apis.Util.Store;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Net;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1034 // Class nesting

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface ISignInAdapter
    {
        Task DeleteStoredRefreshToken();

        Task<ICredential> TrySignInWithRefreshTokenAsync(
            CancellationToken token);

        Task<ICredential> SignInWithBrowserAsync(CancellationToken token);

        Task<UserInfo> QueryUserInfoAsync(ICredential credential, CancellationToken token);
    }

    public class UserInfo
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hd")]
        public string HostedDomain { get; set; }

        [JsonProperty("sub")]
        public string Subject { get; set; }
    }

    public class SignInAdapter : ISignInAdapter
    {
        // Scope required to query email from UserInfo endpoint.
        public const string EmailScope = "https://www.googleapis.com/auth/userinfo.email";

        public const string StoreUserId = "oauth";

        private readonly OAuthInitializer initializer;
        private readonly ICodeReceiver codeReceiver;
        private readonly Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow;

        public SignInAdapter(
            ClientSecrets clientSecrets,
            IEnumerable<string> scopes,
            IDataStore dataStore,
            string closePageReponse,
            Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow = null)
        {
            // Add email scope.
            this.initializer = new OAuthInitializer
            {
                ClientSecrets = clientSecrets,
                Scopes = scopes.Concat(new[] { SignInAdapter.EmailScope }),
                DataStore = dataStore
            };

            this.codeReceiver = new LocalServerCodeReceiver(closePageReponse);

            if (createCodeFlow != null)
            {
                this.createCodeFlow = createCodeFlow;
            }
            else
            {
                this.createCodeFlow = i => new GoogleAuthorizationCodeFlow(i);
            }
        }

        public Task DeleteStoredRefreshToken()
        {
            return this.initializer.DataStore.DeleteAsync<TokenResponse>(StoreUserId);
        }

        //---------------------------------------------------------------------
        // Authorize.
        //---------------------------------------------------------------------

        public async Task<ICredential> TrySignInWithRefreshTokenAsync(
            CancellationToken token)
        {
            //
            // N.B. Do not dispose the flow if the sign-in succeeds as the
            // credential object must hold on to it.
            //

            var flow = this.createCodeFlow(this.initializer);
            var app = new AuthorizationCodeInstalledApp(flow, this.codeReceiver);

            var existingTokenResponse = await flow.LoadTokenAsync(
                    StoreUserId,
                    token)
                .ConfigureAwait(false);

            if (!app.ShouldRequestAuthorizationCode(existingTokenResponse))
            {
                ApplicationTraceSources.Default.TraceVerbose("Found existing credentials");

                var scopesOfExistingTokenResponse = existingTokenResponse.Scope.Split(' ');
                if (!scopesOfExistingTokenResponse.ContainsAll(this.initializer.Scopes))
                {
                    ApplicationTraceSources.Default.TraceVerbose(
                        "Dropping existing credential as it lacks one or more scopes");

                    //
                    // The existing auth might be fine, but it lacks a scope.
                    // Delete it so that it does not cause harm later.
                    //
                    await DeleteStoredRefreshToken().ConfigureAwait(false);

                    flow.Dispose();
                    return null;
                }
                else
                {
                    return new UserCredential(
                        flow,
                        StoreUserId,
                        existingTokenResponse);
                }
            }
            else
            {
                flow.Dispose();
                return null;
            }
        }

        public async Task<ICredential> SignInWithBrowserAsync(CancellationToken token)
        {
            try
            {
                //
                // N.B. Do not dispose the flow if the sign-in succeeds as the
                // credential object must hold on to it.
                //

                var flow = this.createCodeFlow(this.initializer);
                var app = new AuthorizationCodeInstalledApp(flow, this.codeReceiver);

                var userCredential = await app.AuthorizeAsync(
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

        //---------------------------------------------------------------------
        // User info.
        //---------------------------------------------------------------------

        public async Task<OpenIdConfiguration> QueryOpenIdConfigurationAsync(
            CancellationToken token)
        {
            return await new RestClient().GetAsync<OpenIdConfiguration>(
                    this.initializer.MetadataUrl,
                    token)
                .ConfigureAwait(false);
        }

        public async Task<UserInfo> QueryUserInfoAsync(
            ICredential credential,
            CancellationToken token)
        {
            var configuration = await QueryOpenIdConfigurationAsync(token).ConfigureAwait(false);

            var client = new RestClient(credential);

            return await client.GetAsync<UserInfo>(
                    configuration.UserInfoEndpoint,
                    token)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class OAuthInitializer : GoogleAuthorizationCodeFlow.Initializer
        {
            public const string DefaultMetadataUrl =
                "https://accounts.google.com/.well-known/openid-configuration";

            public string MetadataUrl { get; set; } = DefaultMetadataUrl;

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
