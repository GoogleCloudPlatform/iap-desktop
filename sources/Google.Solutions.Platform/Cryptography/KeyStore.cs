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
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Google.Solutions.Platform.Cryptography
{
    /// <summary>
    /// Adapter for the current user's Windows CNG key store.
    /// </summary>
    public interface IKeyStore
    {
        /// <summary>
        /// Provider that is used to manage keys.
        /// </summary>
        CngProvider Provider { get; }

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
        public KeyStore(CngProvider provider)
        {
            this.Provider = provider;
        }

        private bool CheckKeyExists(string name)
        {
            try
            {
                return CngKey.Exists(name);
            }
            catch (CryptographicException e) when (e.HResult == Ntstatus.NTE_TEMPORARY_PROFILE)
            {
                //
                // The Windows profile is marked as temporary ("mandatory"). In this state,
                // we can't access any persisted keys.
                //
                // FWIW, it's still possible to create ephemeral keys in this state.
                //
                // Note for testing: The following PowerShell [2] command turns a
                // profile read-only:
                //
                //   $USERSID='S-...' # SID of user
                //   Set-ItemProperty -path Registry::"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\$USERSID\" -Name State -Value 128
                //
                // [1] https://web.archive.org/web/20140217055725/http://support.microsoft.com/kb/264732.
                // [2] https://web.archive.org/web/20151218221641/https://ittechlog.wordpress.com/2014/06/27/switch-a-local-profile-to-temporary/
                //
                throw new KeyStoreUnavailableException(
                    "Accessing your local CNG key store failed because you are using a " +
                    "mandatory Windows user profile. Convert your user profile to " +
                    "a regular profile, or log in as a different Windows user.");
            }
        }

        internal static string GetKeyContainerPath(CngKey key)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Crypto\Keys",
                key.UniqueName);
        }

        //---------------------------------------------------------------------
        // IKeyStore.
        //---------------------------------------------------------------------

        public CngProvider Provider { get; }

        public CngKey OpenKey(
            IntPtr owner,
            string name,
            KeyType type,
            CngKeyUsages usage,
            bool forceCreate)
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

                if (!forceCreate && CheckKeyExists(name))
                {
                    var key = CngKey.Open(name);
                    if (key.Algorithm != type.Algorithm)
                    {
                        var foundAlgorithm = key.Algorithm;
                        key.Dispose();

                        throw new KeyConflictException(
                            $"Key {name} exists but uses algorithm {foundAlgorithm}");
                    }

                    if (key.KeySize != type.Size)
                    {
                        var foundSize = key.KeySize;
                        key.Dispose();

                        throw new KeyConflictException(
                            $"Key {name} exists but uses size {foundSize}");
                    }

                    //
                    // NB. The opened key could reside in a different provider,
                    // but that's ok.
                    //

                    if ((key.KeyUsage & usage) == 0)
                    {
                        key.Dispose();
                        throw new KeyConflictException(
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
                    //
                    // Store in user profile, overwrite if necessary.
                    //
                    KeyCreationOptions = forceCreate 
                        ? CngKeyCreationOptions.OverwriteExistingKey 
                        : CngKeyCreationOptions.None,

                    //
                    // Do not allow exporting.
                    //
                    ExportPolicy = CngExportPolicies.None,

                    Provider = this.Provider,
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
                        "Created new CNG key {0} in {1}", name, this.Provider.Provider);

                    return key;
                }
                catch (CryptographicException e) when (e.HResult == Ntstatus.NTE_EXISTS)
                {
                    //
                    // This obscure error can happen if:
                    //
                    //  - The key container is corrupted and
                    //  - The user has lost write-access to the key container (although they
                    //    might still be the file owner).
                    //
                    // In this state:
                    //
                    //  - CngKey.Exists() returns false (although the key container is there)
                    //  - Open() and Delete() consistantly fail with NTE_EXISTS
                    //  - certutil is unable to delete the key.
                    //
                    // The only remedy in that case is to delete the key container by
                    // deleting the file. Obviously, the problem is that we don't know the
                    // file name.
                    //
                    // To reproduce the issue, do the following:
                    //
                    //  1. Run `certutil -csp "Microsoft Software Key Storage Provider" -key -user`
                    //     to find out the name of the key container.
                    //  2. Go to %APPDATA%\Microsoft\Crypto\Keys and find the corresponding file.
                    //  3. Open the file and corrupt the content.
                    //  4. Update the file DACL so that it only contains 2 ACEs:
                    //
                    //       SYSTEM: read
                    //       Current user: read
                    //
                    throw new InvalidKeyContainerException(
                        "Failed to create or access cryptographic key. This might " +
                        "be caused by corrupted file permissions on the CNG key container.", 
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
            public const int NTE_TEMPORARY_PROFILE = unchecked((int)0x80090024);
        }
    }

    /// <summary>
    /// Indicates that an operation could not be performed because 
    /// of an existing conflicting key.
    /// </summary>
    public class KeyConflictException : CryptographicException
    {
        internal KeyConflictException(string message) 
            : base(message)
        {
        }

        internal KeyConflictException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Indicates that the key container is corrupt, inaccessible, or both.
    /// </summary>
    public class InvalidKeyContainerException : CryptographicException
    {
        internal InvalidKeyContainerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Indicates that the key store is unavailable.
    /// </summary>
    public class KeyStoreUnavailableException : CryptographicException
    {
        internal KeyStoreUnavailableException(string message) : base(message)
        {
        }
    }
}
