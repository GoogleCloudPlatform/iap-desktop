

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Util;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// OIDC authorization for this app.
    /// </summary>
    public interface IOidcAuthorization  // TOOD: Push IAuthorization down to App project, inherit from this
    {
        /// <summary>
        /// ID token for the signed-in user.
        /// Not null.
        /// </summary>
        IJsonWebToken IdToken { get; }

        /// <summary>
        /// Credential to use for Google API calls.
        /// Not null.
        /// </summary>
        ICredential ApiCredential { get; }

        /// <summary>
        /// Device enrollment. Not null.
        /// </summary>
        IDeviceEnrollment DeviceEnrollment { get; }
    }

    public class OidcOfflineCredential
    {
        /// <summary>
        /// Refresh token, not null.
        /// </summary>
        public string RefreshToken { get; }

        /// <summary>
        /// ID token, optional.
        /// </summary>
        public string IdToken { get; }

        public OidcOfflineCredential(string refreshToken, string idToken)
        {
            this.RefreshToken = refreshToken.ExpectNotEmpty(nameof(refreshToken));
            this.IdToken = idToken;
        }

        //TODO: Store browser preference?
    }

    public interface IOidcOfflineCredentialStore
    {
        bool TryRead(out OidcOfflineCredential credential);
        void Write(OidcOfflineCredential credential);
        void Clear();
    }
}
