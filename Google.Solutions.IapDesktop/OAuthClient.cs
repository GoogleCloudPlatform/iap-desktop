using Google.Apis.Auth.OAuth2;

namespace Google.Solutions.IapDesktop
{
    internal class OAuthClient
    {
        internal static readonly ClientSecrets Secrets = new ClientSecrets()
        {
            // Unverified credentials
            //ClientId = "155714704081-fb9ggkqt2bfuhsm8dh6p7c728j6goqm9.apps.googleusercontent.com",
            //ClientSecret = "yxEdC7xuTtl410eniYpLeVJ7"

            // Verified credentials
            ClientId = "78381520511-4fu6ve6b49kknk3dkdnpudoi0tivq6jn.apps.googleusercontent.com",
            ClientSecret = "wWPmQnmBTmjyMMintGTSSypE"
        };
    }
}
