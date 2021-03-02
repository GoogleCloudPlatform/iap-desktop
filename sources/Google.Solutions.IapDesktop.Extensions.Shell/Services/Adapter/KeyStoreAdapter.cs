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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter
{
    public interface IKeyStoreAdapter
    {
        RSA CreateRsaKey(
            string name,
            CngKeyUsages usage,
            bool createNewIfNotExists,
            IWin32Window window);
    }

    [Service(typeof(IKeyStoreAdapter))]
    public class KeyStoreAdapter : IKeyStoreAdapter
    {
        private const int DefaultKeySize = 3072;

        // TODO: Make provider, key size configurable.
        private readonly CngProvider provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;
        private readonly int keySize = DefaultKeySize;

        public KeyStoreAdapter()
        {
        }

        //---------------------------------------------------------------------
        // IKeyStoreAdapter
        //---------------------------------------------------------------------

        public RSA CreateRsaKey(
            string name,
            CngKeyUsages usage,
            bool createNewIfNotExists,
            IWin32Window window)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
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
                    if (key.Algorithm != CngAlgorithm.Rsa)
                    {
                        key.Dispose();
                        throw new CryptographicException(
                            $"Key {name} is not an RSA key");
                    }

                    if ((key.KeyUsage & usage) == 0)
                    {
                        key.Dispose();
                        throw new CryptographicException(
                            $"Key {name} exists, but does not support requested usage");

                    }

                    ApplicationTraceSources.Default.TraceInformation(
                        "Found existing CNG key {0} in {1}", name, this.provider);

                    return new RSACng(key);
                }

                if (createNewIfNotExists)
                {
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
                    if (window != null && window.Handle != null)
                    {
                        keyParams.ParentWindowHandle = window.Handle;
                    }

                    keyParams.Parameters.Add(
                        new CngProperty(
                            "Length",
                            BitConverter.GetBytes(this.keySize),
                            CngPropertyOptions.None));

                    //
                    // Create the key. 
                    //
                    var key = new RSACng(CngKey.Create(
                        CngAlgorithm.Rsa,
                        name,
                        keyParams));

                    ApplicationTraceSources.Default.TraceInformation(
                        "Created new CNG key {0} in {1}", name, this.provider);

                    return key;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
