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
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Profile.Settings.Registry;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.Mvvm.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    /// <summary>
    /// Registry-backed repository for terminal settings.
    /// 
    /// Service is a singleton so that objects can subscribe to events.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class TerminalSettingsRepository : RegistryRepositoryBase<TerminalSettings>
    {
        public event EventHandler<EventArgs<TerminalSettings>> SettingsChanged;

        public TerminalSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Precondition.ExpectNotNull(baseKey, nameof(baseKey));
        }

        public TerminalSettingsRepository(UserProfile profile)
            : this(profile.SettingsKey.CreateSubKey("Terminal"))
        {
            profile.ExpectNotNull(nameof(profile));
        }

        protected override TerminalSettings LoadSettings(RegistryKey key)
            => TerminalSettings.FromKey(key);

        public override void SetSettings(TerminalSettings settings)
        {
            base.SetSettings(settings);
            this.SettingsChanged?.Invoke(this, new EventArgs<TerminalSettings>(settings));
        }
    }

    public class TerminalSettings : ISettingsCollection
    {
        public RegistryBoolSetting IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; private set; }
        public RegistryBoolSetting IsSelectAllUsingCtrlAEnabled { get; private set; }
        public RegistryBoolSetting IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; private set; }
        public RegistryBoolSetting IsSelectUsingShiftArrrowEnabled { get; private set; }
        public RegistryBoolSetting IsQuoteConvertionOnPasteEnabled { get; private set; }
        public RegistryBoolSetting IsNavigationUsingControlArrrowEnabled { get; private set; }
        public RegistryBoolSetting IsScrollingUsingCtrlUpDownEnabled { get; private set; }
        public RegistryBoolSetting IsScrollingUsingCtrlHomeEndEnabled { get; private set; }
        public RegistryStringSetting FontFamily { get; private set; }
        public RegistryDwordSetting FontSizeAsDword { get; private set; }
        public RegistryDwordSetting ForegroundColorArgb { get; private set; }
        public RegistryDwordSetting BackgroundColorArgb { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled,
            this.IsSelectAllUsingCtrlAEnabled,
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled,
            this.IsSelectUsingShiftArrrowEnabled,
            this.IsQuoteConvertionOnPasteEnabled,
            this.IsNavigationUsingControlArrrowEnabled,
            this.IsScrollingUsingCtrlUpDownEnabled,
            this.IsScrollingUsingCtrlHomeEndEnabled,
            this.FontFamily,
            this.FontSizeAsDword,
            this.ForegroundColorArgb,
            this.BackgroundColorArgb
        };

        private TerminalSettings()
        {
        }

        public static TerminalSettings FromKey(RegistryKey registryKey)
        {
            return new TerminalSettings()
            {
                IsCopyPasteUsingCtrlCAndCtrlVEnabled = RegistryBoolSetting.FromKey(
                    "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                    "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsSelectAllUsingCtrlAEnabled = RegistryBoolSetting.FromKey(
                    "IsSelectAllUsingCtrlAEnabled",
                    "IsSelectAllUsingCtrlAEnabled",
                    null,
                    null,
                    false,
                    registryKey),
                IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = RegistryBoolSetting.FromKey(
                    "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                    "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsSelectUsingShiftArrrowEnabled = RegistryBoolSetting.FromKey(
                    "IsSelectUsingShiftArrrowEnabled",
                    "IsSelectUsingShiftArrrowEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsQuoteConvertionOnPasteEnabled = RegistryBoolSetting.FromKey(
                    "IsQuoteConvertionOnPasteEnabled",
                    "IsQuoteConvertionOnPasteEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsNavigationUsingControlArrrowEnabled = RegistryBoolSetting.FromKey(
                    "IsNavigationUsingControlArrrowEnabled",
                    "IsNavigationUsingControlArrrowEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsScrollingUsingCtrlUpDownEnabled = RegistryBoolSetting.FromKey(
                    "IsScrollingUsingCtrlUpDownEnabled",
                    "IsScrollingUsingCtrlUpDownEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsScrollingUsingCtrlHomeEndEnabled = RegistryBoolSetting.FromKey(
                    "IsScrollingUsingCtrlHomeEndEnabled",
                    "IsScrollingUsingCtrlHomeEndEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                FontFamily = RegistryStringSetting.FromKey(
                    "FontFamily",
                    "FontFamily",
                    null,
                    null,
                    TerminalFont.DefaultFontFamily,
                    registryKey,
                    f => f == null || TerminalFont.IsValidFont(f)),
                FontSizeAsDword = RegistryDwordSetting.FromKey(
                    "FontSize",
                    "FontSize",
                    null,
                    null,
                    DwordFromFontSize(TerminalFont.DefaultSize),
                    registryKey,
                    DwordFromFontSize(TerminalFont.MinimumSize),
                    DwordFromFontSize(TerminalFont.MaximumSize)),
                ForegroundColorArgb = RegistryDwordSetting.FromKey(
                    "ForegroundColor",
                    "ForegroundColor",
                    null,
                    null,
                    Color.White.ToArgb(),
                    registryKey,
                    Color.Black.ToArgb(),
                    Color.White.ToArgb()),
                BackgroundColorArgb = RegistryDwordSetting.FromKey(
                    "BackgroundColor",
                    "BackgroundColor",
                    null,
                    null,
                    DefaultBackgroundColor.ToArgb(),
                    registryKey,
                    Color.Black.ToArgb(),
                    Color.White.ToArgb())
            };
        }

        //
        // Font sizes are floats. To avoid loss of precision,
        // multiple them by 100 before coercing them into a DWORD.
        //

        public static float FontSizeFromDword(int dw) => (float)dw / 100;
        public static int DwordFromFontSize(float fontSize) => (int)(fontSize * 100);

        //
        // Use a dark gray as default (xterm 236).
        //
#if DEBUG
        internal static Color DefaultBackgroundColor = Color.DarkBlue;
#else
        internal static Color DefaultBackgroundColor = Color.FromArgb(48, 48, 48);
#endif
    }
}
