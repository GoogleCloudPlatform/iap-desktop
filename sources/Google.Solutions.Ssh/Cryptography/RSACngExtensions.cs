using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Cryptography
{
    public static class RSACngExtensions
    {
        private static byte[] ToBytes(int i)
        {
            var bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Export the public key in a format compliant with
        /// https://tools.ietf.org/html/rfc4253#section-6.6
        /// </summary>
        public static byte[] ToSshPublicKey(this RSACng key)
        {
            var prefix = "ssh-rsa";

            var prefixEncoded = Encoding.ASCII.GetBytes(prefix);
            var modulus = key.ExportParameters(false).Modulus;
            var exponent = key.ExportParameters(false).Exponent;

            using (var buffer = new MemoryStream())
            {
                buffer.Write(ToBytes(prefixEncoded.Length), 0, 4);
                buffer.Write(prefixEncoded, 0, prefixEncoded.Length);
                
                buffer.Write(ToBytes(exponent.Length), 0, 4);
                buffer.Write(exponent, 0, exponent.Length);
                
                buffer.Write(ToBytes(modulus.Length), 0, 4); 
                buffer.Write(modulus, 0, modulus.Length);

                buffer.Flush();

                return buffer.ToArray();
            }
        }
    }
}
