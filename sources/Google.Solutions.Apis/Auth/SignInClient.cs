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
using Google.Apis.Http;
using Google.Apis.Util.Store;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
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
    public interface ISignInClient
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

    public class SignInClient : ISignInClient 
    {
        // Scope required to query email from UserInfo endpoint.
        public const string EmailScope = "https://www.googleapis.com/auth/userinfo.email";

        public const string StoreUserId = "oauth";

        private readonly ServiceEndpoint<OAuthClient> oauthEndpoint;
        private readonly ServiceEndpoint<OpenIdClient> openIdEndpoint;
        private readonly IDeviceEnrollment enrollment;
        private readonly ICodeReceiver codeReceiver;
        private readonly ClientSecrets clientSecrets;
        private readonly UserAgent userAgent;
        private readonly IEnumerable<string> scopes;
        private readonly IDataStore dataStore;
        private readonly Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow;

        public SignInClient(
            ServiceEndpoint<OAuthClient> oauthEndpoint,
            ServiceEndpoint<OpenIdClient> openIdEndpoint,
            IDeviceEnrollment enrollment,
            ClientSecrets clientSecrets,
            UserAgent userAgent,
            IEnumerable<string> scopes,
            IDataStore dataStore,
            ICodeReceiver codeReceiver,
            Func<GoogleAuthorizationCodeFlow.Initializer, IAuthorizationCodeFlow> createCodeFlow = null)
        {
            this.oauthEndpoint = oauthEndpoint.ExpectNotNull(nameof(oauthEndpoint));
            this.openIdEndpoint = openIdEndpoint.ExpectNotNull(nameof(openIdEndpoint));
            this.enrollment = enrollment.ExpectNotNull(nameof(enrollment));
            this.codeReceiver = codeReceiver.ExpectNotNull(nameof(codeReceiver));
            this.clientSecrets = clientSecrets.ExpectNotNull(nameof(clientSecrets));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
            this.scopes = scopes.ExpectNotNull(nameof(scopes));
            this.dataStore = dataStore.ExpectNotNull(nameof(dataStore));
            this.createCodeFlow = createCodeFlow;
        }

        private Initializers.OpenIdInitializer CreateInitializer()
        {
            var initializer = Initializers.CreateOpenIdInitializer(
                this.oauthEndpoint,
                this.openIdEndpoint,
                this.enrollment);

            //
            // Add email scope to requested scope so that we can query
            // user info.
            //
            initializer.ClientSecrets = this.clientSecrets;
            initializer.Scopes = this.scopes.Concat(new[] { SignInClient.EmailScope });
            initializer.DataStore = this.dataStore;

            return initializer;
        }

        private IAuthorizationCodeFlow CreateFlow(
            Initializers.OpenIdInitializer initializer)
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
                    "mTLS certificate: {0}", this.enrollment?.Certificate?.Subject);

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
                if (this.enrollment.State == DeviceEnrollmentState.Enrolled)
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
            var initializer = CreateInitializer();
            var client = new RestClient(
                initializer.HttpClientFactory
                    .CreateHttpClient(new CreateHttpClientArgs()),
                this.userAgent);
            
            return await client
                .GetAsync<UserInfo>(
                    CreateInitializer().UserInfoUrl.ToString(),
                    credential,
                    token)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public abstract class OAuthClient : IClient
        {
            public static ServiceEndpoint<OAuthClient> CreateEndpoint()
            {
                return new ServiceEndpoint<OAuthClient>(
                    PrivateServiceConnectDirections.None,
                    "https://oauth2.googleapis.com/");
            }

            public IServiceEndpoint Endpoint { get; }
        }

        public abstract class OpenIdClient : IClient
        {
            public static ServiceEndpoint<OpenIdClient> CreateEndpoint()
            {
                return new ServiceEndpoint<OpenIdClient>(
                    PrivateServiceConnectDirections.None,
                    "https://openidconnect.googleapis.com/");
            }

            public IServiceEndpoint Endpoint { get; }
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
