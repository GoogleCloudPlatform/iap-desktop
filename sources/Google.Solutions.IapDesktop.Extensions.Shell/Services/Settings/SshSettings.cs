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
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for SSH settings.
    /// 
    /// Service is a singleton so that objects can subscribe to events.
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceVisibility.Global)]
    public class SshSettingsRepository : PolicyEnabledSettingsRepository<SshSettings>
    {
        private static RegistryKey WithHive(
            RegistryHive hive,
            Func<RegistryKey, RegistryKey> openFunc)
        {
            using (var hiveKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
            {
                return openFunc(hiveKey);
            }
        }

        public SshSettingsRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey) : base(settingsKey, machinePolicyKey, userPolicyKey)
        {
        }

        public SshSettingsRepository()
            : this(
                WithHive(RegistryHive.CurrentUser, hive => hive.CreateSubKey($@"{Globals.SettingsKeyPath}\Ssh")),
                WithHive(RegistryHive.LocalMachine, hive => hive.OpenSubKey($@"{Globals.PoliciesKeyPath}\Ssh")),
                WithHive(RegistryHive.CurrentUser, hive => hive.OpenSubKey($@"{Globals.PoliciesKeyPath}\Ssh")))
        {
        }

        protected override SshSettings LoadSettings(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
            => SshSettings.FromKey(
                settingsKey,
                machinePolicyKey,
                userPolicyKey);
    }

    public class SshSettings : IRegistrySettingsCollection
    {
        public RegistryBoolSetting IsPropagateLocaleEnabled { get; private set; }
        public RegistryDwordSetting PublicKeyValidity { get; private set; }
        public RegistryEnumSetting<SshKeyType> PublicKeyType { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            IsPropagateLocaleEnabled,
            PublicKeyValidity,
            PublicKeyType
        };

        private SshSettings()
        {
        }

        public static SshSettings FromKey(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
        {
            return new SshSettings()
            {
                //
                // Settings that can be overriden by policy.
                //
                // NB. Default values must be kept consistent with the
                //     ADMX policy templates!
                // NB. Machine policies override user policies, and
                //     user policies override settings.
                //
                PublicKeyType = RegistryEnumSetting<SshKeyType>.FromKey(
                        "PublicKeyType",
                        "PublicKeyType",
                        "Key type for public key authentication",
                        null,
                        SshKeyType.Rsa3072,
                        settingsKey)
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey), // TODO: Extend ADMX
                PublicKeyValidity = RegistryDwordSetting.FromKey(
                        "PublicKeyValidity",
                        "PublicKeyValidity",
                        "Validity of (OS Login/Metadata) keys in seconds",
                        null,
                        (int)TimeSpan.FromDays(30).TotalSeconds,
                        settingsKey,
                        (int)TimeSpan.FromMinutes(1).TotalSeconds,
                        int.MaxValue)
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey), // TODO: Extend ADMX

                //
                // User preferences. These cannot be overriden by policy.
                //
                IsPropagateLocaleEnabled = RegistryBoolSetting.FromKey(
                    "IsPropagateLocaleEnabled",
                    "IsPropagateLocaleEnabled",
                    null,
                    null,
                    true,
                    settingsKey)
            };
        }
    }
}
