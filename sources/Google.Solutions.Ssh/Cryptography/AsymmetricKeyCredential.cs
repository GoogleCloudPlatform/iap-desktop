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
            string username,
            SshKeyType sshKeyType)
        {
            return sshKeyType switch
            {
                SshKeyType.Rsa3072 => new RsaKeyCredential(
                    username,
                    new RSACng(3072)),

                SshKeyType.EcdsaNistp256 => new EcdsaKeyCredential(
                    username,
                    new ECDsaCng(256)),

                SshKeyType.EcdsaNistp384 => new EcdsaKeyCredential(
                    username,
                    new ECDsaCng(384)),

                SshKeyType.EcdsaNistp521 => new EcdsaKeyCredential(
                    username,
                    new ECDsaCng(521)),

                _ => throw new ArgumentOutOfRangeException(nameof(sshKeyType))
            };
        }
    }
}
