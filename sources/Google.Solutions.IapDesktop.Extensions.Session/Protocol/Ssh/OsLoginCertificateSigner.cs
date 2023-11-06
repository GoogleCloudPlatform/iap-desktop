using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh.Cryptography;

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
            byte[] certifiedPublicKey)
        {
            //
            // Use the same signer, but "replace" its public key
            // with the certified key.
            //
            this.underlyingSigner = underlyingSigner;
            this.PublicKey = new CertifiedPublicKey(
                $"{underlyingSigner.PublicKey.Type}-cert-v01@openssh.com",
                certifiedPublicKey);
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
