using Google.Apis.Util;
using Google.Solutions.Ssh.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Source of authentication information.
    /// </summary>
    public interface ISshAuthenticator
    {
        /// <summary>
        /// Username.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Key pair for public/private key authentication.
        /// </summary>
        ISshKeyPair KeyPair { get; }
        
        /// <summary>
        /// Prompt for additional (second factor)
        /// information.
        /// </summary>
        string Prompt(
            string name,
            string instruction,
            string prompt,
            bool echo);
    }

    /// <summary>
    /// Authenticator that uses a public key and doesn't
    /// support 2FA.
    /// </summary>
    public class SshSingleFactorAuthenticator : ISshAuthenticator
    {
        public string Username { get; }

        public ISshKeyPair KeyPair { get; }

        public SshSingleFactorAuthenticator(
            string username,
            ISshKeyPair keyPair)
        {
            this.Username = username.ThrowIfNull(nameof(username));
            this.KeyPair = keyPair.ThrowIfNull(nameof(KeyPair));
        }

        public virtual string Prompt(
            string name, 
            string instruction, 
            string prompt, 
            bool echo)
        {
            throw new NotImplementedException();
        }
    }
}
