using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// A signer that uses a key that has been certified
    /// (i.e., turned into a certificate) by OS Login.
    /// </summary>
    public class OsLoginCertificateSigner : DisposableBase, IAsymmetricKeySigner
    {
        /// <summary>
        /// Underlying signer, typically a local key pair.
        /// </summary>
        private readonly IAsymmetricKeySigner underlyingSigner;

        public OsLoginCertificateSigner(
            IAsymmetricKeySigner underlyingSigner,
            string openSshFormattedCertifiedPublicKey) //TODO: test
        {
            //
            // Use the same signer, but "replace" its public key
            // with the certified key.
            //
            this.underlyingSigner = underlyingSigner;

            var parts = openSshFormattedCertifiedPublicKey.Split(' ');
            if (parts.Length != 2)
            {
                throw new FormatException(
                    "The key does not follow the OpenSSH format");
            }

            Debug.Assert(parts[0].EndsWith("-cert-v01@openssh.com"));

            this.PublicKey = new CertifiedPublicKey(
                parts[0],
                Convert.FromBase64String(parts[1]));
        }

        //---------------------------------------------------------------------
        // IAsymmetricKeySigner.
        //---------------------------------------------------------------------

        public PublicKey PublicKey { get; }

        public byte[] Sign(AuthenticationChallenge challenge)
        {
            return this.underlyingSigner.Sign(challenge);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            this.underlyingSigner.Dispose();
            this.PublicKey.Dispose();
            base.Dispose(disposing);
        }

        //---------------------------------------------------------------------
        // PublicKey.
        //---------------------------------------------------------------------

        private class CertifiedPublicKey : PublicKey
        {
            public CertifiedPublicKey(string type, byte[] wireFormatValue)
            {
                this.Type = type;
                this.WireFormatValue = wireFormatValue;
            }

            public override string Type { get; }

            public override byte[] WireFormatValue { get; }
        }
    }
}
