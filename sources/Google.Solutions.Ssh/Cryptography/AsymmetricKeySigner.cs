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

using System;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// Signer for public key authentication.
    /// </summary>
    public interface IAsymmetricKeySigner : IDisposable
    {
        /// <summary>
        /// Public key that corresponds to the signing key.
        /// </summary>
        PublicKey PublicKey { get; }

        /// <summary>
        /// Sign an authentication challenge.
        /// </summary>
        /// <returns>Signature, in the format expected by SSH</returns>
        byte[] Sign(AuthenticationChallenge challenge);
    }

    /// <summary>
    /// Helper class for creating signers.
    /// </summary>
    public static class AsymmetricKeySigner
    {
        /// <summary>
        /// Create a signer that uses an in-memory key. For testing purposes.
        /// </summary>
        public static IAsymmetricKeySigner CreateEphemeral(
            SshKeyType sshKeyType)
        {
            return sshKeyType switch
            {
                SshKeyType.Rsa3072 => new RsaSigner(new RSACng(3072), true),
                SshKeyType.EcdsaNistp256 => new EcdsaSigner(new ECDsaCng(256), true),
                SshKeyType.EcdsaNistp384 => new EcdsaSigner(new ECDsaCng(384), true),
                SshKeyType.EcdsaNistp521 => new EcdsaSigner(new ECDsaCng(521), true),
                _ => throw new ArgumentOutOfRangeException(nameof(sshKeyType))
            };
        }

        /// <summary>
        /// Create a signer for an existing key.
        /// </summary>
        public static IAsymmetricKeySigner Create(CngKey key, bool ownsKey)
        {
            if (key.Algorithm == CngAlgorithm.Rsa)
            {
                return new RsaSigner(new RSACng(key), ownsKey);
            }
            else
            {
                return new EcdsaSigner(new ECDsaCng(key), ownsKey);
            }
        }
    }
}
