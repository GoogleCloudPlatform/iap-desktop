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
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1034 // Class nesting

namespace Google.Solutions.Apis.Auth
{
    public interface ISignInAdapter
    {
        Task DeleteRefreshTokenAsync();

        Task<UserCredential> TrySignInWithRefreshTokenAsync(
            CancellationToken token);

        Task<UserCredential> SignInWithBrowserAsync(
            string loginHint,
            CancellationToken token);

        Task<UserInfo> QueryUserInfoAsync(
            ICredential credential,
            CancellationToken token);
    }

    public class SignInAdapter : ISignInAdapter
    {
        // Scope required to query email from UserInfo endpoint.
        public const string EmailScope = "https://www.googleapis.com/auth/userinfo.email";

        public const string StoreUserId = "oauth";

        private readonly X509Certificate2 deviceCertificate;
        private readonly ICodeReceiver codeReceiver;
        private readonly ClientSecrets clientSecrets;
        private readonly UserAgent userAgent;
        private readonly IEnumerable<string> scopes;
        private readonly IDataStore dataStore;
        private readonly Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow;

        public SignInAdapter(
            X509Certificate2 deviceCertificate,
            ClientSecrets clientSecrets,
            UserAgent userAgent,
            IEnumerable<string> scopes,
            IDataStore dataStore,
            ICodeReceiver codeReceiver,
            Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow = null)
        {
            this.deviceCertificate = deviceCertificate;
            this.codeReceiver = codeReceiver.ExpectNotNull(nameof(codeReceiver));
            this.clientSecrets = clientSecrets.ExpectNotNull(nameof(clientSecrets));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
            this.scopes = scopes.ExpectNotNull(nameof(scopes));
            this.dataStore = dataStore.ExpectNotNull(nameof(dataStore));
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
                Scopes = this.scopes.Concat(new[] { SignInAdapter.EmailScope }),
                DataStore = dataStore
            };
        }

        private IAuthorizationCodeFlow CreateFlow(OAuthInitializer initializer)
        {
            if (this.createCodeFlow != null)
            {
                return this.createCodeFlow(initializer);
            }
            else
            {
                var flow = new GoogleAuthorizationCodeFlow(initializer);

                ApiTraceSources.Default.TraceVerbose(
                    "mTLS supported: {0}", ClientServiceMtlsExtensions.CanEnableDeviceCertificateAuthentication);
                ApiTraceSources.Default.TraceVerbose(
                    "mTLS certificate: {0}", this.deviceCertificate?.Subject);
                ApiTraceSources.Default.TraceVerbose(
                    "TokenServerUrl: {0}", flow.TokenServerUrl);
                ApiTraceSources.Default.TraceVerbose(
                    "RevokeTokenUrl: {0}", flow.RevokeTokenUrl);

                return flow;
            }
        }

        public Task DeleteRefreshTokenAsync()
        {
            return this.dataStore.DeleteAsync<TokenResponse>(StoreUserId);
        }

        //---------------------------------------------------------------------
        // Authorize.
        //---------------------------------------------------------------------

        public async Task<UserCredential> TrySignInWithRefreshTokenAsync(
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
                ApiTraceSources.Default.TraceVerbose("Found existing credentials");

                var scopesOfExistingTokenResponse = existingTokenResponse.Scope.Split(' ');
                if (!scopesOfExistingTokenResponse.ContainsAll(this.scopes))
                {
                    ApiTraceSources.Default.TraceVerbose(
                        "Dropping existing credential as it lacks one or more scopes");

                    //
                    // The existing auth might be fine, but it lacks a scope.
                    // Delete it so that it does not cause harm later.
                    //
                    await DeleteRefreshTokenAsync().ConfigureAwait(false);

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

        public async Task<UserCredential> SignInWithBrowserAsync(
            string loginHint,
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
                    throw new OAuthScopeNotGrantedException(
                        "Authorization failed because you have denied access to a " +
                        "required resource. Sign in again and make sure " +
                        "to grant access to all requested resources.");
                }

                return userCredential;
            }
            catch (TokenResponseException e) when (
                e.Error?.ErrorUri != null &&
                e.Error.ErrorUri.StartsWith("https://accounts.google.com/info/servicerestricted"))
            {
                if (this.deviceCertificate != null)
                {
                    throw new AuthorizationFailedException(
                        "Authorization failed because your computer's device certificate is " +
                        "is invalid or unrecognized. Use the Endpoint Verification extension " +
                        "to verify that your computer is enrolled and try again.\n\n" + e.Error.ErrorDescription);
                }
                else
                {
                    throw new AuthorizationFailedException(
                        "Authorization failed because your computer is not enrolled in Endpoint " +
                        "Verification.\n\n" + e.Error.ErrorDescription);

                }
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
            var client = new RestClient(this.userAgent, this.deviceCertificate);

            return await client
                .GetAsync<UserInfo>(
                    CreateInitializer().UserInfoUrl,
                    credential,
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
                    ClientServiceMtlsExtensions.EnableDeviceCertificateAuthentication(
                        this,
                        certificate);

                    ApiTraceSources.Default.TraceVerbose("Using OAuth mTLS endpoints");
                }
                else
                {
                    ApiTraceSources.Default.TraceVerbose("Using OAuth TLS endpoints");
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
    public class OAuthScopeNotGrantedException : AuthorizationFailedException
    {
        public OAuthScopeNotGrantedException(string message) : base(message)
        {
        }
    }
}
