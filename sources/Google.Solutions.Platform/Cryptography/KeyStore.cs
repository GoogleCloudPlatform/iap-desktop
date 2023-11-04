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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.Platform;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Google.Solutions.Platform.Cryptography
{
    /// <summary>
    /// Adapter for the Windows CNG key store.
    /// </summary>
    public interface IKeyStore
    {
        /// <summary>
        /// Open or create a persistent key.
        /// </summary>
        CngKey OpenKey(
            IntPtr owner,
            string name,
            KeyType type,
            CngKeyUsages usage,
            bool forceRecreate);

        /// <summary>
        /// Delete a persistent key.
        /// </summary>
        void DeleteKey(string name);
    }

    public class KeyStore : IKeyStore
    {
        private readonly CngProvider provider;

        public KeyStore(CngProvider provider)
        {
            this.provider = provider;
        }

        //---------------------------------------------------------------------
        // IKeyStore.
        //---------------------------------------------------------------------

        public CngKey OpenKey(
            IntPtr owner,
            string name,
            KeyType type,
            CngKeyUsages usage,
            bool forceRecreate)
        {
            name.ExpectNotEmpty(nameof(name));

            using (PlatformTraceSource.Log.TraceMethod().WithParameters(name, type))
            {
                //
                // Create or open CNG key in the user profile.
                //
                // NB. Keys are stored in %APPDATA%\Microsoft\Crypto\Keys when using
                // the default Microsoft Software Key Storage Provider.
                // (see https://docs.microsoft.com/en-us/windows/win32/seccng/key-storage-and-retrieval)
                //
                // We can list CNG keys using the following command:
                //
                //   certutil -csp "Microsoft Software Key Storage Provider" -key -user
                //

                if (!forceRecreate && CngKey.Exists(name))
                {
                    var key = CngKey.Open(name);
                    if (key.Algorithm != type.Algorithm)
                    {
                        var foundAlgorithm = key.Algorithm;
                        key.Dispose();

                        throw new CryptographicException(
                            $"Key {name} exists but uses algorithm {foundAlgorithm}");
                    }

                    if (key.KeySize != type.Size)
                    {
                        var foundSize = key.KeySize;
                        key.Dispose();

                        throw new CryptographicException(
                            $"Key {name} exists but uses size {foundSize}");
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

                    PlatformTraceSource.Log.TraceInformation(
                        "Found existing CNG key {0} in {1}", name, key.Provider.Provider);

                    return key;
                }

                //
                // Key not found, create new.
                //
                var keyParams = new CngKeyCreationParameters
                {
                    // Do not overwrite, store in user profile.
                    KeyCreationOptions = CngKeyCreationOptions.None,

                    // Do not allow exporting.
                    ExportPolicy = CngExportPolicies.None,

                    Provider = this.provider,
                    KeyUsage = usage
                };

                //
                // NB. If we're using the Smart Card provider, the key store
                // might show a UI dialog. Therefore, this method must
                // be run on the UI thread.
                //
                if (owner != IntPtr.Zero)
                {
                    keyParams.ParentWindowHandle = owner;
                }

                if (type.Algorithm == CngAlgorithm.Rsa)
                {
                    keyParams.Parameters.Add(
                        new CngProperty(
                            "Length",
                            BitConverter.GetBytes((int)type.Size),
                            CngPropertyOptions.None));
                }

                try
                {
                    var key = CngKey.Create(
                        type.Algorithm,
                        name,
                        keyParams);

                    Debug.Assert(key.KeySize == type.Size);

                    PlatformTraceSource.Log.TraceInformation(
                        "Created new CNG key {0} in {1}", name, provider.Provider);

                    return key;
                }
                catch (CryptographicException e) when (e.HResult == Ntstatus.NTE_EXISTS)
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
                        $"persists, try using an algorithm other than {type.Algorithm}.", 
                        e);
                }
            }
        }

        public void DeleteKey(string name)
        {
            using (PlatformTraceSource.Log.TraceMethod().WithParameters(name))
            {
                if (CngKey.Exists(name))
                {
                    CngKey.Open(name).Delete();
                }
            }
        }

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        private static class Ntstatus
        {
            public const int NTE_EXISTS = unchecked((int)0x8009000F);
        }
    }
}
