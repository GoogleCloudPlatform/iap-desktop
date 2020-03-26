//
// Copyright 2010 Google LLC
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
