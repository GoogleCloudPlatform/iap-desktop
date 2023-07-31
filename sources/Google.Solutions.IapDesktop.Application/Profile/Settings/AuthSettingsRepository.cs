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
using Google.Apis.Util.Store;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// Registry-backed repository for UI layout settings.
    /// </summary>
    public class AuthSettingsRepository : 
        SettingsRepositoryBase<AuthSettings>, IDataStore, IOidcOfflineCredentialStore
    {
        public string CredentialStoreKey { get; }

        public AuthSettingsRepository(RegistryKey baseKey, string credentialStoreKey) : base(baseKey)
        {
            baseKey.ExpectNotNull(nameof(baseKey));
            credentialStoreKey.ExpectNotEmpty(nameof(credentialStoreKey));

            this.CredentialStoreKey = credentialStoreKey;
        }

        public AuthSettingsRepository(RegistryKey baseKey) : this(baseKey, "credential")
        {
        }

        //---------------------------------------------------------------------
        // SettingsRepositoryBase.
        //---------------------------------------------------------------------

        protected override AuthSettings LoadSettings(RegistryKey key)
            => AuthSettings.FromKey(key);

        //---------------------------------------------------------------------
        // IOidcOfflineCredentialStore
        //---------------------------------------------------------------------

        public bool TryRead(out OidcOfflineCredential credential)
        {
            credential = null;

            var clearTextJson = GetSettings().Credentials.ClearTextValue;
            if (!string.IsNullOrEmpty(clearTextJson))
            {
                try
                {
                    credential = NewtonsoftJsonSerializer
                        .Instance
                        .Deserialize<CredentialBlob>(clearTextJson)
                        .ToOidcOfflineCredential();
                } 
                catch (JsonSerializationException)
                { }
            }

            return credential?.RefreshToken != null;
        }

        public void Write(OidcOfflineCredential credential)
        {
            credential.ExpectNotNull(nameof(credential));

            var settings = GetSettings();
            settings.Credentials.ClearTextValue = NewtonsoftJsonSerializer
                .Instance
                .Serialize(CredentialBlob.FromOidcOfflineCredential(credential));
            SetSettings(settings);
        }

        public void Clear()
        {
            ClearSettings();
        }


        // TODO: Remove IDataStore impl
        //---------------------------------------------------------------------
        // IDataStore.
        //
        // Rather than supporting all possible keys, this implementation only 
        // supports a single known key and maps that to a property.
        //---------------------------------------------------------------------

        public Task ClearAsync()
        {
            ClearSettings();
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            key.ExpectNotEmpty(nameof(key));

            if (key == this.CredentialStoreKey)
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
            key.ExpectNotEmpty(nameof(key));

            if (key == this.CredentialStoreKey)
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
            key.ExpectNotEmpty(nameof(key));

            if (key == this.CredentialStoreKey)
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

        /// <summary>
        /// Credential blob stored in the registry.
        /// 
        /// NB. Previous versions of IAP Desktop implemented IDataStore
        /// to load and store OAuth credentials. IDataStore uses TokenResult
        /// objects for persistence. Therefore, the blob is JSON-compatible
        /// with TokenResult.
        /// </summary>
        private class CredentialBlob
        {
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }

            [JsonProperty("id_token")]
            public string IdToken { get; set; }

            public OidcOfflineCredential ToOidcOfflineCredential()
            {
                return new OidcOfflineCredential(
                    this.RefreshToken, 
                    this.IdToken);
            }

            public static CredentialBlob FromOidcOfflineCredential(
                OidcOfflineCredential offlineCredential)
            {
                return new CredentialBlob()
                {
                    RefreshToken = offlineCredential.RefreshToken,
                    IdToken = offlineCredential.IdToken
                };
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
                    "JSON-formatted credentials",
                    null,
                    null,
                    registryKey,
                    DataProtectionScope.CurrentUser)
            };
        }
    }
}
