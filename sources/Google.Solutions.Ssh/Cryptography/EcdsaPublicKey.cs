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
using System;
using System.IO;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// An ECDsa public key.
    /// </summary>
    public class EcdsaPublicKey : PublicKey
    {
        private readonly ECDsaCng key;
        private readonly bool ownsKey;

        public EcdsaPublicKey(ECDsaCng key, bool ownsKey)
        {
            this.key = key.ExpectNotNull(nameof(key));
            this.ownsKey = ownsKey;
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        private static ECParameters DecodeParametersFromWireFormat(byte[] encodedKey)
        {
            encodedKey.ExpectNotNull(nameof(encodedKey));

            using (var reader = new SshReader(new MemoryStream(encodedKey)))
            {
                try
                {
                    var type = reader.ReadString();

                    ECCurve curve;
                    ushort keySizeInBits;
                    string expectedIdentifier;

                    switch (type)
                    {
                        case "ecdsa-sha2-nistp256":
                            curve = ECCurve.NamedCurves.nistP256;
                            expectedIdentifier = "nistp256";
                            keySizeInBits = 256;
                            break;

                        case "ecdsa-sha2-nistp384":
                            curve = ECCurve.NamedCurves.nistP384;
                            expectedIdentifier = "nistp384";
                            keySizeInBits = 384;
                            break;

                        case "ecdsa-sha2-nistp521":
                            curve = ECCurve.NamedCurves.nistP521;
                            expectedIdentifier = "nistp521";
                            keySizeInBits = 521;
                            break;

                        default:
                            throw new SshFormatException(
                                "The key is not an ECDSA key or uses " +
                                $"an unsupported curve: {type}");
                    }

                    var idenfifier = reader.ReadString();
                    if (idenfifier != expectedIdentifier)
                    {
                        throw new SshFormatException(
                            "The key contains an unexpected identifier: " +
                            $"{idenfifier} (expected: {expectedIdentifier})");
                    }

                    var q = ECPointEncoding.Decode(
                        reader.ReadByteString(),
                        keySizeInBits);
                    return new ECParameters()
                    {
                        Q = q,
                        Curve = curve
                    };
                }
                catch (IOException e)
                {
                    throw new SshFormatException(
                        "The key encoding is malformed or truncated", e);
                }
            }
        }

        /// <summary>
        /// Read a key from its wire format (i.e., RFC5656 section 3.1).
        /// </summary>
        public static EcdsaPublicKey FromWireFormat(byte[] encodedKey)
        {
            encodedKey.ExpectNotNull(nameof(encodedKey));

            var parameters = DecodeParametersFromWireFormat(encodedKey);
            var key = new ECDsaCng(parameters.Curve);

            try
            {
                key.ImportParameters(parameters);
                return new EcdsaPublicKey(key, true);
            }
            catch (Exception e)
            {
                key.Dispose();
                throw new SshFormatException(
                    "The key contains malformed parameters", e);
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Type
        {
            get => "ecdsa-sha2-nistp" + this.key.KeySize;
        }

        public override byte[] WireFormatValue
        {
            get
            {
                using (var buffer = new MemoryStream())
                using (var writer = new SshWriter(buffer))
                {
                    //
                    // Encode public key according to RFC5656 section 3.1.
                    //

                    var qInUncompressedEncoding = ECPointEncoding.Encode(
                        this.key.ExportParameters(false).Q,
                        (ushort)this.key.KeySize);

                    writer.WriteString(this.Type);
                    writer.WriteString("nistp" + this.key.KeySize);
                    writer.WriteString(qInUncompressedEncoding);
                    writer.Flush();

                    return buffer.ToArray();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (this.ownsKey)
            {
                this.key.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
