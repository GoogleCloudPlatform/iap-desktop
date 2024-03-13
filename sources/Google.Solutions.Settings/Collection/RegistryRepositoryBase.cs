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

using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using System;

namespace Google.Solutions.Settings.Collection
{
    /// <summary>
    /// Base class for all settings repositories.
    /// </summary>
    public abstract class RegistryRepositoryBase<TSettings>  // TODO: Rename, drop prefix
        : IRepository<TSettings>
        where TSettings : ISettingsCollection
    {
        protected RegistryKey BaseKey { get; }

        protected RegistryRepositoryBase(RegistryKey baseKey)
        {
            this.BaseKey = baseKey;
        }

        public virtual TSettings GetSettings()
        {
            return LoadSettings(this.BaseKey);
        }

        public virtual void SetSettings(TSettings settings)
        {
            settings.Save(this.BaseKey);
        }

        public void ClearSettings()
        {
            // Delete values, but keep any subkeys.
            foreach (var valueName in this.BaseKey.GetValueNames())
            {
                this.BaseKey.DeleteValue(valueName);
            }
        }

        protected abstract TSettings LoadSettings(RegistryKey key);

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
                this.BaseKey.Dispose();
            }
        }
    }
}
