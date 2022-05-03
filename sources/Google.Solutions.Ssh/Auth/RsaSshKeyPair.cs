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

using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Format;
using Google.Solutions.Ssh.Native;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Auth
{
    public sealed class RsaSshKeyPair : ISshKeyPair
    {
#if DEBUG
        private bool disposed = false;
#endif

        private readonly RSA key;

        private RsaSshKeyPair(RSA key)
        {
            this.key = key;
        }

        public static RsaSshKeyPair FromKey(RSA key)
        {
            return new RsaSshKeyPair(key);
        }

        public static RsaSshKeyPair NewEphemeralKey(int keySize)
        {
            return new RsaSshKeyPair(new RSACng(keySize));
        }

        //---------------------------------------------------------------------
        // ISshKey.
        //---------------------------------------------------------------------

        public byte[] GetPublicKey()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                //
                // Encode public key according to RFC4253 section 6.6.
                //
                var parameters = key.ExportParameters(false);

                //
                // Pad modulus with a leading zero, 
                // cf https://www.cameronmoten.com/2017/12/21/rsacryptoserviceprovider-create-a-ssh-rsa-public-key/
                //
                var paddedModulus = (new byte[] { 0 })
                    .Concat(parameters.Modulus)
                    .ToArray();

                writer.WriteString(this.Type);
                writer.WriteMpint(parameters.Exponent);
                writer.WriteMpint(paddedModulus);
                writer.Flush();

                return buffer.ToArray();
            }
        }

        public string PublicKeyString => Convert.ToBase64String(GetPublicKey());

        public string Type => "ssh-rsa";

        public uint KeySize => (uint)this.key.KeySize;

        public byte[] SignData(byte[] data)
        {
            //
            // NB. Since we are using RSA, signing always needs to use
            // SHA-1 and PKCS#1, 
            // cf. https://tools.ietf.org/html/rfc4253#section-6.6
            //
            return this.key.SignData(
                data,
                HashAlgorithmName.SHA1,
                RSASignaturePadding.Pkcs1);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
#if DEBUG
            Debug.Assert(!this.disposed);
            this.disposed = true;
#endif

            this.key.Dispose();
        }
    }
}
