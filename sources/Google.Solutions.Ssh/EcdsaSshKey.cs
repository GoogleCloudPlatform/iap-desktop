//
// Copyright 2021 Google LLC
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
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh
{
    public sealed class ECDsaSshKey : ISshKey
    {
#if DEBUG
        private bool disposed = false;
#endif

        private readonly ECDsaCng key;

        private ECDsaSshKey(ECDsaCng key)
        {
            Debug.Assert(key.KeySize == 256 ||
                         key.KeySize == 384 ||
                         key.KeySize == 521);

            this.key = key;
        }
        public static ECDsaSshKey FromKey(ECDsaCng key)
        {
            return new ECDsaSshKey(key);
        }

        public static ECDsaSshKey NewEphemeralKey(int keySize)
        {
            return new ECDsaSshKey(new ECDsaCng(keySize));
        }

        private HashAlgorithmName HashAlgorithm
        {
            get
            {
                //
                // The hashing algorithm to use depends on the key size,
                // cf rfc5656 6.2.1:
                //
                // +----------------+----------------+
                // |   Curve Size   | Hash Algorithm |
                // +----------------+----------------+
                // |    b <= 256    |     SHA-256    |
                // | 256 < b <= 384 |     SHA-384    |
                // |     384 < b    |     SHA-512    |
                // +----------------+----------------+
                //
                if (key.KeySize <= 256)
                {
                    return HashAlgorithmName.SHA256;
                }
                else if (key.KeySize <= 384)
                {
                    return HashAlgorithmName.SHA384;
                }
                else
                {
                    return HashAlgorithmName.SHA512;
                }
            }
        }

        //---------------------------------------------------------------------
        // ISshKey.
        //---------------------------------------------------------------------

        public byte[] GetPublicKey()
        {
            using (var writer = new SshDataBuffer())
            {
                //
                // Encode public key according to RFC5656 section 3.1.
                //

                writer.WriteString(this.Type);
                writer.WriteString("nistp" + this.key.KeySize);
                writer.WriteBytes(this.key.EncodePublicKey());
                return writer.ToArray();
            }
        }

        public string PublicKeyString => Convert.ToBase64String(GetPublicKey());

        public string Type => "ecdsa-sha2-nistp" + this.key.KeySize;

        public uint KeySize => (uint)this.key.KeySize;

        public byte[] SignData(byte[] data)
        {
            //
            // NB. The signature returned by CNG is formatted according to
            // ISO/IEC 7816-8 / IEEE P1363. This is not the format SSH uses.
            //
            // Later .NET versions support alternate signature formats,
            // but in .NET 4.x, we must convert the format ourselves.
            //
            var signature = ECDsaSignature.FromIeee1363(
                this.key.SignData(
                    data, 
                    this.HashAlgorithm));

            return signature.ToSshBlob();
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
