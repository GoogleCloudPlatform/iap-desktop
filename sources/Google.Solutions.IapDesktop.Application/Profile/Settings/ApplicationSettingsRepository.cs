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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Auth;
using Google.Solutions.IapDesktop.Application.Profile.Settings.Registry;
using Google.Solutions.Platform.Net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// General settings.
    /// </summary>
    public interface IApplicationSettings : ISettingsCollection
    {
        IBoolSetting IsMainWindowMaximized { get; }
        IIntSetting MainWindowHeight { get; }
        IIntSetting MainWindowWidth { get; }
        IBoolSetting IsUpdateCheckEnabled { get; }
        IBoolSetting IsTelemetryEnabled { get; }
        ILongSetting LastUpdateCheck { get; }
        IBoolSetting IsPreviewFeatureSetEnabled { get; }
        IStringSetting ProxyUrl { get; }
        IStringSetting ProxyPacUrl { get; }
        IStringSetting ProxyUsername { get; }
        ISecureStringSetting ProxyPassword { get; }
        IEnumSetting<SecurityProtocolType> TlsVersions { get; }
        IStringSetting FullScreenDevices { get; }
        IStringSetting CollapsedProjects { get; }

        /// <summary>
        /// Participate in surveys.
        /// </summary>
        IBoolSetting IsSurveyEnabled { get; }

        /// <summary>
        /// Last release version for which the user has taken a survey.
        /// </summary>
        IStringSetting LastSurveyVersion { get; }
    }

    /// <summary>
    /// Registry-backed repository for app settings.
    /// </summary>
    public class ApplicationSettingsRepository
        : PolicyEnabledRegistryRepository<IApplicationSettings>
    {
        public const char FullScreenDevicesSeparator = ',';

        public ApplicationSettingsRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey) 
            : base(settingsKey, machinePolicyKey, userPolicyKey)
        {
        }

        protected override IApplicationSettings LoadSettings(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
            => ApplicationSettings.FromKey(
                settingsKey,
                machinePolicyKey,
                userPolicyKey);


        //---------------------------------------------------------------------
        // Inner class.
        //---------------------------------------------------------------------

        private class ApplicationSettings : IApplicationSettings
        {
            private ApplicationSettings()
            { }

            public IBoolSetting IsMainWindowMaximized { get; private set; }

            public IIntSetting MainWindowHeight { get; private set; }

            public IIntSetting MainWindowWidth { get; private set; }

            public IBoolSetting IsUpdateCheckEnabled { get; private set; }

            public IBoolSetting IsTelemetryEnabled { get; private set; }

            public ILongSetting LastUpdateCheck { get; private set; }

            public IBoolSetting IsPreviewFeatureSetEnabled { get; private set; }

            public IStringSetting ProxyUrl { get; private set; }

            public IStringSetting ProxyPacUrl { get; private set; }

            public IStringSetting ProxyUsername { get; private set; }

            public ISecureStringSetting ProxyPassword { get; private set; }

            public IEnumSetting<SecurityProtocolType> TlsVersions { get; private set; }

            public IStringSetting FullScreenDevices { get; private set; }

            public IStringSetting CollapsedProjects { get; private set; }

            public IBoolSetting IsSurveyEnabled { get; private set; }

            public IStringSetting LastSurveyVersion { get; private set; }

            public IEnumerable<ISetting> Settings => new ISetting[]
            {
                this.IsMainWindowMaximized,
                this.MainWindowHeight,
                this.MainWindowWidth,
                this.IsUpdateCheckEnabled,
                this.IsTelemetryEnabled,
                this.LastUpdateCheck,
                this.IsUpdateCheckEnabled,
                this.ProxyUrl,
                this.ProxyPacUrl,
                this.ProxyUsername,
                this.ProxyPassword,
                this.TlsVersions,
                this.FullScreenDevices,
                this.CollapsedProjects,
                this.IsSurveyEnabled,
                this.LastSurveyVersion
            };

            public static ApplicationSettings FromKey(
                RegistryKey settingsKey,
                RegistryKey machinePolicyKey,
                RegistryKey userPolicyKey)
            {
                return new ApplicationSettings()
                {
                    //
                    // Settings that can be overridden by policy.
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
                    IsTelemetryEnabled = RegistryBoolSetting.FromKey(
                            "IsTelemetryEnabled",
                            "IsTelemetryEnabled",
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
                            url => url == null || Uri.TryCreate(url, UriKind.Absolute, out var _))
                        .ApplyPolicy(userPolicyKey)
                        .ApplyPolicy(machinePolicyKey),
                    ProxyPacUrl = RegistryStringSetting.FromKey(
                            "ProxyPacUrl",
                            "ProxyPacUrl",
                            null,
                            null,
                            null,
                            settingsKey,
                            url => url == null || Uri.TryCreate(url, UriKind.Absolute, out var _))
                        .ApplyPolicy(userPolicyKey)
                        .ApplyPolicy(machinePolicyKey),
                    TlsVersions = RegistryEnumSetting<SecurityProtocolType>.FromKey(
                            "TlsVersions",
                            "TlsVersions",
                            null,
                            null,
                            OSCapabilities.SupportedTlsVersions,
                            settingsKey)
                        .ApplyPolicy(userPolicyKey)
                        .ApplyPolicy(machinePolicyKey),

                    //
                    // User preferences. These cannot be overridden by policy.
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
                    CollapsedProjects = RegistryStringSetting.FromKey(
                        "CollapsedProjects",
                        "CollapsedProjects",
                        null,
                        null,
                        null,
                        settingsKey,
                        _ => true),
                    IsSurveyEnabled = RegistryBoolSetting.FromKey(
                        "IsSurveyEnabled",
                        "IsSurveyEnabled",
                        null,
                        null,
                        true,
                        settingsKey),
                    LastSurveyVersion = RegistryStringSetting.FromKey(
                        "LastSurveyVersion",
                        "LastSurveyVersion",
                        null,
                        null,
                        null,
                        settingsKey,
                        _ => true)
                };
            }
        }
    }
}