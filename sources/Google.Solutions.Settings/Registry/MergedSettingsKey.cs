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
using Microsoft.Win32;
using System.Diagnostics;

namespace Google.Solutions.Settings.Registry
{
    /// <summary>
    /// Merges settings from two registry keys.
    /// </summary>
    public class MergedSettingsKey : SettingsKey
    {
        /// <summary>
        /// Key whose settings are being "overlaid" by this key.
        /// </summary>
        private readonly SettingsKey lesserKey;
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

        public MergedSettingsKey(
            RegistryKey key,
            SettingsKey lesserKey,
            MergeBehavior mergeBehavior)
            : base(key)
        {
            this.lesserKey = lesserKey.ExpectNotNull(nameof(lesserKey));
            this.mergeBehavior = mergeBehavior;
        }

        public override ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            ValidateDelegate<T> validate = null)
        {
            var lesserSetting = (MappedSetting<T>)this.lesserKey.Read(
                name,
                displayName,
                description,
                category,
                defaultValue,
                validate);

            var overlaySetting = (MappedSetting<T>)base.Read(
                name,
                displayName,
                description,
                category,
                defaultValue,
                validate);

            if (this.mergeBehavior == MergeBehavior.Policy)
            {
                if (!overlaySetting.IsSpecified)
                {
                    //
                    // Policy value not defined, ignore.
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
                        false,
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
    }
}
