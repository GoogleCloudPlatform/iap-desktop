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

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Client for performing a OIDC-based authorization.
    /// </summary>
    public interface IOidcClient : IClient
    {
        /// <summary>
        /// Try to authorize using an existing refresh token.
        /// </summary>
        /// <returns>Null if silent authorization failed</returns>
        Task<IOidcAuthorization> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Authorize using a browser-based OIDC flow.
        /// </summary>
        Task<IOidcAuthorization> AuthorizeAsync(
            CancellationToken cancellationToken);
    }

    public abstract class OidcClientBase : IOidcClient
    {
        private readonly IOidcOfflineCredentialStore store;

        protected IDeviceEnrollment DeviceEnrollment { get; }

        protected OidcClientBase(
            IDeviceEnrollment deviceEnrollment,
            IOidcOfflineCredentialStore store)
        {
            this.DeviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.store = store.ExpectNotNull(nameof(store));
        }

        //---------------------------------------------------------------------
        // IOidcClient.
        //---------------------------------------------------------------------

        public abstract IServiceEndpoint Endpoint { get; }

        public async Task<IOidcAuthorization> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken)
        {
            if (this.store.TryRead(out var offlineCredential))
            {
                ApiTraceSources.Default.TraceVerbose(
                    "Attempting authorization using offline credential...");

                Debug.Assert(offlineCredential.RefreshToken != null);
                try
                {
                    var authorization = await
                        ActivateOfflineCredentialAsync(offlineCredential, cancellationToken)
                        .ConfigureAwait(false);
                    Debug.Assert(authorization != null);
                    Debug.Assert(authorization.IdToken.Payload.Email != null);

                    //
                    // Update the offline credential as the refresh
                    // token and/or ID token might have changed.
                    //
                    this.store.Write(authorization.OfflineCredential);

                    return authorization;
                }
                catch (Exception e)
                {
                    //
                    // The offline credentials didn't work.
                    //

                    ApiTraceSources.Default.TraceWarning(
                        "Activating offline credential failed: {0}", 
                        e.FullMessage());

                    this.store.Clear();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public async Task<IOidcAuthorization> AuthorizeAsync(
            CancellationToken cancellationToken)
        {
            this.store.TryRead(out var offlineCredential);

            var authorization = await 
                AuthorizeWithBrowserAsync(offlineCredential, cancellationToken)
                .ConfigureAwait(false);

            //
            // Store the refresh token so that we can do a silent
            // activation next time.
            //
            this.store.Write(authorization.OfflineCredential);
            return authorization;
        }

        protected abstract Task<OidcAuthorization> AuthorizeWithBrowserAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);

        protected abstract Task<OidcAuthorization> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        protected class OidcAuthorization : IOidcAuthorization
        {
            private readonly UserCredential apiCredential;

            public OidcAuthorization(
                IDeviceEnrollment deviceEnrollment,
                UserCredential apiCredential, 
                IJsonWebToken idToken)
            {
                this.DeviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
                this.apiCredential = apiCredential.ExpectNotNull(nameof(apiCredential));
                this.IdToken = idToken.ExpectNotNull(nameof(idToken));
            }

            public IJsonWebToken IdToken { get; }
            public ICredential ApiCredential => this.apiCredential;
            public IDeviceEnrollment DeviceEnrollment { get; }

            public OidcOfflineCredential OfflineCredential
            {
                get => new OidcOfflineCredential(
                    this.apiCredential.Token.RefreshToken,
                    this.apiCredential.Token.IdToken);
            }
        }
    }

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
            PrivateServiceConnectDirections pscDirections)
        {
            return new ServiceEndpoint<GoogleOidcClient>(
                pscDirections,
                "https://oauth2.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public override IServiceEndpoint Endpoint => this.endpoint;

        //---------------------------------------------------------------------
        // Privates.
        //---------------------------------------------------------------------

        private OidcAuthorization MergeCredentials(
            GoogleAuthorizationCodeFlow flow,
            OidcOfflineCredential offlineCredential,
            TokenResponse tokenResponse)
        {
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
                    .Contains(GoogleOAuthScopes.Email));

                var apiCredential = new UserCredential(flow, null, tokenResponse);
                var idToken = tokenResponse.IdToken;

                //
                // Use the fresh ID token.
                //
                // NB. We but don't verify the ID token here because
                // verification requires access to the JWKS, and the JWKS
                // might not be available over PSC.
                //
                return new OidcAuthorization(
                    this.DeviceEnrollment,
                    apiCredential,
                    UnverifiedGoogleJsonWebToken.Decode(idToken));
            }
            else if (offlineCredential.IdToken != null &&
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
                    .Contains(GoogleOAuthScopes.Email));

                var apiCredential = new UserCredential(flow, null, tokenResponse);

                return new OidcAuthorization(
                    this.DeviceEnrollment,
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

        protected override async Task<OidcAuthorization> AuthorizeWithBrowserAsync(
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
                UnverifiedGoogleJsonWebToken.Decode(offlineCredential.IdToken) is var offlineIdToken &&
                !string.IsNullOrEmpty(offlineIdToken.Payload.Email))
            {
                //
                // We still have an ID token with an email address, so we can perform
                // a "minimal" authorization:
                //
                //  - use existing email as login hint (to skip account chooser)
                //  - don't request the email scope again so that consent unbundling
                //    doesn't apply
                //
                initializer.LoginHint = offlineIdToken.Payload.Email;
                initializer.Scopes = new[] { GoogleOAuthScopes.Cloud };
            }
            else
            {
                initializer.Scopes = new[] { GoogleOAuthScopes.Cloud, GoogleOAuthScopes.Email };
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
                    return MergeCredentials(flow, offlineCredential, apiCredential.Token);
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

        protected override async Task<OidcAuthorization> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
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
                return MergeCredentials(flow, offlineCredential, tokenResponse);
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
    }

    // TODO: WorkforceIdentityClient, cf https://docs.google.com/document/d/1wVqW62U-BMXnSlNdS962ixPPeUf-IN-MJjuZduEfgIU/edit?resourcekey=0-Oc-wjfuG9RWL5-yjnkiltg#heading=h.l5nfl0rnl0va

    public static class GoogleOAuthScopes
    {
        public const string Email = "https://www.googleapis.com/auth/userinfo.email";
        public const string Cloud = "https://www.googleapis.com/auth/cloud-platform";
    }
}
