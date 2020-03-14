using Google.Apis.Json;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Settings
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
