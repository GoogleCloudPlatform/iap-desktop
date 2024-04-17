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

using Google.Solutions.Ssh.Cryptography;
using System.Collections.Generic;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// Creates and caches ephemeral (i.e., in-memory) SSH signing keys.
    /// </summary>
    internal static class EphemeralKeySigners
    {
        private static readonly object signersLock = new object();
        private static readonly IDictionary<SshKeyType, IAsymmetricKeySigner> signers
            = new Dictionary<SshKeyType, IAsymmetricKeySigner>();

        public static IAsymmetricKeySigner Get(SshKeyType keyType)
        {
            //
            // Lazily create keys on first access, and never dispose them.
            //
            lock (signersLock)
            {
                if (!signers.TryGetValue(keyType, out var signer))
                {
                    signer = AsymmetricKeySigner.Create(
                        AsymmetricKeySigner.CreateSigningKey(keyType),
                        false);

                    signers[keyType] = signer;
                }

                return signer;
            }
        }

        internal static void ClearCache()
        {
            lock (signersLock)
            {
                foreach (var signer in signers.Values)
                {
                    signer.Dispose();
                }

                signers.Clear();
            }
        }
    }
}
