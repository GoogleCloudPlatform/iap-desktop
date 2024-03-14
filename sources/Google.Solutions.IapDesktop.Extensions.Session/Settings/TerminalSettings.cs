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
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    /// <summary>
    /// Terminal-related settings.
    /// </summary>
    public interface ITerminalSettings : ISettingsCollection
    {
        ISetting<bool> IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; }
        ISetting<bool> IsSelectAllUsingCtrlAEnabled { get; }
        ISetting<bool> IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; }
        ISetting<bool> IsSelectUsingShiftArrrowEnabled { get; }
        ISetting<bool> IsQuoteConvertionOnPasteEnabled { get; }
        ISetting<bool> IsNavigationUsingControlArrrowEnabled { get; }
        ISetting<bool> IsScrollingUsingCtrlUpDownEnabled { get; }
        ISetting<bool> IsScrollingUsingCtrlHomeEndEnabled { get; }
        ISetting<string> FontFamily { get; }
        ISetting<int> FontSizeAsDword { get; }
        ISetting<int> ForegroundColorArgb { get; }
        ISetting<int> BackgroundColorArgb { get; }
    }

    public interface ITerminalSettingsRepository : IRepository<ITerminalSettings>
    {
        event EventHandler<EventArgs<ITerminalSettings>> SettingsChanged;
    }

    /// <summary>
    /// Registry-backed repository for terminal settings.
    /// 
    /// Service is a singleton so that objects can subscribe to events.
    /// </summary>
    [Service(typeof(ITerminalSettingsRepository), ServiceLifetime.Singleton)]
    public class TerminalSettingsRepository
        : RepositoryBase<ITerminalSettings>, ITerminalSettingsRepository
    {
        //
        // Use a dark gray as default (xterm 236).
        //
#if DEBUG
        internal static Color DefaultBackgroundColor = Color.DarkBlue;
#else
        internal static Color DefaultBackgroundColor = Color.FromArgb(48, 48, 48);
#endif

        public event EventHandler<EventArgs<ITerminalSettings>> SettingsChanged;

        public TerminalSettingsRepository(RegistryKey key)
            : this(new RegistrySettingsStore(key))
        {
        }

        public TerminalSettingsRepository(ISettingsStore store) : base(store)
        {
        }

        public TerminalSettingsRepository(UserProfile profile)
            : this(new RegistrySettingsStore(profile
                .ExpectNotNull(nameof(profile))
                .SettingsKey
                .CreateSubKey("Terminal")))
        {
        }

        protected override ITerminalSettings LoadSettings(ISettingsStore store)
            => new TerminalSettings(store);

        public override void SetSettings(ITerminalSettings settings)
        {
            base.SetSettings(settings);
            this.SettingsChanged?.Invoke(this, new EventArgs<ITerminalSettings>(settings));
        }

        //
        // Font sizes are floats. To avoid loss of precision,
        // multiple them by 100 before coercing them into a DWORD.
        //

        public static float FontSizeFromDword(int dw) => (float)dw / 100;
        public static int DwordFromFontSize(float fontSize) => (int)(fontSize * 100);

        //---------------------------------------------------------------------
        // Inner class.
        //---------------------------------------------------------------------

        public class TerminalSettings : ITerminalSettings
        {
            public ISetting<bool> IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; }
            public ISetting<bool> IsSelectAllUsingCtrlAEnabled { get; }
            public ISetting<bool> IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; }
            public ISetting<bool> IsSelectUsingShiftArrrowEnabled { get; }
            public ISetting<bool> IsQuoteConvertionOnPasteEnabled { get; }
            public ISetting<bool> IsNavigationUsingControlArrrowEnabled { get; }
            public ISetting<bool> IsScrollingUsingCtrlUpDownEnabled { get; }
            public ISetting<bool> IsScrollingUsingCtrlHomeEndEnabled { get; }
            public ISetting<string> FontFamily { get; }
            public ISetting<int> FontSizeAsDword { get; }
            public ISetting<int> ForegroundColorArgb { get; }
            public ISetting<int> BackgroundColorArgb { get; }

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

            internal TerminalSettings(ISettingsStore store)
            {
                this.IsCopyPasteUsingCtrlCAndCtrlVEnabled = store.Read<bool>(
                    "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                    "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                    null,
                    null,
                    true);
                this.IsSelectAllUsingCtrlAEnabled = store.Read<bool>(
                    "IsSelectAllUsingCtrlAEnabled",
                    "IsSelectAllUsingCtrlAEnabled",
                    null,
                    null,
                    false);
                this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = store.Read<bool>(
                    "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                    "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                    null,
                    null,
                    true);
                this.IsSelectUsingShiftArrrowEnabled = store.Read<bool>(
                    "IsSelectUsingShiftArrrowEnabled",
                    "IsSelectUsingShiftArrrowEnabled",
                    null,
                    null,
                    true);
                this.IsQuoteConvertionOnPasteEnabled = store.Read<bool>(
                    "IsQuoteConvertionOnPasteEnabled",
                    "IsQuoteConvertionOnPasteEnabled",
                    null,
                    null,
                    true);
                this.IsNavigationUsingControlArrrowEnabled = store.Read<bool>(
                    "IsNavigationUsingControlArrrowEnabled",
                    "IsNavigationUsingControlArrrowEnabled",
                    null,
                    null,
                    true);
                this.IsScrollingUsingCtrlUpDownEnabled = store.Read<bool>(
                    "IsScrollingUsingCtrlUpDownEnabled",
                    "IsScrollingUsingCtrlUpDownEnabled",
                    null,
                    null,
                    true);
                this.IsScrollingUsingCtrlHomeEndEnabled = store.Read<bool>(
                    "IsScrollingUsingCtrlHomeEndEnabled",
                    "IsScrollingUsingCtrlHomeEndEnabled",
                    null,
                    null,
                    true);
                this.FontFamily = store.Read<string>(
                    "FontFamily",
                    "FontFamily",
                    null,
                    null,
                    TerminalFont.DefaultFontFamily,
                    f => f == null || TerminalFont.IsValidFont(f));
                this.FontSizeAsDword = store.Read<int>(
                    "FontSize",
                    "FontSize",
                    null,
                    null,
                    DwordFromFontSize(TerminalFont.DefaultSize),
                    Predicate.InRange(
                        DwordFromFontSize(TerminalFont.MinimumSize),
                        DwordFromFontSize(TerminalFont.MaximumSize)));
                this.ForegroundColorArgb = store.Read<int>(
                    "ForegroundColor",
                    "ForegroundColor",
                    null,
                    null,
                    Color.White.ToArgb(),
                    Predicate.InRange(
                        Color.Black.ToArgb(),
                        Color.White.ToArgb()));
                this.BackgroundColorArgb = store.Read<int>(
                    "BackgroundColor",
                    "BackgroundColor",
                    null,
                    null,
                    DefaultBackgroundColor.ToArgb(),
                    Predicate.InRange(
                        Color.Black.ToArgb(),
                        Color.White.ToArgb()));
            }
        }
    }
}
