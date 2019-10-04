using Google.Apis.Auth.OAuth2;

namespace Google.Solutions.CloudIap
{
    internal class OAuthClient
    {
        internal static readonly ClientSecrets Secrets = new ClientSecrets()
        {
            ClientId = "<<your-client-id-here>>",
            ClientSecret = "<<your-client-secret-here>>"
        };
    }
}
