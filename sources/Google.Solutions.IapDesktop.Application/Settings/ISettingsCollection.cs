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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public interface ISettingsCollection
    {
        IEnumerable<ISetting> Settings { get; }
    }

    public static class SettingsCollectionExtensions
    {
        private static IEnumerable<ISetting> Overlay(
            ISettingsCollection baseSettings,
            ISettingsCollection overlaySettings)
        {
            var overlaySettingsByKey = overlaySettings.Settings.ToDictionary(
                s => s.Key,
                s => s);

            foreach (var baseSetting in baseSettings.Settings)
            {
                if (overlaySettingsByKey.TryGetValue(
                    baseSetting.Key,
                    out var overlaySetting))
                {
                    // Setting exists in both collections => overlay.
                    overlaySettingsByKey.Remove(overlaySetting.Key);
                    yield return baseSetting.OverlayBy(overlaySetting);
                }
                else
                {
                    // Setting only exists in base collection => keep.
                    yield return baseSetting;
                }
            }

            foreach (var overlaySetting in overlaySettingsByKey.Values)
            {
                // Setting only exists in overlay => add
                yield return overlaySetting;
            }
        }

        public static ISettingsCollection OverlayBy(
            this ISettingsCollection baseSettings,
            ISettingsCollection overlaySettings)
        {
            return new OverlayCollection(Overlay(baseSettings, overlaySettings));
        }

        public static void ApplyValues(
            this ISettingsCollection settings,
            NameValueCollection values,
            bool ignoreFormatErrors)
        {
            foreach (var setting in settings.Settings)
            {
                var value = values.Get(setting.Key);
                if (value != null)
                {
                    try
                    {
                        setting.Value = value;
                    }
                    catch (FormatException) when (ignoreFormatErrors)
                    {
                        // Ignore, keeping the previous value.
                    }
                }
            }
        }

        private class OverlayCollection : ISettingsCollection
        {
            public IEnumerable<ISetting> Settings { get; }

            public OverlayCollection(IEnumerable<ISetting> settings)
            {
                this.Settings = settings;
            }
        }
    }
}
