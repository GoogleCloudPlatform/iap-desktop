﻿//
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
        SettingsRepositoryBase<AuthSettings>, IOidcOfflineCredentialStore
    {
        public AuthSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            baseKey.ExpectNotNull(nameof(baseKey));
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

            [JsonProperty("scope")]
            public string Scope { get; set; }

            /// <summary>
            /// Issuer of credential. For backwards compatibility,
            /// a null/empty value is interpreted as Gaia.
            /// </summary>
            [JsonProperty("issuer")]
            public string Issuer { get; set; }

            public OidcOfflineCredential ToOidcOfflineCredential()
            {
                if (this.Issuer == "sts")
                {
                    return new OidcOfflineCredential(
                        OidcIssuer.Iam,
                        this.Scope,
                        this.RefreshToken,
                        this.IdToken);
                }
                else
                {
                    return new OidcOfflineCredential(
                        OidcIssuer.Gaia,
                        this.Scope,
                        this.RefreshToken,
                        this.IdToken);
                }
            }

            public static CredentialBlob FromOidcOfflineCredential(
                OidcOfflineCredential offlineCredential)
            {
                return new CredentialBlob()
                {
                    Issuer = offlineCredential.Issuer == OidcIssuer.Iam
                        ? "sts"
                        : null,
                    RefreshToken = offlineCredential.RefreshToken,
                    IdToken = offlineCredential.IdToken,
                    Scope = offlineCredential.Scope
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
