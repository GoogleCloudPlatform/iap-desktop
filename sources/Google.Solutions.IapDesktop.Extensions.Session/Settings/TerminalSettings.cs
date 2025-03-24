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
using Google.Solutions.Mvvm.Drawing;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Terminal.Controls;
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
        ISetting<bool> IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; }
        ISetting<bool> IsQuoteConvertionOnPasteEnabled { get; }
        ISetting<bool> IsBracketedPasteEnabled { get; }
        ISetting<bool> IsScrollingUsingCtrlHomeEndEnabled { get; }
        ISetting<bool> IsScrollingUsingCtrlPageUpDownEnabled { get; }
        ISetting<string> FontFamily { get; }
        ISetting<int> FontSizeAsDword { get; }
        ISetting<int> ForegroundColorArgb { get; }
        ISetting<int> BackgroundColorArgb { get; }
        ISetting<VirtualTerminal.CaretStyle> CaretStyle { get; }
    }

    internal class TerminalSettings : ITerminalSettings
    {
        //
        // Use a dark gray as default (xterm 236).
        //
#if DEBUG
        internal static Color DefaultBackgroundColor = Color.DarkBlue;
#else
        internal static Color DefaultBackgroundColor = Color.FromArgb(48, 48, 48);
#endif

        internal const string DefaultFontFamily = "Consolas";
        internal const float DefaultFontSize = 9.75f;
        internal const float MinimumFontSize = 4.0f;
        internal const float MaximumFontSize = 36.0f;

        /// <summary>
        /// Static, empty settings. Intended for testing only.
        /// </summary>
        internal static TerminalSettings Default
        {
            get => new TerminalSettings(
                new DictionarySettingsStore(new Dictionary<string, string>()));
        }

        //
        // Font sizes are floats. To avoid loss of precision,
        // multiple them by 100 before coercing them into a DWORD.
        //

        internal static float FontSizeFromDword(int dw) => (float)dw / 100;
        internal static int DwordFromFontSize(float fontSize) => (int)(fontSize * 100);

        internal static bool IsValidFont(string fontFamily)
        {
            try
            {
                using (var font = new Font(fontFamily, DefaultFontSize))
                {
                    return font.IsMonospaced();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public ISetting<bool> IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; }
        public ISetting<bool> IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; }
        public ISetting<bool> IsQuoteConvertionOnPasteEnabled { get; }
        public ISetting<bool> IsBracketedPasteEnabled { get; }
        public ISetting<bool> IsScrollingUsingCtrlHomeEndEnabled { get; }
        public ISetting<bool> IsScrollingUsingCtrlPageUpDownEnabled { get; }
        public ISetting<string> FontFamily { get; }
        public ISetting<int> FontSizeAsDword { get; }
        public ISetting<int> ForegroundColorArgb { get; }
        public ISetting<int> BackgroundColorArgb { get; }
        public ISetting<VirtualTerminal.CaretStyle> CaretStyle { get; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled,
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled,
            this.IsQuoteConvertionOnPasteEnabled,
            this.IsBracketedPasteEnabled,
            this.IsScrollingUsingCtrlHomeEndEnabled,
            this.IsScrollingUsingCtrlPageUpDownEnabled,
            this.FontFamily,
            this.FontSizeAsDword,
            this.ForegroundColorArgb,
            this.BackgroundColorArgb,
            this.CaretStyle,
        };

        internal TerminalSettings(ISettingsStore store)
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled = store.Read<bool>(
                "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                null,
                null,
                true);
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = store.Read<bool>(
                "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                null,
                null,
                true);
            this.IsQuoteConvertionOnPasteEnabled = store.Read<bool>(
                "IsQuoteConvertionOnPasteEnabled",
                "IsQuoteConvertionOnPasteEnabled",
                null,
                null,
                true);
            this.IsBracketedPasteEnabled = store.Read<bool>(
                "IsBracketedPasteEnabled",
                "IsBracketedPasteEnabled",
                null,
                null,
                true);
            this.IsScrollingUsingCtrlHomeEndEnabled = store.Read<bool>(
                "IsScrollingUsingCtrlHomeEndEnabled",
                "IsScrollingUsingCtrlHomeEndEnabled",
                null,
                null,
                true);
            this.IsScrollingUsingCtrlPageUpDownEnabled = store.Read<bool>(
                "IsScrollingUsingCtrlPageUpDownEnabled",
                "IsScrollingUsingCtrlPageUpDownEnabled",
                null,
                null,
                true);
            this.FontFamily = store.Read<string>(
                "FontFamily",
                "FontFamily",
                null,
                null,
                DefaultFontFamily,
                f => f == null || IsValidFont(f));
            this.FontSizeAsDword = store.Read<int>(
                "FontSize",
                "FontSize",
                null,
                null,
                DwordFromFontSize(DefaultFontSize),
                Predicate.InRange(
                    DwordFromFontSize(MinimumFontSize),
                    DwordFromFontSize(MaximumFontSize)));
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
            this.CaretStyle = store.Read<VirtualTerminal.CaretStyle>(
                "CaretStyle",
                "CaretStyle",
                null,
                null,
                VirtualTerminal.CaretStyle.BlinkingBar);
        }
    }
}