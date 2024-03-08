//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Profile.Auth;
using Google.Solutions.Platform.Net;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// Google API access-related settings.
    /// </summary>
    public interface IAccessSettings : ISettingsCollection
    {
        /// <summary>
        /// IP or hostname to use for Private Service Connect (PSC). 
        /// If null, PSC is diabled.
        /// </summary>
        IStringSetting PrivateServiceConnectEndpoint { get; }

        /// <summary>
        /// Enable BeyondCorp certificate-based access.
        /// </summary>
        IBoolSetting IsDeviceCertificateAuthenticationEnabled { get; }

        /// <summary>
        /// AutoSelectCertificateForUrl-formatted selector for certificate.
        /// </summary>
        IStringSetting DeviceCertificateSelector { get; }

        /// <summary>
        /// Maximum number of connections per API endpoint.
        /// </summary>
        IIntSetting ConnectionLimit { get; }

        /// <summary>
        /// Workforce pool provider locator. 
        /// 
        /// When set, authentication is performed using workforce
        /// identity instead of Gaia.
        /// </summary>
        IStringSetting WorkforcePoolProvider { get; }
    }

    /// <summary>
    /// Registry-backed repository for access-related settings.
    /// </summary>
    public class AccessSettingsRepository
        : PolicyEnabledRegistryRepository<IAccessSettings>
    {
        public AccessSettingsRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
            : base(settingsKey, machinePolicyKey, userPolicyKey)
        {
        }

        protected override IAccessSettings LoadSettings(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
            => AccessSettings.FromKey(
                settingsKey,
                machinePolicyKey,
                userPolicyKey);

        //---------------------------------------------------------------------
        // Inner class.
        //---------------------------------------------------------------------

        private class AccessSettings : IAccessSettings
        {
            private AccessSettings()
            {
            }

            public IStringSetting WorkforcePoolProvider { get; private set; }

            public IStringSetting PrivateServiceConnectEndpoint { get; private set; }

            public IBoolSetting IsDeviceCertificateAuthenticationEnabled { get; private set; }

            public IStringSetting DeviceCertificateSelector { get; private set; }

            public IIntSetting ConnectionLimit { get; private set; }

            public IEnumerable<ISetting> Settings => new ISetting[]
            {
                this.WorkforcePoolProvider,
                this.PrivateServiceConnectEndpoint,
                this.IsDeviceCertificateAuthenticationEnabled,
                this.DeviceCertificateSelector,
                this.ConnectionLimit
            };

            public static AccessSettings FromKey(
                RegistryKey settingsKey,
                RegistryKey machinePolicyKey,
                RegistryKey userPolicyKey)
            {
                return new AccessSettings()
                {
                    //
                    // Settings that can be overridden by policy.
                    //
                    // NB. Default values must be kept consistent with the
                    //     ADMX policy templates!
                    // NB. Machine policies override user policies, and
                    //     user policies override settings.
                    //
                    WorkforcePoolProvider = RegistryStringSetting.FromKey(
                            "WorkforcePoolProvider",
                            "Workforce pool provider locator",
                            null,
                            null,
                            null, // No locator => workforce identity is disabled.
                            settingsKey,
                            s => s == null || WorkforcePoolProviderLocator.TryParse(s, out var _))
                        .ApplyPolicy(userPolicyKey)
                        .ApplyPolicy(machinePolicyKey),
                    PrivateServiceConnectEndpoint = RegistryStringSetting.FromKey(
                            "PrivateServiceConnectEndpoint",
                            "Private Service Connect endpoint",
                            null,
                            null,
                            null, // No endpoint => PSC disabled.
                            settingsKey,
                            _ => true)
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

                    //
                    // User preferences. These cannot be overridden by policy.
                    //
                    DeviceCertificateSelector = RegistryStringSetting.FromKey(
                        "DeviceCertificateSelector",
                        "DeviceCertificateSelector",
                        null,
                        null,
                        DeviceEnrollment.DefaultDeviceCertificateSelector,
                        settingsKey,
                        selector => selector == null || ChromeCertificateSelector.TryParse(selector, out var _)),
                    ConnectionLimit = RegistryDwordSetting.FromKey(
                        "ConnectionLimit",
                        "ConnectionLimit",
                        null,
                        null,
                        16,
                        settingsKey,
                        1,
                        32)
                };
            }
        }
    }
}
