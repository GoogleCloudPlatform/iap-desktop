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

using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// A signer that uses a key that has been certified
    /// (i.e., turned into a certificate) by OS Login.
    /// </summary>
    internal class OsLoginCertificateSigner : DisposableBase, IAsymmetricKeySigner
    {
        /// <summary>
        /// Underlying signer, typically a local key pair.
        /// </summary>
        private readonly IAsymmetricKeySigner underlyingSigner;

        public OsLoginCertificateSigner(
            IAsymmetricKeySigner underlyingSigner,
            string openSshFormattedCertifiedPublicKey)
        {
            //
            // Use the same signer, but "replace" its public key
            // with the certified key.
            //
            this.underlyingSigner = underlyingSigner;

            //
            // Parse the key, which is in the format:
            //
            //  [type] [base64-blob] [username]
            //
            // It's safe to assume that the server includes
            // a username, see b/309752006.
            //
            var parts = openSshFormattedCertifiedPublicKey.Split(' ');
            if (parts.Length < 3)
            {
                throw new FormatException(
                    "The key does not follow the OpenSSH format");
            }

            Debug.Assert(parts[0].EndsWith("-cert-v01@openssh.com"));

            this.PublicKey = new CertifiedPublicKey(
                parts[0],
                Convert.FromBase64String(parts[1]));
            this.Username = parts[2];
        }

        internal string Username { get; }

        //---------------------------------------------------------------------
        // IAsymmetricKeySigner.
        //---------------------------------------------------------------------

        public PublicKey PublicKey { get; }

        public byte[] Sign(AuthenticationChallenge challenge)
        {
            return this.underlyingSigner.Sign(challenge);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            this.underlyingSigner.Dispose();
            this.PublicKey.Dispose();
            base.Dispose(disposing);
        }

        //---------------------------------------------------------------------
        // PublicKey.
        //---------------------------------------------------------------------

        private class CertifiedPublicKey : PublicKey
        {
            public CertifiedPublicKey(string type, byte[] wireFormatValue)
            {
                this.Type = type;
                this.WireFormatValue = wireFormatValue;
            }

            public override string Type { get; }

            public override byte[] WireFormatValue { get; }
        }
    }
}
