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
using Google.Solutions.Settings.Collection;
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
        ISetting<string> PrivateServiceConnectEndpoint { get; }

        /// <summary>
        /// Enable BeyondCorp certificate-based access.
        /// </summary>
        ISetting<bool> IsDeviceCertificateAuthenticationEnabled { get; }

        /// <summary>
        /// AutoSelectCertificateForUrl-formatted selector for certificate.
        /// </summary>
        ISetting<string> DeviceCertificateSelector { get; }

        /// <summary>
        /// Maximum number of connections per API endpoint.
        /// </summary>
        ISetting<int> ConnectionLimit { get; }

        /// <summary>
        /// Workforce pool provider locator. 
        /// 
        /// When set, authentication is performed using workforce
        /// identity instead of Gaia.
        /// </summary>
        ISetting<string> WorkforcePoolProvider { get; }
    }

    /// <summary>
    /// Registry-backed repository for access-related settings.
    /// </summary>
    public class AccessSettingsRepository
        : GroupPolicyAwareRepository<IAccessSettings>
    {
        public AccessSettingsRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
            : base(settingsKey, machinePolicyKey, userPolicyKey)
        {
        }

        protected override IAccessSettings LoadSettings(ISettingsStore store)
            => new AccessSettings(store);

        //---------------------------------------------------------------------
        // Inner class.
        //---------------------------------------------------------------------

        private class AccessSettings : IAccessSettings
        {

            public ISetting<string> WorkforcePoolProvider { get; }

            public ISetting<string> PrivateServiceConnectEndpoint { get; }

            public ISetting<bool> IsDeviceCertificateAuthenticationEnabled { get; }

            public ISetting<string> DeviceCertificateSelector { get; }

            public ISetting<int> ConnectionLimit { get; }

            public IEnumerable<ISetting> Settings => new ISetting[]
            {
                this.WorkforcePoolProvider,
                this.PrivateServiceConnectEndpoint,
                this.IsDeviceCertificateAuthenticationEnabled,
                this.DeviceCertificateSelector,
                this.ConnectionLimit
            };


            internal AccessSettings(ISettingsStore store)
            {
                //
                // Settings that can be overridden by policy.
                //
                // NB. Default values must be kept consistent with the
                //     ADMX policy templates!
                //
                this.WorkforcePoolProvider = store.Read<string>(
                    "WorkforcePoolProvider",
                    "Workforce pool provider locator",
                    null,
                    null,
                    null, // No locator => workforce identity is disabled.
                    s => s == null || WorkforcePoolProviderLocator.TryParse(s, out var _));
                this.PrivateServiceConnectEndpoint = store.Read<string>(
                    "PrivateServiceConnectEndpoint",
                    "Private Service Connect endpoint",
                    null,
                    null,
                    null, // No endpoint => PSC disabled.
                    _ => true);
                this.IsDeviceCertificateAuthenticationEnabled = store.Read<bool>(
                    "IsDeviceCertificateAuthenticationEnabled",
                    "IsDeviceCertificateAuthenticationEnabled",
                    null,
                    null,
                    false);

                //
                // User preferences. These cannot be overridden by policy.
                //
                this.DeviceCertificateSelector = store.Read<string>(
                    "DeviceCertificateSelector",
                    "DeviceCertificateSelector",
                    null,
                    null,
                    DeviceEnrollment.DefaultDeviceCertificateSelector,
                    selector => selector == null || ChromeCertificateSelector.TryParse(selector, out var _));
                this.ConnectionLimit = store.Read<int>(
                    "ConnectionLimit",
                    "ConnectionLimit",
                    null,
                    null,
                    16,
                    Predicate.InRange(1, 32));
            }
        }
    }
}
