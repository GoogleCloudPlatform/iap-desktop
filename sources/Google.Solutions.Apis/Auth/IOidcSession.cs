using Google.Apis.Auth.OAuth2;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// OpenID Connect session. A session is backed by a refresh token, 
    /// but it may expire due to the 'Google Cloud Session Lenth' control.
    /// </summary>
    public interface IOidcSession 
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
}
