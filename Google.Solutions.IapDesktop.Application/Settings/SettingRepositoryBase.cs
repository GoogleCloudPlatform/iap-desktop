using Google.Solutions.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public class SettingsRepositoryBase<TSettings> : IDisposable where TSettings : new()
    {
        protected readonly RegistryKey baseKey;

        public SettingsRepositoryBase(RegistryKey baseKey)
        {
            this.baseKey = baseKey;
        }

        private bool DoesKeyExist(IEnumerable<string> keyPath)
        {
            using (var key = this.baseKey.OpenSubKey(string.Join("\\", keyPath)))
            {
                return key != null;
            }
        }

        protected T Get<T>(IEnumerable<string> keyPath) where T : new()
        {
            using (var key = this.baseKey.OpenSubKey(string.Join("\\", keyPath)))
            {
                if (key == null)
                {
                    if (keyPath.Count() == 0 || DoesKeyExist(keyPath.Take(1)))
                    {
                        // The first segment of the path exists, it is safe to return
                        // defaults then.
                        return new T();
                    }
                    else
                    {
                        // The parent key does not exist, that's an error.
                        throw new KeyNotFoundException(string.Join("\\", keyPath));
                    }
                }
                else
                {
                    return new RegistryBinder<T>().Load(key);
                }
            }
        }

        protected void Set<T>(IEnumerable<string> keyPath, T settings) where T : new()
        {
            using (var key = this.baseKey.CreateSubKey(string.Join("\\", keyPath)))
            {
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
