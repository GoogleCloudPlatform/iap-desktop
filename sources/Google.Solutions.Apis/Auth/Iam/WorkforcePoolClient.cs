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
using Google.Apis.Services;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// Client for "headful" workforce identity "3PI" OAuth.
    /// </summary>
    public class WorkforcePoolClient : OidcClientBase
    {
        private readonly ServiceEndpoint<WorkforcePoolClient> endpoint;
        private readonly IDeviceEnrollment deviceEnrollment;
        private readonly WorkforcePoolProviderLocator provider;
        private readonly UserAgent userAgent;
        private readonly StsService stsService;

        public WorkforcePoolClient(
            ServiceEndpoint<WorkforcePoolClient> endpoint,
            IDeviceEnrollment deviceEnrollment,
            IOidcOfflineCredentialStore store,
            WorkforcePoolProviderLocator provider,
            OidcClientRegistration registration,
            UserAgent userAgent)
            : base(store, registration)
        {
            Precondition.Expect(registration.Issuer == OidcIssuer.Sts, nameof(OidcIssuer));

            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.provider = provider.ExpectNotNull(nameof(provider));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));

            Precondition.Expect(registration.Issuer == OidcIssuer.Sts, "Issuer");

            var directions = endpoint.GetDirections(deviceEnrollment.State);
            this.stsService = new StsService(new BaseClientService.Initializer()
            {
                BaseUri = directions.BaseUri.ToString(),
                HttpClientFactory = new PscAndMtlsAwareHttpClientFactory(
                    directions,
                    deviceEnrollment,
                    userAgent)
            });
        }

        public static ServiceEndpoint<WorkforcePoolClient> CreateEndpoint(
            ServiceRoute? route = null)
        {
            return new ServiceEndpoint<WorkforcePoolClient>(
                route ?? ServiceRoute.Public,
                "https://sts.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public override IServiceEndpoint Endpoint => this.endpoint;

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        protected virtual IAuthorizationCodeFlow CreateFlow()
        {
            var initializer = new AuthPortalCodeFlow.Initializer(
                this.endpoint,
                this.deviceEnrollment,
                this.provider,
                this.Registration.ToClientSecrets(),
                this.userAgent)
            {
                Scopes = new[] { Scopes.Cloud }
            };

            return new AuthPortalCodeFlow(initializer);
        }

        private protected virtual Task<StsService.IntrospectTokenResponse> IntrospectTokenAsync(
            StsService.IntrospectTokenRequest request,
            CancellationToken cancellationToken)
        {
            return this.stsService.IntrospectTokenAsync(request, cancellationToken);
        }

        private async Task<WorkforcePoolSession> CreateSessionAsync(
            UserCredential apiCredential,
            CancellationToken cancellationToken)
        {
            var tokenInfo = await IntrospectTokenAsync(
                    new StsService.IntrospectTokenRequest()
                    {
                        ClientCredentials = this.Registration.ToClientSecrets(),
                        Token = apiCredential.Token.AccessToken,
                        TokenTypeHint = StsService.TokenTypes.AccessToken
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (tokenInfo.Active != true || tokenInfo.Username == null)
            {
                throw new AuthorizationFailedException(
                    "Authorization failed because the access token could " +
                    "not be introspected.");
            }

            Debug.Assert(tokenInfo.ClientId == this.Registration.ClientId);
            Debug.Assert(tokenInfo.Iss == "https://sts.googleapis.com/");
            Debug.Assert(tokenInfo.Username.StartsWith("principal://"));

            var session = new WorkforcePoolSession(
                apiCredential,
                this.provider,
                WorkforcePoolIdentity.FromPrincipalIdentifier(tokenInfo.Username));

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
                offlineCredential.Issuer == OidcIssuer.Sts,
                "Offline credential must be issued by STS");

            codeReceiver.ExpectNotNull(nameof(codeReceiver));

            var flow = CreateFlow();
            var app = new AuthorizationCodeInstalledApp(flow, codeReceiver);

            var apiCredential = await
                app.AuthorizeAsync(null, cancellationToken)
                .ConfigureAwait(true);

            //
            // NB. The API does OAuth, not OIDC, so we don't receive an ID token.
            // To get information about the user, we have to introspect the
            // access token.
            //
            Debug.Assert(apiCredential.Token.IdToken == null);

            try
            {
                return await
                    CreateSessionAsync(apiCredential, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                flow.Dispose();
                throw;
            }
        }

        protected override async Task<IOidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            offlineCredential.ExpectNotNull(nameof(offlineCredential));
            Precondition.Expect(offlineCredential.Issuer == OidcIssuer.Sts,
                "Offline credential must be issued by STS");

            var flow = CreateFlow();

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

                var apiCredential = new UserCredential(flow, null, tokenResponse);

                return await
                    CreateSessionAsync(apiCredential, cancellationToken)
                    .ConfigureAwait(false);
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
