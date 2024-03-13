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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// General settings.
    /// </summary>
    public interface IApplicationSettings : ISettingsCollection
    {
        ISetting<bool> IsMainWindowMaximized { get; }
        ISetting<int> MainWindowHeight { get; }
        ISetting<int> MainWindowWidth { get; }
        ISetting<bool> IsUpdateCheckEnabled { get; }
        ISetting<bool> IsTelemetryEnabled { get; }
        ISetting<long> LastUpdateCheck { get; }
        ISetting<bool> IsPreviewFeatureSetEnabled { get; }
        ISetting<string> ProxyUrl { get; }
        ISetting<string> ProxyPacUrl { get; }
        ISetting<string> ProxyUsername { get; }
        ISetting<SecureString> ProxyPassword { get; }
        ISetting<int> ProxyAuthenticationRetries { get; }
        ISetting<SecurityProtocolType> TlsVersions { get; }
        ISetting<string> FullScreenDevices { get; }
        ISetting<string> CollapsedProjects { get; }

        /// <summary>
        /// Participate in surveys.
        /// </summary>
        ISetting<bool> IsSurveyEnabled { get; }

        /// <summary>
        /// Last release version for which the user has taken a survey.
        /// </summary>
        ISetting<string> LastSurveyVersion { get; }
    }

    /// <summary>
    /// Registry-backed repository for app settings.
    /// </summary>
    public class ApplicationSettingsRepository
        : PolicyEnabledRegistryRepository<IApplicationSettings>
    {
        public const char FullScreenDevicesSeparator = ',';
        private readonly UserProfile.SchemaVersion schemaVersion;

        public ApplicationSettingsRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey,
            UserProfile.SchemaVersion schemaVersion) 
            : base(settingsKey, machinePolicyKey, userPolicyKey)
        {
            Precondition.ExpectNotNull(settingsKey, nameof(settingsKey));
            this.schemaVersion = schemaVersion;
        }

        public ApplicationSettingsRepository(UserProfile profile)
            :this(
                profile.SettingsKey.CreateSubKey("Application"),
                profile.MachinePolicyKey?.OpenSubKey("Application"),
                profile.UserPolicyKey?.OpenSubKey("Application"),
                profile.Version)
        {
        }

        protected override IApplicationSettings LoadSettings(ISettingsStore store)
        {
            return new ApplicationSettings(store, this.schemaVersion);
        }

        //---------------------------------------------------------------------
        // Inner class.
        //---------------------------------------------------------------------

        private class ApplicationSettings : IApplicationSettings
        {
            public ISetting<bool> IsMainWindowMaximized { get; private set; }

            public ISetting<int> MainWindowHeight { get; private set; }

            public ISetting<int> MainWindowWidth { get; private set; }

            public ISetting<bool> IsUpdateCheckEnabled { get; private set; }

            public ISetting<bool> IsTelemetryEnabled { get; private set; }

            public ISetting<long> LastUpdateCheck { get; private set; }

            public ISetting<bool> IsPreviewFeatureSetEnabled { get; private set; }

            public ISetting<string> ProxyUrl { get; private set; }

            public ISetting<string> ProxyPacUrl { get; private set; }

            public ISetting<string> ProxyUsername { get; private set; }

            public ISetting<SecureString> ProxyPassword { get; private set; }
            
            public ISetting<int> ProxyAuthenticationRetries { get; private set; }

            public ISetting<SecurityProtocolType> TlsVersions { get; private set; }

            public ISetting<string> FullScreenDevices { get; private set; }

            public ISetting<string> CollapsedProjects { get; private set; }

            public ISetting<bool> IsSurveyEnabled { get; private set; }

            public ISetting<string> LastSurveyVersion { get; private set; }

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
                this.ProxyAuthenticationRetries,
                this.TlsVersions,
                this.FullScreenDevices,
                this.CollapsedProjects,
                this.IsSurveyEnabled,
                this.LastSurveyVersion
            };

            internal ApplicationSettings(
                ISettingsStore store,
                UserProfile.SchemaVersion schemaVersion)
            {
                //
                // Settings that can be overridden by policy.
                //
                // NB. Default values must be kept consistent with the
                //     ADMX policy templates!
                //
                this.IsPreviewFeatureSetEnabled = store.Read<bool>(
                    "IsPreviewFeatureSetEnabled",
                    "IsPreviewFeatureSetEnabled",
                    null,
                    null,
                    false);
                this.IsUpdateCheckEnabled = store.Read<bool>(
                    "IsUpdateCheckEnabled",
                    "IsUpdateCheckEnabled",
                    null,
                    null,
                    true);
                this.IsTelemetryEnabled = store.Read<bool>(
                    "IsTelemetryEnabled",
                    "IsTelemetryEnabled",
                    null,
                    null,
                    schemaVersion >= UserProfile.SchemaVersion.Version240
                        ? true      // Auto opt-in new profiles
                        : false);    // Opt-out existing profiles
                this.ProxyUrl = store.Read<string>(
                    "ProxyUrl",
                    "ProxyUrl",
                    null,
                    null,
                    null,
                    url => url == null || Uri.TryCreate(url, UriKind.Absolute, out var _));
                this.ProxyPacUrl = store.Read<string>(
                    "ProxyPacUrl",
                    "ProxyPacUrl",
                    null,
                    null,
                    null,
                    url => url == null || Uri.TryCreate(url, UriKind.Absolute, out var _));
                this.ProxyAuthenticationRetries = store.Read<int>(
                    "ProxyAuthenticationRetries",
                    "ProxyAuthenticationRetries",
                    null,
                    null,
                    2,              // A single retry might not be sufficient (b/323465182).
                    Validators.InRange(0, 8));
                this.TlsVersions = store.Read<SecurityProtocolType>(
                    "TlsVersions",
                    "TlsVersions",
                    null,
                    null,
                    OSCapabilities.SupportedTlsVersions);

                //
                // User preferences. These cannot be overridden by policy.
                //
                this.IsMainWindowMaximized = store.Read<bool>(
                    "IsMainWindowMaximized",
                    "IsMainWindowMaximized",
                    null,
                    null,
                    false);
                this.MainWindowHeight = store.Read<int>(
                    "MainWindowHeight",
                    "MainWindowHeight",
                    null,
                    null,
                    0,
                    Validators.InRange(0, ushort.MaxValue));
                this.MainWindowWidth = store.Read<int>(
                    "WindowWidth",
                    "WindowWidth",
                    null,
                    null,
                    0,
                    Validators.InRange(0, ushort.MaxValue));
                this.LastUpdateCheck = store.Read<long>(
                    "LastUpdateCheck",
                    "LastUpdateCheck",
                    null,
                    null,
                    0,
                    Validators.InRange(0, long.MaxValue));
                this.ProxyUsername = store.Read<string>(
                    "ProxyUsername",
                    "ProxyUsername",
                    null,
                    null,
                    null,
                    _ => true);
                this.ProxyPassword = store.Read<SecureString>(
                    "ProxyPassword",
                    "ProxyPassword",
                    null,
                    null,
                    null);
                this.FullScreenDevices = store.Read<string>(
                    "FullScreenDevices",
                    "FullScreenDevices",
                    null,
                    null,
                    null,
                    _ => true);
                this.CollapsedProjects = store.Read<string>(
                    "CollapsedProjects",
                    "CollapsedProjects",
                    null,
                    null,
                    null,
                    _ => true);
                this.IsSurveyEnabled = store.Read<bool>(
                    "IsSurveyEnabled",
                    "IsSurveyEnabled",
                    null,
                    null,
                    true);
                this.LastSurveyVersion = store.Read<string>(
                    "LastSurveyVersion",
                    "LastSurveyVersion",
                    null,
                    null,
                    null,
                    _ => true);
            }
        }
    }
}