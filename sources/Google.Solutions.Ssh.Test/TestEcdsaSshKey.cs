using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestEcdsaSshKey
    {
        [Test]
        public void __()
        {
            var keyName = "__test-ecdsa";

            if (!CngKey.Exists(keyName))
            {
                var keyParams = new CngKeyCreationParameters
                {
                    // Do not overwrite, store in user profile.
                    KeyCreationOptions = CngKeyCreationOptions.None,

                    // Do not allow exporting.
                    ExportPolicy = CngExportPolicies.AllowExport,

                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    KeyUsage = CngKeyUsages.AllUsages
                };

                //keyParams.Parameters.Add(
                //    new CngProperty(
                //        "Length",
                //        BitConverter.GetBytes(this.keySize),
                //        CngPropertyOptions.None));

                //
                // Create the key. 
                //
                CngKey.Create(
                    CngAlgorithm.ECDsaP256,
                    keyName,
                    keyParams);
            }


            var key = new EcdsaSshKey(new ECDsaCng(CngKey.Open(keyName)));
            var s = key.PublicKeyString;

            Assert.AreEqual(
                "AAAAE2VjZHNhLXNoYTItbmlzdHAyNTYAAAAIbmlzdHAyNTYAAABBBNytnb4TNvoGKthqO/SmIDI5Qj16D9TrmVlOKybXCjBB8DKzYEVcVzAJHoSHEoZKxIAbgyKaa6u+Q7ezJwDM4Fw=",
                s);
        }
    }
}
