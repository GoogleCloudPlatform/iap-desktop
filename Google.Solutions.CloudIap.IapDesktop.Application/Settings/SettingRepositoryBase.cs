using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Settings
{
    public class SettingsRepositoryBase<TSettings> : IDisposable where TSettings : new()
    {
        protected readonly RegistryKey baseKey;

        public SettingsRepositoryBase(RegistryKey baseKey)
        {
            this.baseKey = baseKey;
        }


        protected T Get<T>(string keyName) where T : new()
        {
            using (var key = this.baseKey.OpenSubKey(keyName))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(keyName);
                }

                return new RegistryBinder<T>().Load(key);
            }
        }

        protected void Set<T>(string keyName, T settings) where T : new()
        {
            using (var key = this.baseKey.CreateSubKey(keyName))
            {
                if (key == null)
                {
                    throw new ArgumentException(keyName);
                }

                new RegistryBinder<T>().Store(settings, key);
            }
        }


        public TSettings GetSettings()
        {
            return new RegistryBinder<TSettings>().Load(this.baseKey);
        }

        public void SetSettings(TSettings settings)
        {
            new RegistryBinder<TSettings>().Store(settings, this.baseKey);
        }


        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.baseKey.Dispose();
            }
        }
    }
}
