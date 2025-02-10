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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Gaia
{
    /// <summary>
    /// Client for Google "1PI" OIDC.
    /// </summary>
    public class GaiaOidcClient : OidcClientBase
    {
        private readonly ServiceEndpoint<GaiaOidcClient> endpoint;
        private readonly IDeviceEnrollment deviceEnrollment;
        private readonly UserAgent userAgent;

        public GaiaOidcClient(
            ServiceEndpoint<GaiaOidcClient> endpoint,
            IDeviceEnrollment deviceEnrollment,
            IOidcOfflineCredentialStore store,
            OidcClientRegistration registration,
            UserAgent userAgent)
            : base(store, registration)
        {
            Precondition.Expect(registration.Issuer == OidcIssuer.Gaia, nameof(OidcIssuer));

            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));

            Precondition.Expect(registration.Issuer == OidcIssuer.Gaia, "Issuer");
            Precondition.Expect(registration.RedirectPath == "/authorize/", "RedirectPath");
        }

        public static ServiceEndpoint<GaiaOidcClient> CreateEndpoint(
            ServiceRoute? route = null)
        {
            return new ServiceEndpoint<GaiaOidcClient>(
                route ?? ServiceRoute.Public,
                "https://oauth2.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public override IServiceEndpoint Endpoint => this.endpoint;

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        protected virtual IAuthorizationCodeFlow CreateFlow(
            GoogleAuthorizationCodeFlow.Initializer initializer)
        {
            return new GoogleAuthorizationCodeFlow(initializer);
        }

        internal static GaiaOidcSession CreateSession(
            IAuthorizationCodeFlow flow,
            OidcOfflineCredential? offlineCredential,
            TokenResponse tokenResponse)
        {
            flow.ExpectNotNull(nameof(flow));
            tokenResponse.ExpectNotNull(nameof(tokenResponse));

            Debug.Assert(tokenResponse.RefreshToken != null);
            Debug.Assert(tokenResponse.AccessToken != null);

            if (tokenResponse.IdToken != null)
            {
                //
                // We got a fresh ID token because the original
                // authorization included the email scope.
                //

                Debug.Assert(tokenResponse.Scope
                    .Split(' ')
                    .Contains(Scopes.Email));

                var apiCredential = new UserCredential(flow, null, tokenResponse);
                var idToken = tokenResponse.IdToken;

                //
                // Use the fresh ID token.
                //
                // NB. We but don't verify the ID token here because
                // verification requires access to the JWKS, and the JWKS
                // might not be available over PSC.
                //
                return new GaiaOidcSession(
                    apiCredential,
                    UnverifiedGaiaJsonWebToken.Decode(idToken));
            }
            else if (offlineCredential != null &&
                !string.IsNullOrEmpty(offlineCredential.IdToken) &&
                UnverifiedGaiaJsonWebToken.Decode(offlineCredential.IdToken!) is var offlineIdToken &&
                offlineIdToken != null &&
                !string.IsNullOrEmpty(offlineIdToken.Payload.Email))
            {
                //
                // We didn't get a new ID token, but we still have
                // the one from last time. This one might be expired,
                // but that doesn't matter since we only use it to
                // extract the email address.
                //

                Debug.Assert(!tokenResponse.Scope
                    .Split(' ')
                    .Contains(Scopes.Email));

                var apiCredential = new UserCredential(flow, null, tokenResponse);

                return new GaiaOidcSession(
                    apiCredential,
                    offlineIdToken);
            }
            else
            {
                //
                // We don't have any usable ID token.
                //
                throw new OAuthScopeNotGrantedException(
                    "The offline credential neither contains an existing ID token " +
                    "nor the necessary scopes to obtain an ID token");
            }
        }

        private GaiaOidcSession CreateSessionAndRegisterTerminateEvent(
            IAuthorizationCodeFlow flow,
            OidcOfflineCredential? offlineCredential,
            TokenResponse tokenResponse)
        {
            var session = CreateSession(
                flow,
                offlineCredential,
                tokenResponse);

            Debug.Assert(session.IdToken.Payload.Email != null);

            session.Terminated += (_, __) => ClearOfflineCredentialStore();
            return session;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override async Task<IOidcSession> AuthorizeWithBrowserAsync(
            OidcOfflineCredential? offlineCredential,
            ICodeReceiver codeReceiver,
            CancellationToken cancellationToken)
        {
            Precondition.Expect(offlineCredential == null ||
                offlineCredential.Issuer == OidcIssuer.Gaia,
                "Offline credential must be issued by Gaia");

            codeReceiver.ExpectNotNull(nameof(codeReceiver));

            var initializer = new GaiaCodeFlow.Initializer(
                this.endpoint,
                this.deviceEnrollment,
                this.userAgent)
            {
                ClientSecrets = this.Registration.ToClientSecrets()
            };

            if (offlineCredential?.IdToken != null &&
                UnverifiedGaiaJsonWebToken.TryDecode(offlineCredential.IdToken, out var offlineIdToken) &&
                offlineIdToken != null &&
                !string.IsNullOrEmpty(offlineIdToken.Payload.Email))
            {
                //
                // We still have an ID token with an email address, so we can perform
                // a "minimal flow":
                //
                //  - use existing email as login hint (to skip account chooser)
                //  - don't request the email scope again so that consent unbundling
                //    doesn't apply
                //
                // NB. The last point is important and the entire point why we're storing
                // the ID token: Consent unbundling (i.e., the behavior of the OAuth consent
                // screen where it shows unchecked checkboxes for all scopes) only applies
                // when we request two or more scopes. By only requesting a single scope,
                // we can sidestep consent unbundling, thereby improving UX.
                //

                initializer.LoginHint = offlineIdToken.Payload.Email;
                initializer.Scopes = new[] { Scopes.Cloud };
            }
            else
            {
                initializer.Scopes = new[] { Scopes.Cloud, Scopes.Email };
            }

            try
            {
                var flow = CreateFlow(initializer);
                var app = new AuthorizationCodeInstalledApp(flow, codeReceiver);

                var apiCredential = await
                    app.AuthorizeAsync(null, cancellationToken)
                    .ConfigureAwait(true);

                //
                // Verify that all requested scopes have been granted.
                //
                var grantedScopes = apiCredential.Token.Scope?.Split(' ');
                if (initializer.Scopes.Any(
                    requestedScope => !grantedScopes.Contains(requestedScope)))
                {
                    throw new OAuthScopeNotGrantedException(
                        "Authorization failed because you have denied access to a " +
                        "required resource. Sign in again and make sure " +
                        "to grant access to all requested resources.");
                }

                try
                {
                    //
                    // N.B. Do not dispose the flow if the sign-in succeeds as the
                    // credential object must hold on to it.
                    //
                    return CreateSessionAndRegisterTerminateEvent(
                        flow,
                        offlineCredential,
                        apiCredential.Token);
                }
                catch
                {
                    flow.Dispose();
                    throw;
                }
            }
            catch (TokenResponseException e) when (
                e.Error?.ErrorUri != null &&
                e.Error.ErrorUri.StartsWith("https://accounts.google.com/info/servicerestricted"))
            {
                if (this.deviceEnrollment.State == DeviceEnrollmentState.Enrolled)
                {
                    throw new AuthorizationFailedException(
                        "Authorization failed because your computer's device certificate is " +
                        "is invalid or unrecognized. Use the Endpoint Verification extension " +
                        "to verify that your computer is enrolled and try again.\n\n" +
                        e.Error.ErrorDescription);
                }
                else
                {
                    throw new AuthorizationFailedException(
                        "Authorization failed because a context-aware access requirement " +
                        "was not met.\n\n" + e.Error.ErrorDescription);

                }
            }
        }

        protected override async Task<IOidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            offlineCredential.ExpectNotNull(nameof(offlineCredential));
            Precondition.Expect(offlineCredential.Issuer == OidcIssuer.Gaia,
                "Offline credential must be issued by Gaia");

            var initializer = new GaiaCodeFlow.Initializer(
                this.endpoint,
                this.deviceEnrollment,
                this.userAgent)
            {
                ClientSecrets = this.Registration.ToClientSecrets()
            };

            var flow = CreateFlow(initializer);

            //
            // Try to use the refresh token to obtain a new access token.
            //
            try
            {
                var tokenResponse = await flow
                    .RefreshTokenAsync(null, offlineCredential.RefreshToken, cancellationToken)
                    .ConfigureAwait(false);

                //
                // N.B. Do not dispose the flow if the sign-in succeeds as the
                // credential object must hold on to it.
                //
                return CreateSessionAndRegisterTerminateEvent(
                    flow,
                    offlineCredential,
                    tokenResponse);
            }
            catch (Exception e)
            {
                ApiTraceSource.Log.TraceWarning(
                    "Refreshing the stored token failed: {0}", e.FullMessage());

                //
                // The refresh token must have been revoked or
                // the session expired (reauth).
                //

                flow.Dispose();
                throw;
            }
        }
    }
}
