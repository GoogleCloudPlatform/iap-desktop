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
using System.Collections.Generic;

namespace Google.Solutions.Settings.Collection
{
    /// <summary>
    /// Base class for settings repositories that support group policies.
    /// </summary>
    public abstract class GroupPolicyAwareRepository<TCollection>
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
            //     user policies override settings.
            //
            // NB. Policy keys might be null.
            //

            var storesOrderedByImportance = new List<ISettingsStore>(3)
            {
                new RegistrySettingsStore(settingsKey)
            };

            if (userPolicyKey != null)
            {
                storesOrderedByImportance.Add(new RegistrySettingsStore(userPolicyKey));
            }

            if (machinePolicyKey != null)
            {
                storesOrderedByImportance.Add(new RegistrySettingsStore(machinePolicyKey));
            }

            return new MergedSettingsStore(
                storesOrderedByImportance,
                MergedSettingsStore.MergeBehavior.Policy);
        }

        protected GroupPolicyAwareRepository(
            RegistryKey settingsKey,
            RegistryKey machinePolicyKey,
            RegistryKey userPolicyKey)
            : base(CreateMergedStore(settingsKey, machinePolicyKey, userPolicyKey))
        {
        }

        public bool IsPolicyPresent
        {
            //
            // When there's more than one store, there's a policy in play.
            //
            get => ((MergedSettingsStore)this.Store).Stores.Count > 1;
        }
    }
}
