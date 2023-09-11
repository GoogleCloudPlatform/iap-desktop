using Google.Solutions.Ssh.Format;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// An RSA public key.
    /// </summary>
    public class RsaPublicKey : PublicKey
    {
        //
        // NB. The key type is always "rsa-ssh", although
        // the algorithm might be rsa-sha2-256 or rsa-sha2-512.
        // 
        private const string RsaType = "rsa-ssh";

        private readonly RSA key;

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Type => RsaType;

        public override byte[] Value
        {
            get
            {
                using (var buffer = new MemoryStream())
                using (var writer = new SshWriter(buffer))
                {
                    //
                    // Encode public key according to RFC4253 section 6.6.
                    //
                    var parameters = this.key.ExportParameters(false);

                    writer.WriteString(this.Type);
                    writer.WriteMultiPrecisionInteger(parameters.Exponent);
                    writer.WriteMultiPrecisionInteger(parameters.Modulus);
                    writer.Flush();

                    return buffer.ToArray();
                }
            }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        //public bool TryParse(string s, out RsaPublicKey result)
        //{
        //    using (var buffer = new MemoryStream(Convert.FromBase64String(s))
        //    using (var reader = new SshReader(buffer))
        //    {
        //        var type = reader.ReadString();
        //        if (type != RsaType)
        //        {
        //        }
        //    }
        //}
    }
}
