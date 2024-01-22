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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// Name of CNG key to use for SSH authentication.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    internal class CngKeyName
    {
        public string Value { get; }

        public CngKeyName(
            IOidcSession session,
            SshKeyType keyType,
            CngProvider provider)
        {
            session.ExpectNotNull(nameof(session));
            provider.ExpectNotNull(nameof(provider));

            this.Type = keyType switch
            {
                SshKeyType.Rsa3072 => new KeyType(CngAlgorithm.Rsa, 3072),
                SshKeyType.EcdsaNistp256 => new KeyType(CngAlgorithm.ECDsaP256, 256),
                SshKeyType.EcdsaNistp384 => new KeyType(CngAlgorithm.ECDsaP384, 384),
                SshKeyType.EcdsaNistp521 => new KeyType(CngAlgorithm.ECDsaP521, 521),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType))
            };

            if (keyType == SshKeyType.Rsa3072 &&
                provider == CngProvider.MicrosoftSoftwareKeyStorageProvider)
            {
                //
                // Use backwards-compatible name.
                //
                this.Value = $"IAPDESKTOP_{session.Username}";
            }
            else
            {
                //
                // Embed the key type and provider in the name. 
                //
                // CNG key names aren't scoped to a provider. By embedding the
                // provider name in the key, we ensure that we don't accidentally
                // use a key from a provider different from the one we're 
                // expecting to use.
                //
                // NB. Use SHA256.Create for FIPS-awareness.
                //
                using (var sha = SHA256.Create())
                {
                    //
                    // Instead of using the full provider name (which can be
                    // very long), hash the name and use the prefix.
                    //
                    var providerToken = BitConverter.ToString(
                        sha.ComputeHash(Encoding.UTF8.GetBytes(provider.Provider)),
                        0,
                        4).Replace("-", string.Empty);

                    this.Value = $"IAPDESKTOP_{session.Username}_{keyType:x}_{providerToken}";
                }
            }
        }

        public KeyType Type { get; }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
