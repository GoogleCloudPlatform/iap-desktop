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
using Google.Solutions.Common.Util;
using Google.Apis.Util.Store;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for UI layout settings.
    /// </summary>
    public class AuthSettingsRepository : SettingsRepositoryBase<AuthSettings>, IDataStore
    {
        public string CredentialStoreKey { get; }

        public AuthSettingsRepository(RegistryKey baseKey, string credentialStoreKey) : base(baseKey)
        {
            Precondition.ExpectNotNull(baseKey, nameof(baseKey));
            Precondition.ExpectNotEmpty(credentialStoreKey, nameof(credentialStoreKey));

            this.CredentialStoreKey = credentialStoreKey;
        }

        public AuthSettingsRepository(RegistryKey baseKey) : this(baseKey, "credential")
        {
        }

        //---------------------------------------------------------------------
        // SettingsRepositoryBase
        //---------------------------------------------------------------------

        protected override AuthSettings LoadSettings(RegistryKey key)
            => AuthSettings.FromKey(key);

        //---------------------------------------------------------------------
        // IDataStore.
        //
        // Rather than supporting all possible keys, this implementation only 
        // supports a single known key and maps that to a prooperty.
        //---------------------------------------------------------------------

        public Task ClearAsync()
        {
            ClearSettings();
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            Precondition.ExpectNotEmpty(key, nameof(key));

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
            Precondition.ExpectNotEmpty(key, nameof(key));

            if (key == CredentialStoreKey)
            {
                var clearText = GetSettings().Credentials.ClearTextValue;
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
            Precondition.ExpectNotEmpty(key, nameof(key));

            if (key == CredentialStoreKey)
            {
                var settings = GetSettings();
                settings.Credentials.Value = SecureStringExtensions.FromClearText(
                        NewtonsoftJsonSerializer.Instance.Serialize(value));
                SetSettings(settings);

                return Task.CompletedTask;
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }
    }

    public class AuthSettings : IRegistrySettingsCollection
    {
        public RegistrySecureStringSetting Credentials { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.Credentials
        };

        private AuthSettings()
        { }

        public static AuthSettings FromKey(RegistryKey registryKey)
        {
            return new AuthSettings()
            {
                Credentials = RegistrySecureStringSetting.FromKey(
                    "Credentials",
                    "OAuth credentials",
                    null,
                    null,
                    registryKey,
                    DataProtectionScope.CurrentUser)
            };
        }
    }
}
