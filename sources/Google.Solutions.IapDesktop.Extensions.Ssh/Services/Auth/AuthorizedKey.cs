using Google.Solutions.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{

    public class AuthorizedKey
    {
        public KeyAuthorizationMethod KeyAuthorizationMethod { get; }
        public ISshKey Key { get; }
        public string Username { get; }

        private AuthorizedKey(
            ISshKey key,
            KeyAuthorizationMethod method,
            string posixUsername)
        {
            this.Key = key;
            this.KeyAuthorizationMethod = method;
            this.Username = posixUsername;
        }

        public static AuthorizedKey Create(
            string preferredUsername)
        {
            throw new NotImplementedException();
        }
    }

    public enum KeyAuthorizationMethod
    {
        Metadata,
        OsLogin
    }
}
