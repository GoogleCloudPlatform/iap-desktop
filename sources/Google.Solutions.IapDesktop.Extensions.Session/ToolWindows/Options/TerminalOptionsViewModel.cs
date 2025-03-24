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

using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Terminal.Controls;
using System.Diagnostics;
using System.Drawing;

#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    [Service(ServiceLifetime.Transient)]
    public class TerminalOptionsViewModel : OptionsViewModelBase<ITerminalSettings>
    {
        public TerminalOptionsViewModel(
            ITerminalSettingsRepository settingsRepository)
            : base("Terminal", settingsRepository)
        {
            var settings = settingsRepository.GetSettings();

            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled = ObservableProperty.Build(
                settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value);
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = ObservableProperty.Build(
                settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value);
            this.IsQuoteConvertionOnPasteEnabled = ObservableProperty.Build(
                settings.IsQuoteConvertionOnPasteEnabled.Value);
            this.IsBracketedPasteEnabled = ObservableProperty.Build(
                settings.IsBracketedPasteEnabled.Value);
            this.IsScrollingUsingCtrlHomeEndEnabled = ObservableProperty.Build(
                settings.IsScrollingUsingCtrlHomeEndEnabled.Value);
            this.IsScrollingUsingCtrlPageUpDownEnabled = ObservableProperty.Build(
                settings.IsScrollingUsingCtrlPageUpDownEnabled.Value);
            this.TerminalFont = ObservableProperty.Build<Font>(new Font(
                settings.FontFamily.Value,
                TerminalSettings.FontSizeFromDword(settings.FontSizeAsDword.Value)));
            this.TerminalForegroundColor = ObservableProperty.Build<Color>(
                Color.FromArgb(settings.ForegroundColorArgb.Value));
            this.TerminalBackgroundColor = ObservableProperty.Build<Color>(
                Color.FromArgb(settings.BackgroundColorArgb.Value));
            this.CaretStyle = ObservableProperty.Build(settings.CaretStyle.Value);

            MarkDirtyWhenPropertyChanges(this.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
            MarkDirtyWhenPropertyChanges(this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);
            MarkDirtyWhenPropertyChanges(this.IsQuoteConvertionOnPasteEnabled);
            MarkDirtyWhenPropertyChanges(this.IsBracketedPasteEnabled);
            MarkDirtyWhenPropertyChanges(this.IsScrollingUsingCtrlHomeEndEnabled);
            MarkDirtyWhenPropertyChanges(this.IsScrollingUsingCtrlPageUpDownEnabled);
            MarkDirtyWhenPropertyChanges(this.TerminalFont);
            MarkDirtyWhenPropertyChanges(this.TerminalForegroundColor);
            MarkDirtyWhenPropertyChanges(this.TerminalBackgroundColor);
            MarkDirtyWhenPropertyChanges(this.CaretStyle);

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Save(ITerminalSettings settings)
        {
            Debug.Assert(this.IsDirty.Value);

            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value =
                this.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value =
                this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;
            settings.IsQuoteConvertionOnPasteEnabled.Value =
                this.IsQuoteConvertionOnPasteEnabled.Value;
            settings.IsBracketedPasteEnabled.Value =
                this.IsBracketedPasteEnabled.Value;
            settings.IsScrollingUsingCtrlHomeEndEnabled.Value =
                this.IsScrollingUsingCtrlHomeEndEnabled.Value;
            settings.IsScrollingUsingCtrlPageUpDownEnabled.Value =
                this.IsScrollingUsingCtrlPageUpDownEnabled.Value;
            settings.FontFamily.Value =
                this.TerminalFont.Value.FontFamily.Name;
            settings.FontSizeAsDword.Value =
                TerminalSettings.DwordFromFontSize(this.TerminalFont.Value.Size);
            settings.ForegroundColorArgb.Value =
                this.TerminalForegroundColor.Value.ToArgb();
            settings.BackgroundColorArgb.Value =
                this.TerminalBackgroundColor.Value.ToArgb();
            settings.CaretStyle.Value =
                this.CaretStyle.Value;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public float MaximumFontSize => TerminalSettings.MaximumFontSize;
        public float MinimumFontSize => TerminalSettings.MinimumFontSize;

        public ObservableProperty<bool> IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; }

        public ObservableProperty<bool> IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; }

        public ObservableProperty<bool> IsQuoteConvertionOnPasteEnabled { get; }

        public ObservableProperty<bool> IsBracketedPasteEnabled { get; }

        public ObservableProperty<bool> IsScrollingUsingCtrlHomeEndEnabled { get; }

        public ObservableProperty<bool> IsScrollingUsingCtrlPageUpDownEnabled { get; }

        public ObservableProperty<Font> TerminalFont { get; }

        public ObservableProperty<Color> TerminalForegroundColor { get; }

        public ObservableProperty<Color> TerminalBackgroundColor { get; }

        public ObservableProperty<VirtualTerminal.CaretStyle> CaretStyle { get; }
    }
}
