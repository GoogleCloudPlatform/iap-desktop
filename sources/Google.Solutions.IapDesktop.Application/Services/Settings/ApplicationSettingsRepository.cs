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
        private readonly RegistryKey groupPolicyBaseKey;

        public ApplicationSettingsRepository(
            RegistryKey baseKey,
            RegistryKey groupPolicyBaseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));

            this.groupPolicyBaseKey = groupPolicyBaseKey;
        }

        protected override ApplicationSettings LoadSettings(RegistryKey key)
            => ApplicationSettings.FromKey(key, this.groupPolicyBaseKey);
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
            this.IncludeOperatingSystems
        };

        private ApplicationSettings()
        { }

        public static ApplicationSettings FromKey(
            RegistryKey key,
            RegistryKey groupPolicyKey)
        {
            return new ApplicationSettings()
            {
                //
                // Settings that can be overriden by policy.
                //
                // NB. Default values must be kept consistent with the
                // ADMX policy templates!
                //
                IsPreviewFeatureSetEnabled = RegistryBoolSetting.FromKey(
                    "IsPreviewFeatureSetEnabled",
                    "IsPreviewFeatureSetEnabled",
                    null,
                    null,
                    false,
                    key).ApplyPolicy(groupPolicyKey),
                IsUpdateCheckEnabled = RegistryBoolSetting.FromKey(
                    "IsUpdateCheckEnabled",
                    "IsUpdateCheckEnabled",
                    null,
                    null,
                    true,
                    key).ApplyPolicy(groupPolicyKey),
                IsDeviceCertificateAuthenticationEnabled = RegistryBoolSetting.FromKey(
                    "IsDeviceCertificateAuthenticationEnabled",
                    "IsDeviceCertificateAuthenticationEnabled",
                    null,
                    null,
                    false,
                    key).ApplyPolicy(groupPolicyKey),
                ProxyUrl = RegistryStringSetting.FromKey(
                    "ProxyUrl",
                    "ProxyUrl",
                    null,
                    null,
                    null,
                    key,
                    url => url == null || Uri.TryCreate(url, UriKind.Absolute, out Uri _))
                    .ApplyPolicy(groupPolicyKey),
                ProxyPacUrl = RegistryStringSetting.FromKey(
                    "ProxyPacUrl",
                    "ProxyPacUrl",
                    null,
                    null,
                    null,
                    key,
                    url => url == null || Uri.TryCreate(url, UriKind.Absolute, out Uri _))
                    .ApplyPolicy(groupPolicyKey),

                //
                // User preferences. These cannot be overriden by policy.
                //
                IsMainWindowMaximized = RegistryBoolSetting.FromKey(
                    "IsMainWindowMaximized",
                    "IsMainWindowMaximized",
                    null,
                    null,
                    false,
                    key),
                MainWindowHeight = RegistryDwordSetting.FromKey(
                    "MainWindowHeight",
                    "MainWindowHeight",
                    null,
                    null,
                    0,
                    key,
                    0,
                    ushort.MaxValue),
                MainWindowWidth = RegistryDwordSetting.FromKey(
                    "WindowWidth",
                    "WindowWidth",
                    null,
                    null,
                    0,
                    key,
                    0,
                    ushort.MaxValue),
                LastUpdateCheck = RegistryQwordSetting.FromKey(
                    "LastUpdateCheck",
                    "LastUpdateCheck",
                    null,
                    null,
                    0,
                    key,
                    0,
                    long.MaxValue),
                ProxyUsername = RegistryStringSetting.FromKey(
                    "ProxyUsername",
                    "ProxyUsername",
                    null,
                    null,
                    null,
                    key,
                    _ => true),
                ProxyPassword = RegistrySecureStringSetting.FromKey(
                    "ProxyPassword",
                    "ProxyPassword",
                    null,
                    null,
                    key,
                    DataProtectionScope.CurrentUser),
                FullScreenDevices = RegistryStringSetting.FromKey(
                    "FullScreenDevices",
                    "FullScreenDevices",
                    null,
                    null,
                    null,
                    key,
                    _ => true),
                IncludeOperatingSystems = RegistryEnumSetting<OperatingSystems>.FromKey(
                    "IncludeOperatingSystems",
                    "IncludeOperatingSystems",
                    null,
                    null,
                    OperatingSystems.Windows | OperatingSystems.Linux,
                    key)
            };
        }
    }
}
