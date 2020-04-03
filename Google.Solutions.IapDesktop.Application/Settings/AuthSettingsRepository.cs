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

using Google.Apis.Json;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Google.Solutions.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    /// <summary>
    /// Registry-backed repository for UI layout settings.
    /// </summary>
    public class AuthSettingsRepository : SettingsRepositoryBase<AuthSettings>, IDataStore
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        public string CredentialStoreKey { get; }

        public AuthSettingsRepository(RegistryKey baseKey, string credentialStoreKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
            Utilities.ThrowIfNullOrEmpty(credentialStoreKey, nameof(credentialStoreKey));

            this.CredentialStoreKey = credentialStoreKey;
        }

        public AuthSettingsRepository(RegistryKey baseKey) : this(baseKey, "credential")
        {
        }

        //---------------------------------------------------------------------
        // IDataStore.
        //
        // Rather than supporting all possible keys, this implementation only 
        // supports a single known key and maps that to a prooperty.
        //---------------------------------------------------------------------

        public Task ClearAsync()
        {
            SetSettings(new AuthSettings());
            return CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            Utilities.ThrowIfNullOrEmpty(key, nameof(key));

            if (key == CredentialStoreKey)
            {
                return ClearAsync();
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }

        public Task<T> GetAsync<T>(string key)
        {
            Utilities.ThrowIfNullOrEmpty(key, nameof(key));

            if (key == CredentialStoreKey)
            {
                var clearText = GetSettings().Credentials.AsClearText();
                return Task.FromResult(
                    NewtonsoftJsonSerializer.Instance.Deserialize<T>(clearText));
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }

        public Task StoreAsync<T>(string key, T value)
        {
            Utilities.ThrowIfNullOrEmpty(key, nameof(key));

            if (key == CredentialStoreKey)
            {
                SetSettings(new AuthSettings()
                {
                    Credentials = SecureStringExtensions.FromClearText(
                        NewtonsoftJsonSerializer.Instance.Serialize(value))
                });

                return CompletedTask;
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }
    }

    public class AuthSettings
    {

        [SecureStringRegistryValue("Credentials", DataProtectionScope.CurrentUser)]
        public SecureString Credentials { get; set; }
    }
}
