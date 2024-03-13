//
// Copyright 2024 Google LLC
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
using System;
using System.Diagnostics;

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Merges settings from two registry keys.
    /// </summary>
    public class MergedSettingsStore : ISettingsStore
    {
        /// <summary>
        /// Store whose settings are being "overlaid".
        /// </summary>
        private readonly ISettingsStore lesserStore;

        /// <summary>
        /// Store containing the overlay settings.
        /// </summary>
        private readonly ISettingsStore overlayStore;

        /// <summary>
        /// Overlay behavior to apply.
        /// </summary>
        private readonly MergeBehavior mergeBehavior;

        public enum MergeBehavior
        {
            /// <summary>
            /// Values in this key (when present) override the lesser
            /// key's value. The lesser key's value becomes the new
            /// default.
            /// </summary>
            Override,

            /// <summary>
            /// Values in this key (when present) override the lesser
            /// key's value and makes it read-only.
            /// </summary>
            Policy
        }

        public MergedSettingsStore(
            ISettingsStore overlayStore,
            ISettingsStore lesserStore,
            MergeBehavior mergeBehavior)
        {
            this.overlayStore = overlayStore.ExpectNotNull(nameof(overlayStore));
            this.lesserStore = lesserStore.ExpectNotNull(nameof(lesserStore));
            this.mergeBehavior = mergeBehavior;
        }

        public ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            ValidateDelegate<T> validate = null)
        {
            var lesserSetting = (SettingBase<T>)this.lesserStore.Read(
                name,
                displayName,
                description,
                category,
                defaultValue,
                validate);

            var overlaySetting = (SettingBase<T>)this.overlayStore.Read(
                name,
                displayName,
                description,
                category,
                defaultValue,
                validate);

            if (this.mergeBehavior == MergeBehavior.Policy)
            {
                if (!overlaySetting.IsSpecified || !overlaySetting.IsCurrentValueValid)
                {
                    //
                    // Policy value not defined or invalid, ignore.
                    //
                    return lesserSetting;
                }
                else
                {
                    //
                    // Use overlay's value as effective setting and make it
                    // read-only.
                    //
                    return lesserSetting.CreateSimilar(
                        overlaySetting.Value,
                        lesserSetting.DefaultValue,
                        true,
                        true);
                }
            }
            else
            {
                if (!overlaySetting.IsSpecified)
                {
                    //
                    // Overlay does not add anything new. 
                    //
                    var merged = lesserSetting.CreateSimilar(
                        lesserSetting.Value,
                        lesserSetting.Value,            // New default!
                        false,
                        lesserSetting.IsReadOnly);
                    Debug.Assert(merged.IsDefault);
                    return merged;
                }
                else
                {
                    //
                    // Use overlay's value as effective setting.
                    //
                    return lesserSetting.CreateSimilar(
                        overlaySetting.Value,
                        lesserSetting.Value,            // New default!
                        true,
                        lesserSetting.IsReadOnly);
                }
            }
        }

        public void Write(ISetting setting)
        {
            if (this.mergeBehavior == MergeBehavior.Policy)
            {
                //
                // Never try to write to the policy.
                //
                this.lesserStore.Write(setting);
            }
            else
            {
                this.overlayStore.Write(setting);
            }
        }

        public void Clear() // TODO: test
        {
            throw new InvalidOperationException(
                "A merged store cannot be cleared");
        }

        public void Dispose()
        {
            this.lesserStore.Dispose();
            this.overlayStore.Dispose();
        }
    }
}
