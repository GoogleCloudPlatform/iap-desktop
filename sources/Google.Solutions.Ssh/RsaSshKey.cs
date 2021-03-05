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
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh
{
    public sealed class RsaSshKey : ISshKey
    {
#if DEBUG
        private bool disposed = false;
#endif

        private readonly RSA key;

        public RsaSshKey(RSA key)
        {
            this.key = key;
        }

        public static RsaSshKey NewEphemeralKey()
        {
            return new RsaSshKey(new RSACng());
        }

        //---------------------------------------------------------------------
        // ISshKey.
        //---------------------------------------------------------------------

        public byte[] PublicKey => this.key.ToSshRsaPublicKey();

        public string PublicKeyString => Convert.ToBase64String(this.PublicKey);

        public string Type => "ssh-rsa";

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
