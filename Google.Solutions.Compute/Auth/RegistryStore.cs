//
// Copyright 2019 Google LLC
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
using Microsoft.Win32;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Auth
{
    /// <summary>
    /// Authentication data store that encrypts data by using DPAPI and 
    /// stores the cyphertext in the registry.
    /// 
    /// Tokens tend to be slightly too long to be stored in the Windows 
    /// Credential Store (CredRead/CredWrite), so the registry is more 
    /// flexible.
    /// </summary>
    public class RegistryStore : IDataStore, IDisposable
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        private readonly RegistryHive hive;
        private readonly RegistryKey key;

        public RegistryStore(RegistryHive hive, string keyPath)
        {
            this.hive = hive;
            this.key = RegistryKey.OpenBaseKey(hive, RegistryView.Default)
                        .CreateSubKey(keyPath, true);
        }

        private DataProtectionScope ProtectionScope =>
            this.hive == RegistryHive.LocalMachine
                ? DataProtectionScope.LocalMachine
                : DataProtectionScope.CurrentUser;

        public Task StoreAsync<T>(string entryKey, T value)
        {
            Utilities.ThrowIfNullOrEmpty(entryKey, nameof(entryKey));

            var encryptedValue = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(
                    NewtonsoftJsonSerializer.Instance.Serialize(value)),
                Encoding.UTF8.GetBytes(entryKey), 
                this.ProtectionScope);

            this.key.SetValue(entryKey, encryptedValue, RegistryValueKind.Binary);

            return CompletedTask;
        }

        public Task<T> GetAsync<T>(string entryKey)
        {
            Utilities.ThrowIfNullOrEmpty(entryKey, nameof(entryKey));

            var encryptedValue = this.key.GetValue(entryKey, default(T));

            if (encryptedValue is byte[] encryptedValueBytes)
            {
                try
                {
                    var plaintextString = Encoding.UTF8.GetString(
                        ProtectedData.Unprotect(
                            (byte[])encryptedValue,
                            Encoding.UTF8.GetBytes(entryKey),
                            this.ProtectionScope));

                    return Task.FromResult(
                        NewtonsoftJsonSerializer.Instance.Deserialize<T>(plaintextString));
                }
                catch (CryptographicException)
                {
                    // Value cannot be decrypted. This can happen if it was
                    // written by a different user or if the current user's
                    // key has changed (for example, because its credentials
                    // been reset on GCE).
                    return Task.FromResult(default(T));
                }
            }
            else
            {
                // Garbled or missing data, ignore.
                return Task.FromResult(default(T));
            }
        }

        public Task DeleteAsync<T>(string entryKey)
        {
            Utilities.ThrowIfNullOrEmpty(entryKey, nameof(entryKey));

            try
            {
                this.key.DeleteValue(entryKey);
            }
            catch (ArgumentException)
            {
                // Value does not exist, ignore.
            }

            return CompletedTask;
        }

        public Task ClearAsync()
        {
            foreach (string entryKey in this.key.GetValueNames())
            {
                this.key.DeleteValue(entryKey);
            }

            return CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.key.Dispose();
            }
        }
    }
}
