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

using Google.Solutions.Common.Util;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// Application theme.
    /// </summary>
    public enum ApplicationTheme
    {
        [Display(Name = "Light theme")]
        Light = 0,

        [Display(Name = "Current Windows theme")]
        System = 1,

        [Display(Name = "Dark theme")]
        Dark = 2,
        _Default = System
    }

    /// <summary>
    /// Theme-related settings.
    /// </summary>
    public interface IThemeSettings : ISettingsCollection
    {
        /// <summary>
        /// Current theme.
        /// </summary>
        IEnumSetting<ApplicationTheme> Theme { get; }

        /// <summary>
        /// Enable GDI scaling for high-DPI monitors.
        /// </summary>
        IBoolSetting IsGdiScalingEnabled { get; }
    }

    public class ThemeSettingsRepository : RegistryRepositoryBase<IThemeSettings>
    {
        public ThemeSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            baseKey.ExpectNotNull(nameof(baseKey));
        }

        protected override IThemeSettings LoadSettings(RegistryKey key)
            => ThemeSettings.FromKey(key);

        //---------------------------------------------------------------------
        // Inner class.
        //---------------------------------------------------------------------

        private class ThemeSettings : IThemeSettings
        {
            public IEnumSetting<ApplicationTheme> Theme { get; private set; }
            public IBoolSetting IsGdiScalingEnabled { get; private set; }

            public IEnumerable<ISetting> Settings => new ISetting[]
            {
                this.Theme,
                this.IsGdiScalingEnabled
            };

            private ThemeSettings()
            {
            }

            public static ThemeSettings FromKey(RegistryKey registryKey)
            {
                return new ThemeSettings()
                {
                    Theme = RegistryEnumSetting<ApplicationTheme>.FromKey(
                        "Theme",
                        "Theme",
                        null,
                        null,
                        ApplicationTheme._Default,
                        registryKey),
                    IsGdiScalingEnabled = RegistryBoolSetting.FromKey(
                        "IsGdiScalingEnabled",
                        "IsGdiScalingEnabled",
                        null,
                        null,
                        true,
                        registryKey)
                };
            }
        }
    }
}
