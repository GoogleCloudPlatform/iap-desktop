using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Persistent storage for offline credentials.
    /// </summary>
    public interface IOidcOfflineCredentialStore
    {
        /// <summary>
        /// Try to load an offline credential.
        /// </summary>
        bool TryRead(out OidcOfflineCredential credential);

        /// <summary>
        /// Store offline credential.
        /// </summary>
        void Write(OidcOfflineCredential credential);

        /// <summary>
        /// Delete all offline credentials.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Offline credential that permits silent extension of an existing
    /// OAuth session.
    /// </summary>
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
    }
}
