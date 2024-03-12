//
// Copyright 2024 Google LLC
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

using Google.Solutions.Common.Util;
using Microsoft.Win32;
using System;

namespace Google.Solutions.Settings.Registry
{
    /// <summary>
    /// Exposes a registry key's values as settings.
    /// </summary>
    public class SettingsKey : SettingsStoreBase<RegistryKey>, IDisposable // TODO: renme to RegistrySettingsStore
    {
        private protected override RegistryKey ValueSource => this.BackingKey;

        internal RegistryKey BackingKey { get; }

        private protected override IValueAccessor<RegistryKey, T> CreateValueAccessor<T>(
            string valueName)
        {
            return RegistryValueAccessor.Create<T>(valueName);
        }

        public SettingsKey(RegistryKey key)
        {
            this.BackingKey = key.ExpectNotNull(nameof(key));
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            this.BackingKey.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}