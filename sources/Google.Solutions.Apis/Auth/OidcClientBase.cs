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
                    "Attempting authorization using offline credential");

                Debug.Assert(offlineCredential.RefreshToken != null);
                try
                {
                    var authorization = await
                        ActivateOfflineCredentialAsync(offlineCredential, cancellationToken)
                        .ConfigureAwait(false);
                    Debug.Assert(authorization != null);

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

        public Task<IOidcAuthorization> AuthorizeAsync(
            CancellationToken cancellationToken)
        {
            IAuthorizationCodeFlow flow;
            if (this.store.TryRead(out var offlineCredential) &&
                offlineCredential.IdToken != null &&
                UnverifiedGoogleJsonWebToken.Decode(offlineCredential.IdToken) is var idToken &&
                !string.IsNullOrEmpty(idToken.Payload.Email))
            {
                //
                // Perform a "minimal" authorization:
                //  - use existing email as login hint (to skip account chooser)
                //  - don't request the email scope so that consent unbundling
                //    doesn't apply
                //
                flow = CreateReauthFlow(idToken.Payload.Email);
            }
            else
            {
                flow = CreateFlow();
            }

            //
            // TODO: same as old client.

            throw new NotImplementedException();
        }

        protected abstract Task<OidcAuthorization> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);

        protected abstract IAuthorizationCodeFlow CreateFlow();

        protected abstract IAuthorizationCodeFlow CreateReauthFlow(string email);

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

    public class GoogleOidcClient : OidcClientBase
    {
        private readonly ServiceEndpoint<GoogleOidcClient> endpoint;
        private readonly ClientSecrets clientSecrets;

        public GoogleOidcClient(
            ServiceEndpoint<GoogleOidcClient> endpoint,
            IDeviceEnrollment deviceEnrollment,
            IOidcOfflineCredentialStore store,
            ClientSecrets clientSecrets)
            : base(deviceEnrollment, store)
        {
            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
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
        // Overrides
        //---------------------------------------------------------------------

        protected override IAuthorizationCodeFlow CreateFlow()
        {
            var initializer = new CodeFlowInitializer(
                this.endpoint, 
                this.DeviceEnrollment)
            {
                ClientSecrets = this.clientSecrets,
                Scopes = new[] { GoogleOAuthScopes.Cloud, GoogleOAuthScopes.Email }
            };

            return new GoogleAuthorizationCodeFlow(initializer);
        }

        protected override IAuthorizationCodeFlow CreateReauthFlow(string email)
        {
            var initializer = new CodeFlowInitializer(
                this.endpoint,
                this.DeviceEnrollment)
            {
                LoginHint = email.ExpectNotEmpty(nameof(email)),
                ClientSecrets = this.clientSecrets,
                Scopes = new[] { GoogleOAuthScopes.Cloud }
            };

            return new GoogleAuthorizationCodeFlow(initializer);
        }

        protected override async Task<OidcAuthorization> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            var flow = CreateFlow();

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
                // but that doesn't matter since we just use it to
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
                flow.Dispose();

                throw new OAuthScopeNotGrantedException(
                    "The offline credential neither contains an existing ID token " +
                    "nor the necessary scopes to obtain an ID token");
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

    public static class GoogleOAuthScopes
    {
        public const string Email = "https://www.googleapis.com/auth/userinfo.email";
        public const string Cloud = "https://www.googleapis.com/auth/cloud-platform";
    }
}
