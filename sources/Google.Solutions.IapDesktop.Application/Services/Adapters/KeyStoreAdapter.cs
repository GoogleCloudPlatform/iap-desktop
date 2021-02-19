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
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IKeyStoreAdapter
    {
        Task<RSA> CreateRsaKeyAsync(
            string name,
            CngKeyUsages usage,
            bool createNewIfNotExists);
    }

    public class KeyStoreAdapter
    {
        private readonly CngProvider provider;
        private readonly int keySize;

        public KeyStoreAdapter(
            CngProvider provider,
            int keySize)
        {
            this.provider = provider;
            this.keySize = keySize;
        }

        //---------------------------------------------------------------------
        // IKeyStoreAdapter
        //---------------------------------------------------------------------

        public Task<RSA> CreateRsaKeyAsync(
            string name,
            CngKeyUsages usage,
            bool createNewIfNotExists)
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

                return Task.Run<RSA>(() =>
                {
                    if (CngKey.Exists(name))
                    {
                        var key = CngKey.Open(name);
                        if (key.Algorithm != CngAlgorithm.Rsa)
                        {
                            throw new CryptographicException(
                                $"Key {name} is not an RSA key");
                        }

                        if ((key.KeyUsage & usage) == 0)
                        {
                            throw new CryptographicException(
                                $"Key {name} exists, but does not support requested usage");

                        }

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

                        keyParams.Parameters.Add(
                            new CngProperty(
                                "Length",
                                BitConverter.GetBytes(this.keySize),
                                CngPropertyOptions.None));

                        //
                        // Create the key. That's a bit expensive, so do it
                        // asynchronously.
                        //
                        return new RSACng(CngKey.Create(
                            CngAlgorithm.Rsa,
                            name,
                            keyParams));
                    }

                    return null;
                });
            }
        }
    }
}
