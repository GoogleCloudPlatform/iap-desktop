//
// Copyright 2023 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Format;
using System.IO;
using System.Linq;
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

        public RsaPublicKey(RSA key)
        {
            this.key = key.ExpectNotNull(nameof(key));
        }

        public RsaPublicKey(byte[] encodedKey)
        {
            encodedKey.ExpectNotNull(nameof(encodedKey));

            using (var reader = new SshReader(new MemoryStream(encodedKey)))
            {
                var type = reader.ReadString();
                if (type != RsaType)
                {
                    throw new SshFormatException(
                        $"Expected {RsaType} in header, but got {type}");
                }

                //
                // Decode public key according to RFC4253 section 6.6.
                //

                var exponent = reader.ReadMultiPrecisionInteger().ToArray();
                var modulus = reader.ReadMultiPrecisionInteger().ToArray();

                this.key = new RSACng();

                try
                {
                    this.key.ImportParameters(new RSAParameters()
                    {
                        P = exponent,
                        Q = modulus
                    });
                }
                catch (CryptographicException e)
                {
                    throw new SshFormatException(
                        "The key contains malformed parameters", e);
                }
            }
        }

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

        protected override void Dispose(bool disposing)
        {
            this.key.Dispose();
            base.Dispose(disposing);
        }
    }
}
