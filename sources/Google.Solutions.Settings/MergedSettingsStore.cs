﻿//
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Merges settings from multiple stores.
    /// </summary>
    public class MergedSettingsStore : ISettingsStore
    {
        /// <summary>
        /// Stores, in order of increasing importance:
        /// 
        /// 1. "Lesser" store whose settings are being overlaid.
        /// (...)
        /// N. Store containing the most important overlay settings.
        /// </summary>
        internal IReadOnlyCollection<ISettingsStore> Stores { get; }

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
            Overlay,

            /// <summary>
            /// Values in this key (when present) override the lesser
            /// key's value and makes it read-only.
            /// </summary>
            Policy
        }

        /// <param name="stores">Stores, in order of increasing importance</param>
        /// <param name="mergeBehavior">Overlay behavior to apply</param>
        public MergedSettingsStore(
            IReadOnlyCollection<ISettingsStore> stores,
            MergeBehavior mergeBehavior)
        {
            this.Stores = stores.ExpectNotNull(nameof(stores));
            this.mergeBehavior = mergeBehavior;

            Precondition.Expect(stores.Any(), nameof(stores));
        }

        internal SettingBase<T> Merge<T>(
            SettingBase<T> lesserSetting,
            SettingBase<T> overlaySetting)
        {
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

        //---------------------------------------------------------------------
        // ISettingsStore.
        //---------------------------------------------------------------------

        public ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            Predicate<T> validate = null)
        {
            //
            // Start with the least important setting.
            //
            var setting = (SettingBase<T>)this.Stores.First().Read(
                name,
                displayName,
                description,
                category,
                defaultValue,
                validate);

            //
            // Apply overlays.
            //
            foreach (var overlayStore in this.Stores.Skip(1))
            {
                var overlaySetting = (SettingBase<T>)overlayStore.Read(
                    name,
                    displayName,
                    description,
                    category,
                    defaultValue,
                    validate);

                setting = Merge(setting, overlaySetting);
            }

            return setting;
        }

        public void Write(ISetting setting)
        {
            if (this.mergeBehavior == MergeBehavior.Policy)
            {
                //
                // Never try to write to a policy.
                //
                this.Stores.First().Write(setting);
            }
            else
            {
                this.Stores.Last().Write(setting);
            }
        }

        public void Clear()
        {
            throw new InvalidOperationException(
                "A merged store cannot be cleared");
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            foreach (var store in this.Stores)
            {
                store.Dispose();
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
