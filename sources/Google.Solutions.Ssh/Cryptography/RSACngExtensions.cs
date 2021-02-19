//
// Copyright 2020 Google LLC
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

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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
        public static byte[] ToSshRsaPublicKey(this RSA key, bool puttyCompatible = true)
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

                if (puttyCompatible)
                {
                    // Add a leading zero, 
                    // cf https://www.cameronmoten.com/2017/12/21/rsacryptoserviceprovider-create-a-ssh-rsa-public-key/
                    buffer.Write(ToBytes(modulus.Length + 1), 0, 4);
                    buffer.Write(new byte[] { 0 }, 0, 1);
                }
                else
                {
                    buffer.Write(ToBytes(modulus.Length), 0, 4);
                }
                buffer.Write(modulus, 0, modulus.Length);

                buffer.Flush();

                return buffer.ToArray();
            }
        }
    }
}
