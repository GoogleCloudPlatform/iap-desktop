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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh.Format;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    public sealed class RsaSshKeyPair : DisposableBase, ISshKeyPair
    {
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
                var parameters = this.key.ExportParameters(false);

                writer.WriteString(this.Type);
                writer.WriteMultiPrecisionInteger(parameters.Exponent);
                writer.WriteMultiPrecisionInteger(parameters.Modulus);
                writer.Flush();

                return buffer.ToArray();
            }
        }

        public string PublicKeyString => Convert.ToBase64String(GetPublicKey());

        public string Type => "ssh-rsa";

        public uint KeySize => (uint)this.key.KeySize;

        public byte[] Sign(AuthenticationChallenge challenge)
        {
            //
            // NB. Before RFC 8332 (Use of RSA Keys with SHA-256 and SHA-512),
            // authenticating with an "rsa-ssh" key implied using SHA-1 and
            // PKCS#1 to create signatures.
            //
            // As of RFC 8332, we have to consider algorithm upgrades. If we
            // attempt to authenticate using an "rsa-ssh" key, the server is
            // likely to challenge us for an rsa-sha2-256 or rsa-sha2-512 signature.
            //
            // To find out what hash algorithm we need to use, we have to
            // inspect the (decoded) challenge.
            //

            HashAlgorithmName hashAlgorithm;
            switch (challenge.Algorithm)
            {
                case "ssh-rsa":
                    hashAlgorithm = HashAlgorithmName.SHA1;
                    break;

                case "rsa-sha2-256":
                    hashAlgorithm = HashAlgorithmName.SHA256;
                    break;

                case "rsa-sha2-512":
                    hashAlgorithm = HashAlgorithmName.SHA512;
                    break;

                default:
                    SshTraceSource.Log.TraceWarning(
                        "Received challenge for unrecognized algorithm {0}", 
                        challenge.Algorithm);

                    throw new ArgumentException("Unrecognized algorithm: " + challenge.Algorithm);
            }

            return this.key.SignData(
                challenge.Value,
                hashAlgorithm,
                RSASignaturePadding.Pkcs1);
        }

        //---------------------------------------------------------------------
        // Disposable.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.key.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
