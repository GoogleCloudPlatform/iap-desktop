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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for app settings.
    /// </summary>
    public class ApplicationSettingsRepository : SettingsRepositoryBase<ApplicationSettings>
    {
        private readonly RegistryKey machinePolicyKey;
        private readonly RegistryKey userPolicyKey;

        public ApplicationSettingsRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey) : base(settingsKey)
        {
            Utilities.ThrowIfNull(settingsKey, nameof(settingsKey));

            this.machinePolicyKey = machinePolicyKey;
            this.userPolicyKey = userPolicyKey;
        }

        protected override ApplicationSettings LoadSettings(RegistryKey key)
            => ApplicationSettings.FromKey(
                key, 
                this.machinePolicyKey, 
                this.userPolicyKey);

        public bool IsPolicyPresent
            => this.machinePolicyKey != null || this.userPolicyKey != null;
    }

    public class ApplicationSettings : IRegistrySettingsCollection
    {
        public const char FullScreenDevicesSeparator = ',';

        public RegistryBoolSetting IsMainWindowMaximized { get; private set; }

        public RegistryDwordSetting MainWindowHeight { get; private set; }

        public RegistryDwordSetting MainWindowWidth { get; private set; }

        public RegistryBoolSetting IsUpdateCheckEnabled { get; private set; }

        public RegistryQwordSetting LastUpdateCheck { get; private set; }

        public RegistryBoolSetting IsPreviewFeatureSetEnabled { get; private set; }

        public RegistryStringSetting ProxyUrl { get; private set; }

        public RegistryStringSetting ProxyPacUrl { get; private set; }

        public RegistryStringSetting ProxyUsername { get; private set; }

        public RegistrySecureStringSetting ProxyPassword { get; private set; }

        public RegistryBoolSetting IsDeviceCertificateAuthenticationEnabled { get; private set; }

        public RegistryStringSetting FullScreenDevices { get; private set; }

        public RegistryEnumSetting<OperatingSystems> IncludeOperatingSystems { get; private set; }

        public RegistryStringSetting DeviceCertificateSelector { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.IsMainWindowMaximized,
            this.MainWindowHeight,
            this.MainWindowWidth,
            this.IsUpdateCheckEnabled,
            this.LastUpdateCheck,
            this.IsUpdateCheckEnabled,
            this.ProxyUrl,
            this.ProxyPacUrl,
            this.ProxyUsername,
            this.ProxyPassword,
            this.IsDeviceCertificateAuthenticationEnabled,
            this.FullScreenDevices,
            this.IncludeOperatingSystems,
            this.DeviceCertificateSelector
        };

        private ApplicationSettings()
        { }

        public static ApplicationSettings FromKey(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
        {
            return new ApplicationSettings()
            {
                //
                // Settings that can be overriden by policy.
                //
                // NB. Default values must be kept consistent with the
                //     ADMX policy templates!
                // NB. Machine policies override user policies, and
                //     user policies override settings.
                //
                IsPreviewFeatureSetEnabled = RegistryBoolSetting.FromKey(
                        "IsPreviewFeatureSetEnabled",
                        "IsPreviewFeatureSetEnabled",
                        null,
                        null,
                        false,
                        settingsKey)
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey),
                IsUpdateCheckEnabled = RegistryBoolSetting.FromKey(
                        "IsUpdateCheckEnabled",
                        "IsUpdateCheckEnabled",
                        null,
                        null,
                        true,
                        settingsKey)
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey),
                IsDeviceCertificateAuthenticationEnabled = RegistryBoolSetting.FromKey(
                        "IsDeviceCertificateAuthenticationEnabled",
                        "IsDeviceCertificateAuthenticationEnabled",
                        null,
                        null,
                        false,
                        settingsKey)
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey),
                ProxyUrl = RegistryStringSetting.FromKey(
                        "ProxyUrl",
                        "ProxyUrl",
                        null,
                        null,
                        null,
                        settingsKey,
                        url => url == null || Uri.TryCreate(url, UriKind.Absolute, out Uri _))
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey),
                ProxyPacUrl = RegistryStringSetting.FromKey(
                        "ProxyPacUrl",
                        "ProxyPacUrl",
                        null,
                        null,
                        null,
                        settingsKey,
                        url => url == null || Uri.TryCreate(url, UriKind.Absolute, out Uri _))
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey),
                DeviceCertificateSelector = RegistryStringSetting.FromKey(
                        "DeviceCertificateSelector",
                        "DeviceCertificateSelector",
                        null,
                        null,
                        SecureConnectEnrollment.DefaultDeviceCertificateSelector,
                        settingsKey,
                        selector => selector == null || ChromeCertificateSelector.TryParse(selector, out var _))
                    .ApplyPolicy(userPolicyKey)
                    .ApplyPolicy(machinePolicyKey), // TODO: extend ADMX

                //
                // User preferences. These cannot be overriden by policy.
                //
                IsMainWindowMaximized = RegistryBoolSetting.FromKey(
                    "IsMainWindowMaximized",
                    "IsMainWindowMaximized",
                    null,
                    null,
                    false,
                    settingsKey),
                MainWindowHeight = RegistryDwordSetting.FromKey(
                    "MainWindowHeight",
                    "MainWindowHeight",
                    null,
                    null,
                    0,
                    settingsKey,
                    0,
                    ushort.MaxValue),
                MainWindowWidth = RegistryDwordSetting.FromKey(
                    "WindowWidth",
                    "WindowWidth",
                    null,
                    null,
                    0,
                    settingsKey,
                    0,
                    ushort.MaxValue),
                LastUpdateCheck = RegistryQwordSetting.FromKey(
                    "LastUpdateCheck",
                    "LastUpdateCheck",
                    null,
                    null,
                    0,
                    settingsKey,
                    0,
                    long.MaxValue),
                ProxyUsername = RegistryStringSetting.FromKey(
                    "ProxyUsername",
                    "ProxyUsername",
                    null,
                    null,
                    null,
                    settingsKey,
                    _ => true),
                ProxyPassword = RegistrySecureStringSetting.FromKey(
                    "ProxyPassword",
                    "ProxyPassword",
                    null,
                    null,
                    settingsKey,
                    DataProtectionScope.CurrentUser),
                FullScreenDevices = RegistryStringSetting.FromKey(
                    "FullScreenDevices",
                    "FullScreenDevices",
                    null,
                    null,
                    null,
                    settingsKey,
                    _ => true),
                IncludeOperatingSystems = RegistryEnumSetting<OperatingSystems>.FromKey(
                    "IncludeOperatingSystems",
                    "IncludeOperatingSystems",
                    null,
                    null,
                    OperatingSystems.Windows | OperatingSystems.Linux,
                    settingsKey)
            };
        }
    }
}
