using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
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
        Task<IOidcSession> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Authorize using a browser-based OIDC flow.
        /// </summary>
        Task<IOidcSession> AuthorizeAsync(
            CancellationToken cancellationToken);
    }

    public abstract class OidcClientBase : IOidcClient
    {
        private readonly IOidcOfflineCredentialStore store;

        protected IDeviceEnrollment DeviceEnrollment { get; } // TODO: Push down to GaiaClient

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

        public async Task<IOidcSession> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken)
        {
            if (this.store.TryRead(out var offlineCredential))
            {
                ApiTraceSources.Default.TraceVerbose(
                    "Attempting authorization using offline credential...");

                Debug.Assert(offlineCredential.RefreshToken != null);
                try
                {
                    var session = await
                        ActivateOfflineCredentialAsync(offlineCredential, cancellationToken)
                        .ConfigureAwait(false);
                    Debug.Assert(session != null);

                    //
                    // Update the offline credential as the refresh
                    // token and/or ID token might have changed.
                    //
                    this.store.Write(session.OfflineCredential);

                    return session;
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

        public async Task<IOidcSession> AuthorizeAsync(
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

        protected abstract Task<IOidcSession> AuthorizeWithBrowserAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);

        protected abstract Task<IOidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);
    }
}
