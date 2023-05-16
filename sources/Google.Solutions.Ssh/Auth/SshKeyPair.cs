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

using Google.Solutions.Common.Diagnostics;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Auth
{
    public enum SshKeyType : int
    {
        //
        // NB. These values are used for persistence and
        // must be kept constant.
        //

        [Display(Name = "RSA (3072 bit)")]
        Rsa3072 = 0x01,

        [Display(Name = "ECDSA NIST P-256")]
        EcdsaNistp256 = 0x11,

        [Display(Name = "ECDSA NIST P-384")]
        EcdsaNistp384 = 0x12,

        [Display(Name = "ECDSA NIST P-521")]
        EcdsaNistp521 = 0x13
    }

    public static class SshKeyPair
    {
        private const int NTE_EXISTS = -2146893809; // 0x8009000F

        private static CngKey OpenPersistentKeyPair(
            string name,
            CngAlgorithm algorithm,
            CngProvider provider,
            CngKeyUsages usage,
            int keySize,
            bool createNewIfNotExists,
            IntPtr parentWindowHandle)
        {
            using (SshTraceSources.Default.TraceMethod()
                .WithParameters(name, provider.Provider, algorithm.Algorithm, keySize))
            {
                //
                // Create or open CNG key in the user profile.
                //
                // NB. Keys are stored in %APPDATA%\Microsoft\Crypto\Keys when using
                // the default Microsoft Software Key Storage Provider.
                // (see https://docs.microsoft.com/en-us/windows/win32/seccng/key-storage-and-retrieval)
                //
                // For testing, you can list CNG keys using
                // certutil -csp "Microsoft Software Key Storage Provider" -key -user
                //

                if (CngKey.Exists(name))
                {
                    var key = CngKey.Open(name);
                    if (key.Algorithm != algorithm)
                    {
                        var foundAlgorithm = key.Algorithm;
                        key.Dispose();

                        throw new CryptographicException(
                            $"Key {name} exists but uses algorithm {foundAlgorithm}");
                    }

                    //
                    // NB. The opened key could reside in a different provider,
                    // but that's ok.
                    //

                    if ((key.KeyUsage & usage) == 0)
                    {
                        key.Dispose();
                        throw new CryptographicException(
                            $"Key {name} exists, but does not support requested usage");

                    }

                    SshTraceSources.Default.TraceInformation(
                        "Found existing CNG key {0} in {1}", name, key.Provider.Provider);

                    return key;
                }

                if (createNewIfNotExists)
                {
                    var keyParams = new CngKeyCreationParameters
                    {
                        // Do not overwrite, store in user profile.
                        KeyCreationOptions = CngKeyCreationOptions.None,

                        // Do not allow exporting.
                        ExportPolicy = CngExportPolicies.None,

                        Provider = provider,
                        KeyUsage = usage
                    };

                    //
                    // NB. If we're using the Smart Card provider, the key store
                    // might show a UI dialog. Therefore, this method must
                    // be run on the UI thread.
                    //
                    if (parentWindowHandle != IntPtr.Zero)
                    {
                        keyParams.ParentWindowHandle = parentWindowHandle;
                    }

                    if (algorithm == CngAlgorithm.Rsa)
                    {
                        keyParams.Parameters.Add(
                            new CngProperty(
                                "Length",
                                BitConverter.GetBytes(keySize),
                                CngPropertyOptions.None));
                    }

                    //
                    // Create the key. 
                    //
                    try
                    {
                        var key = CngKey.Create(
                            algorithm,
                            name,
                            keyParams);

                        Debug.Assert(key.KeySize == keySize);

                        SshTraceSources.Default.TraceInformation(
                            "Created new CNG key {0} in {1}", name, provider.Provider);

                        return key;
                    }
                    catch (CryptographicException e) when (e.HResult == NTE_EXISTS)
                    {
                        //
                        // This should not happen because of the previous Exists()
                        // check, but:
                        //
                        //  - There might be a race condition (rare)
                        //  - The specific algorithm might be disabled on the machine
                        //    (also rare).
                        //
                        throw new CryptographicException(
                            "Failed to create or access cryptographic key. If the error " +
                            $"persists, try using an algorithm other than {algorithm}.", e);
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Create or open a key in the key storage provider.
        /// </summary>
        public static ISshKeyPair OpenPersistentKeyPair(
            string name,
            SshKeyType sshKeyType,
            CngProvider provider,
            CngKeyUsages usage,
            bool createNewIfNotExists,
            IntPtr parentWindowHandle)
        {
            switch (sshKeyType)
            {
                case SshKeyType.Rsa3072:
                    {
                        var key = OpenPersistentKeyPair(
                            name,
                            CngAlgorithm.Rsa,
                            provider,
                            usage,
                            3072,
                            createNewIfNotExists,
                            parentWindowHandle);

                        return key != null
                            ? RsaSshKeyPair.FromKey(new RSACng(key))
                            : null;
                    }

                case SshKeyType.EcdsaNistp256:
                    {
                        var key = OpenPersistentKeyPair(
                            name,
                            CngAlgorithm.ECDsaP256,
                            provider,
                            usage,
                            256,
                            createNewIfNotExists,
                            parentWindowHandle);

                        return key != null
                            ? ECDsaSshKeyPair.FromKey(new ECDsaCng(key))
                            : null;
                    }

                case SshKeyType.EcdsaNistp384:
                    {
                        var key = OpenPersistentKeyPair(
                            name,
                            CngAlgorithm.ECDsaP384,
                            provider,
                            usage,
                            384,
                            createNewIfNotExists,
                            parentWindowHandle);

                        return key != null
                            ? ECDsaSshKeyPair.FromKey(new ECDsaCng(key))
                            : null;
                    }

                case SshKeyType.EcdsaNistp521:
                    {
                        var key = OpenPersistentKeyPair(
                            name,
                            CngAlgorithm.ECDsaP521,
                            provider,
                            usage,
                            521,
                            createNewIfNotExists,
                            parentWindowHandle);

                        return key != null
                            ? ECDsaSshKeyPair.FromKey(new ECDsaCng(key))
                            : null;
                    }

                default:
                    throw new ArgumentException("Unsupported key type");
            }
        }

        /// <summary>
        /// Delete a key from the key storage provider.
        /// </summary>
        public static void DeletePersistentKeyPair(string name)
        {
            using (SshTraceSources.Default.TraceMethod().WithParameters(name))
            {
                if (CngKey.Exists(name))
                {
                    CngKey.Open(name).Delete();
                }
            }
        }

        /// <summary>
        /// Create a new in-memory key for testing purposes.
        /// </summary>
        public static ISshKeyPair NewEphemeralKeyPair(SshKeyType sshKeyType)
        {
            switch (sshKeyType)
            {
                case SshKeyType.Rsa3072:
                    return RsaSshKeyPair.NewEphemeralKey(3072);

                case SshKeyType.EcdsaNistp256:
                    return ECDsaSshKeyPair.NewEphemeralKey(256);

                case SshKeyType.EcdsaNistp384:
                    return ECDsaSshKeyPair.NewEphemeralKey(384);

                case SshKeyType.EcdsaNistp521:
                    return ECDsaSshKeyPair.NewEphemeralKey(521);

                default:
                    throw new ArgumentException("Unsupported key type");
            }
        }
    }
}
