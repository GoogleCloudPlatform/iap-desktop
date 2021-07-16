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

using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for SSH settings.
    /// 
    /// Service is a singleton so that objects can subscribe to events.
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceVisibility.Global)]
    public class SshSettingsRepository : SettingsRepositoryBase<SshSettings>
    {
        public SshSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }

        public SshSettingsRepository()
            : this(RegistryKey
                .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                .CreateSubKey($@"{Globals.SettingsKeyPath}\Ssh"))
        {
        }

        protected override SshSettings LoadSettings(RegistryKey key)
            => SshSettings.FromKey(key);
    }

    public class SshSettings : IRegistrySettingsCollection
    {
        public RegistryBoolSetting IsPropagateLocaleEnabled { get; private set; }
        public RegistryDwordSetting PublicKeyValidity { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            IsPropagateLocaleEnabled,
            PublicKeyValidity
        };

        private SshSettings()
        {
        }

        public static SshSettings FromKey(RegistryKey registryKey)
        {
            return new SshSettings()
            {
                IsPropagateLocaleEnabled = RegistryBoolSetting.FromKey(
                    "IsPropagateLocaleEnabled",
                    "IsPropagateLocaleEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                PublicKeyValidity = RegistryDwordSetting.FromKey(
                    "PublicKeyValidity",
                    "PublicKeyValidity",
                    "Validity of (OS Login/Metadata) keys in seconds",
                    null,
                    (int)TimeSpan.FromDays(30).TotalSeconds,
                    registryKey,
                    (int)TimeSpan.FromMinutes(1).TotalSeconds,
                    int.MaxValue)
            };
        }
    }
}
