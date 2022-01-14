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
using System.Security.Cryptography.X509Certificates;
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

        Task<ICredential> SignInWithBrowserAsync(
            String loginHint,
            CancellationToken token);

        Task<UserInfo> QueryUserInfoAsync(
            ICredential credential, 
            CancellationToken token);
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

        private readonly X509Certificate2 deviceCertificate;
        private readonly ICodeReceiver codeReceiver;
        private readonly ClientSecrets clientSecrets;
        private readonly IEnumerable<string> scopes;
        private readonly IDataStore dataStore;
        private readonly Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow;

        public SignInAdapter(
            X509Certificate2 deviceCertificate,
            ClientSecrets clientSecrets,
            IEnumerable<string> scopes,
            IDataStore dataStore,
            ICodeReceiver codeReceiver,
            Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow = null)
        {
            this.deviceCertificate = deviceCertificate;
            this.codeReceiver = codeReceiver;
            this.clientSecrets = clientSecrets;
            this.scopes = scopes;
            this.dataStore = dataStore;
            this.createCodeFlow = createCodeFlow;
        }

        private OAuthInitializer CreateInitializer()
        {
            //
            // Add email scope to requested scope so that we can query
            // user info.
            //
            return new OAuthInitializer(this.deviceCertificate)
            {
                ClientSecrets = clientSecrets,
                Scopes = scopes.Concat(new[] { SignInAdapter.EmailScope }),
                DataStore = dataStore
            };
        }

        private IAuthorizationCodeFlow CreateFlow(OAuthInitializer initializer)
        {
            return this.createCodeFlow != null
                ? this.createCodeFlow(initializer)
                :new GoogleAuthorizationCodeFlow(initializer);
        }

        public Task DeleteStoredRefreshToken()
        {
            return this.dataStore.DeleteAsync<TokenResponse>(StoreUserId);
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

            var flow = CreateFlow(CreateInitializer());
            var app = new AuthorizationCodeInstalledApp(flow, this.codeReceiver);

            var existingTokenResponse = await flow.LoadTokenAsync(
                    StoreUserId,
                    token)
                .ConfigureAwait(false);

            if (!app.ShouldRequestAuthorizationCode(existingTokenResponse))
            {
                ApplicationTraceSources.Default.TraceVerbose("Found existing credentials");

                var scopesOfExistingTokenResponse = existingTokenResponse.Scope.Split(' ');
                if (!scopesOfExistingTokenResponse.ContainsAll(this.scopes))
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

        public async Task<ICredential> SignInWithBrowserAsync(
            String loginHint, 
            CancellationToken token)
        {
            try
            {
                var initializer = CreateInitializer();
                initializer.LoginHint = loginHint;

                //
                // N.B. Do not dispose the flow if the sign-in succeeds as the
                // credential object must hold on to it.
                //
                var flow = CreateFlow(initializer);
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
                if (this.scopes.Any(
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

        public async Task<UserInfo> QueryUserInfoAsync(
            ICredential credential,
            CancellationToken token)
        {
            var client = new RestClient()
            {
                ClientCertificate = this.deviceCertificate,
                Credential = credential,
            };

            return await client.GetAsync<UserInfo>(
                    CreateInitializer().UserInfoUrl,
                    token)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class OAuthInitializer : GoogleAuthorizationCodeFlow.Initializer
        {
            public string UserInfoUrl { get; } 

            private static string FixupUrl(
                string url,
                X509Certificate2 certificate)
            {
                //
                // Switch to mTLS endpoint if there is a device certificate.
                //
                return certificate == null
                    ? url
                    : url.Replace(".googleapis.com", ".mtls.googleapis.com");
            }

            public OAuthInitializer(X509Certificate2 certificate)
                : base(FixupUrl("https://accounts.google.com/o/oauth2/v2/auth", certificate), 
                       FixupUrl("https://oauth2.googleapis.com/token", certificate), 
                       FixupUrl("https://oauth2.googleapis.com/revoke", certificate))
            {
                this.UserInfoUrl = FixupUrl(
                    "https://openidconnect.googleapis.com/v1/userinfo", certificate);

                if (certificate != null)
                {
                    //
                    // Inject the certificate into all HTTP communication.
                    //
                    this.HttpClientFactory = new MtlsHttpClientFactory(certificate);
                }
            }
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
