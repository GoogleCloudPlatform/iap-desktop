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
using Microsoft.Win32;

namespace Google.Solutions.Settings.Collection
{
    /// <summary>
    /// Base class for settings repositories that support group policies.
    /// </summary>
    public abstract class PolicyEnabledRegistryRepository<TSettings>
        : RegistryRepositoryBase<TSettings>
        where TSettings : ISettingsCollection
    {
        private readonly RegistryKey machinePolicyKey;
        private readonly RegistryKey userPolicyKey;

        protected PolicyEnabledRegistryRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey) : base(settingsKey)
        {
            settingsKey.ExpectNotNull(nameof(settingsKey));

            this.machinePolicyKey = machinePolicyKey;
            this.userPolicyKey = userPolicyKey;
        }
        protected override TSettings LoadSettings(RegistryKey key)
            => LoadSettings(key, this.machinePolicyKey, this.userPolicyKey);

        protected abstract TSettings LoadSettings(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey);

        public bool IsPolicyPresent
            => this.machinePolicyKey != null || this.userPolicyKey != null;
    }
}
