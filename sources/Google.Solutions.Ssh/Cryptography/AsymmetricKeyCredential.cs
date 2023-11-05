using System;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    internal static class AsymmetricKeyCredential
    {
        /// <summary>
        /// Create a new in-memory key for testing purposes.
        /// </summary>
        public static IAsymmetricKeyCredential CreateEphemeral(
            SshKeyType sshKeyType)
        {
            return sshKeyType switch
            {
                SshKeyType.Rsa3072 => new RsaKeyCredential(new RSACng(3072)),
                SshKeyType.EcdsaNistp256 => new EcdsaKeyCredential(new ECDsaCng(256)),
                SshKeyType.EcdsaNistp384 => new EcdsaKeyCredential(new ECDsaCng(384)),
                SshKeyType.EcdsaNistp521 => new EcdsaKeyCredential(new ECDsaCng(521)),
                _ => throw new ArgumentOutOfRangeException(nameof(sshKeyType))
            };
        }
    }
}
