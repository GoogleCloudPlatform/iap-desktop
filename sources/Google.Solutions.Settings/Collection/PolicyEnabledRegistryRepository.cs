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
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;

namespace Google.Solutions.Settings.Collection
{
    /// <summary>
    /// Base class for settings repositories that support group policies.
    /// </summary>
    public abstract class PolicyEnabledRegistryRepository<TCollection> // TODO: Rename, drop prefix
        : RepositoryBase<TCollection>
        where TCollection : ISettingsCollection
    {
        private static ISettingsStore CreateMergedStore(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
        {
            settingsKey.ExpectNotNull(nameof(settingsKey));

            //
            // NB. Machine policies override user policies, and
            //     user policies override settings. But the policy
            //     keys might be missing (null).
            //

            if (machinePolicyKey != null && userPolicyKey != null)
            {
                var policy = new MergedSettingsStore(
                    new RegistrySettingsStore(machinePolicyKey),
                    new RegistrySettingsStore(userPolicyKey),
                    MergedSettingsStore.MergeBehavior.Policy);

                return new MergedSettingsStore(
                    policy,
                    new RegistrySettingsStore(settingsKey),
                    MergedSettingsStore.MergeBehavior.Policy);
            }
            else if (machinePolicyKey != null)
            {
                return new MergedSettingsStore(
                    new RegistrySettingsStore(machinePolicyKey),
                    new RegistrySettingsStore(settingsKey),
                    MergedSettingsStore.MergeBehavior.Policy);
            }
            else if (userPolicyKey != null)
            {
                return new MergedSettingsStore(
                    new RegistrySettingsStore(userPolicyKey),
                    new RegistrySettingsStore(settingsKey),
                    MergedSettingsStore.MergeBehavior.Policy);
            }
            else
            {
                return new RegistrySettingsStore(settingsKey);
            }
        }

        protected PolicyEnabledRegistryRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey) 
            : base(CreateMergedStore(settingsKey, machinePolicyKey, userPolicyKey))
        {
        }

        public bool IsPolicyPresent 
        {
            get => this.Store is MergedSettingsStore; // TODO: test
        }
    }
}
