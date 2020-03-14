using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Registry
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
