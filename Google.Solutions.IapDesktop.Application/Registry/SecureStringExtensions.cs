using System.Net;
using System.Security;

namespace Google.Solutions.IapDesktop.Application.Registry
{
    public static class SecureStringExtensions
    {
        public static string AsClearText(this SecureString secureString)
        {
            return new NetworkCredential(string.Empty, secureString).Password;
        }

        public static SecureString FromClearText(string plaintextString)
        {
            return new NetworkCredential(string.Empty, plaintextString).SecurePassword;
        }
    }
}
