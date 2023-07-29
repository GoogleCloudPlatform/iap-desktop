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

        private async Task<OidcAuthorization> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            var flow = CreateFlow(null);

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

            UserCredential apiCredential;
            string idToken;

            if (tokenResponse.IdToken != null)
            {
                //
                // We got a fresh ID token because the original
                // authorization included the email scope.
                //

                Debug.Assert(tokenResponse.Scope
                    .Split(' ')
                    .Contains(GoogleOAuthScopes.Email));

                apiCredential = new UserCredential(flow, null, tokenResponse);
                idToken = tokenResponse.IdToken;
            }
            else if (offlineCredential.IdToken != null)
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

                apiCredential = new UserCredential(flow, null, tokenResponse);
                idToken = offlineCredential.IdToken;
            }
            else
            {
                //
                // We don't have any ID token, so this offline credential is
                // useless.
                //
                flow.Dispose();
                this.store.Clear();

                throw new NotImplementedException();//TODO: Throw proper ex
            }

            //
            // Decode, but don't verify the ID token. This is because
            // verification requires access to the JWKS, and the JWKS
            // might not be available over PSC.
            //
            // We don't use the ID token for authorization, so verification
            // isn't neccesary.
            //
            return new OidcAuthorization(
                this.DeviceEnrollment,
                apiCredential,
                UnverifiedGoogleJsonWebToken.Decode(idToken));
        }

        private string CreateLoginHint()
        {
            if (this.store.TryRead(out var offlineCredential) &&
                offlineCredential.IdToken != null &&
                UnverifiedGoogleJsonWebToken.Decode(offlineCredential.IdToken) is var idToken &&
                !string.IsNullOrEmpty(idToken.Payload.Email))
            {
                return idToken.Payload.Email;
            }
            else
            {
                return null;
            }
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
            else
            {
                return null;
            }
        }

        public Task<IOidcAuthorization> AuthorizeAsync(
            CancellationToken cancellationToken)
        {
            //
            // TODO: If we have an offline cred, don't request the email scope again.
            var flow = CreateFlow(CreateLoginHint());

            throw new NotImplementedException();
        }

        protected abstract IAuthorizationCodeFlow CreateFlow(
            string loginHint);

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
        public GoogleOidcClient(
            IServiceEndpoint endpoint,
            IDeviceEnrollment deviceEnrollment,
            IOidcOfflineCredentialStore store)
            : base(deviceEnrollment, store)
        {
            this.Endpoint = endpoint.ExpectNotNull(nameof(endpoint));
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

        public override IServiceEndpoint Endpoint { get; }

        //---------------------------------------------------------------------
        // Overrides
        //-------------------------------------------------------------------

        protected override IAuthorizationCodeFlow CreateFlow(string loginHint)
        {
            //var initializer = null;//TODO
            //return new new GoogleAuthorizationCodeFlow(initializer);
            throw new NotImplementedException();
        }
    }

    public static class GoogleOAuthScopes
    {
        public const string Email= "https://www.googleapis.com/auth/userinfo.email";
    }

}
