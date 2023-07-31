using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Client for Google "1PI" OIDC.
    /// </summary>
    public class GoogleOidcClient : OidcClientBase
    {
        private readonly ServiceEndpoint<GoogleOidcClient> endpoint;
        private readonly ICodeReceiver codeReceiver;
        private readonly ClientSecrets clientSecrets;

        public GoogleOidcClient(
            ServiceEndpoint<GoogleOidcClient> endpoint,
            IDeviceEnrollment deviceEnrollment,
            ICodeReceiver codeReceiver,
            IOidcOfflineCredentialStore store,
            ClientSecrets clientSecrets)
            : base(deviceEnrollment, store)
        {
            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.codeReceiver = codeReceiver.ExpectNotNull(nameof(codeReceiver));
            this.clientSecrets = clientSecrets.ExpectNotNull(nameof(clientSecrets));
        }

        public static ServiceEndpoint<GoogleOidcClient> CreateEndpoint(
            PrivateServiceConnectDirections pscDirections = null)
        {
            return new ServiceEndpoint<GoogleOidcClient>(
                pscDirections ?? PrivateServiceConnectDirections.None,
                "https://oauth2.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public override IServiceEndpoint Endpoint => this.endpoint;

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        internal static OidcSession CreateSession(
            GoogleAuthorizationCodeFlow flow,
            IDeviceEnrollment deviceEnrollment,
            OidcOfflineCredential offlineCredential,
            TokenResponse tokenResponse)
        {
            flow.ExpectNotNull(nameof(flow));
            deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            offlineCredential.ExpectNotNull(nameof(offlineCredential));
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
                return new OidcSession(
                    deviceEnrollment,
                    apiCredential,
                    UnverifiedGoogleJsonWebToken.Decode(idToken));
            }
            else if (!string.IsNullOrEmpty(offlineCredential.IdToken) &&
                UnverifiedGoogleJsonWebToken.Decode(offlineCredential.IdToken) is var offlineIdToken &&
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

                return new OidcSession(
                    deviceEnrollment,
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

        //---------------------------------------------------------------------
        // Overrides
        //---------------------------------------------------------------------

        protected override async Task<OidcSession> AuthorizeWithBrowserAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            var initializer = new CodeFlowInitializer(
                this.endpoint,
                this.DeviceEnrollment)
            {
                ClientSecrets = this.clientSecrets
            };

            if (offlineCredential?.IdToken != null &&
                UnverifiedGoogleJsonWebToken.TryDecode(offlineCredential.IdToken, out var offlineIdToken) &&
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
                var flow = new GoogleAuthorizationCodeFlow(initializer);
                var app = new AuthorizationCodeInstalledApp(flow, this.codeReceiver);

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
                    return CreateSession(
                        flow, 
                        this.DeviceEnrollment, 
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
                if (this.DeviceEnrollment.State == DeviceEnrollmentState.Enrolled)
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
                    "To enable the API, open an elevated command prompt and run " +
                    "'sc config http start= auto'.");
            }
        }

        protected override async Task<OidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            offlineCredential.ExpectNotNull(nameof(offlineCredential));

            var initializer = new CodeFlowInitializer(
                this.endpoint,
                this.DeviceEnrollment)
            {
                ClientSecrets = this.clientSecrets
            };

            var flow = new GoogleAuthorizationCodeFlow(initializer);

            TokenResponse tokenResponse;
            try
            {
                tokenResponse = await flow
                    .RefreshTokenAsync(null, offlineCredential.RefreshToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ApiTraceSources.Default.TraceWarning(
                    "Refreshing the stored token failed: {0}", e.FullMessage());

                //
                // The refresh token must have been revoked or
                // the session expired (reauth).
                //

                flow.Dispose();
                throw;
            }

            try
            {
                //
                // N.B. Do not dispose the flow if the sign-in succeeds as the
                // credential object must hold on to it.
                //
                return CreateSession(
                    flow, 
                    this.DeviceEnrollment,
                    offlineCredential, 
                    tokenResponse);
            }
            catch
            {
                flow.Dispose();
                throw;
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class CodeFlowInitializer : GoogleAuthorizationCodeFlow.Initializer
        {
            protected CodeFlowInitializer(
                ServiceEndpointDirections directions,
                IDeviceEnrollment deviceEnrollment)
                : base(
                      GoogleAuthConsts.OidcAuthorizationUrl,
                      new Uri(directions.BaseUri, "/token").ToString(),
                      new Uri(directions.BaseUri, "/revoke").ToString())
            {
                this.HttpClientFactory = new Initializers.MtlsAwareHttpClientFactory(
                    directions,
                    deviceEnrollment);

                // TODO: set user agent?

                ApiTraceSources.Default.TraceInformation(
                    "OAuth: Using token URL {0}",
                    this.TokenServerUrl);
            }

            public CodeFlowInitializer(
                ServiceEndpoint<GoogleOidcClient> endpoint,
                IDeviceEnrollment deviceEnrollment)
                : this(
                      endpoint.GetDirections(deviceEnrollment.State),
                      deviceEnrollment)
            {
            }
        }

        internal static class Scopes
        {
            public const string Email = "https://www.googleapis.com/auth/userinfo.email";
            public const string Cloud = "https://www.googleapis.com/auth/cloud-platform";
        }
    }
}
