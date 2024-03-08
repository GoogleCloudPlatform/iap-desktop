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
using System.Diagnostics;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    [Service(ServiceLifetime.Transient)]
    public class TerminalOptionsViewModel : OptionsViewModelBase<ITerminalSettings>
    {
        public TerminalOptionsViewModel(
            ITerminalSettingsRepository settingsRepository)
            : base("Terminal", settingsRepository)
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled = ObservableProperty.Build(false);
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = ObservableProperty.Build(false);
            this.IsSelectAllUsingCtrlAEnabled = ObservableProperty.Build(false);
            this.IsSelectUsingShiftArrrowEnabled = ObservableProperty.Build(false);
            this.IsQuoteConvertionOnPasteEnabled = ObservableProperty.Build(false);
            this.IsNavigationUsingControlArrrowEnabled = ObservableProperty.Build(false);
            this.IsScrollingUsingCtrlUpDownEnabled = ObservableProperty.Build(false);
            this.IsScrollingUsingCtrlHomeEndEnabled = ObservableProperty.Build(false);
            this.TerminalFont = ObservableProperty.Build<Font>(null);
            this.TerminalForegroundColor = ObservableProperty.Build<Color>(Color.White);
            this.TerminalBackgroundColor = ObservableProperty.Build<Color>(Color.Black);

            MarkDirtyWhenPropertyChanges(this.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
            MarkDirtyWhenPropertyChanges(this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);
            MarkDirtyWhenPropertyChanges(this.IsSelectAllUsingCtrlAEnabled);
            MarkDirtyWhenPropertyChanges(this.IsSelectUsingShiftArrrowEnabled);
            MarkDirtyWhenPropertyChanges(this.IsQuoteConvertionOnPasteEnabled);
            MarkDirtyWhenPropertyChanges(this.IsNavigationUsingControlArrrowEnabled);
            MarkDirtyWhenPropertyChanges(this.IsScrollingUsingCtrlUpDownEnabled);
            MarkDirtyWhenPropertyChanges(this.IsScrollingUsingCtrlHomeEndEnabled);
            MarkDirtyWhenPropertyChanges(this.TerminalFont);
            MarkDirtyWhenPropertyChanges(this.TerminalForegroundColor);
            MarkDirtyWhenPropertyChanges(this.TerminalBackgroundColor);

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Load(ITerminalSettings settings)
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value =
                settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value =
                settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;
            this.IsSelectAllUsingCtrlAEnabled.Value =
                settings.IsSelectAllUsingCtrlAEnabled.Value;
            this.IsSelectUsingShiftArrrowEnabled.Value =
                settings.IsSelectUsingShiftArrrowEnabled.Value;
            this.IsQuoteConvertionOnPasteEnabled.Value =
                settings.IsQuoteConvertionOnPasteEnabled.Value;
            this.IsNavigationUsingControlArrrowEnabled.Value =
                settings.IsNavigationUsingControlArrrowEnabled.Value;
            this.IsScrollingUsingCtrlUpDownEnabled.Value =
                settings.IsScrollingUsingCtrlUpDownEnabled.Value;
            this.IsScrollingUsingCtrlHomeEndEnabled.Value =
                settings.IsScrollingUsingCtrlHomeEndEnabled.Value;
            this.TerminalFont.Value = new Font(
                settings.FontFamily.Value,
                TerminalSettingsRepository.FontSizeFromDword(settings.FontSizeAsDword.Value));
            this.TerminalForegroundColor.Value = Color.FromArgb(
                settings.ForegroundColorArgb.Value);
            this.TerminalBackgroundColor.Value = Color.FromArgb(
                settings.BackgroundColorArgb.Value);
        }

        protected override void Save(ITerminalSettings settings)
        {
            Debug.Assert(this.IsDirty.Value);

            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value =
                this.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value =
                this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;
            settings.IsSelectAllUsingCtrlAEnabled.Value =
                this.IsSelectAllUsingCtrlAEnabled.Value;
            settings.IsSelectUsingShiftArrrowEnabled.Value =
                this.IsSelectUsingShiftArrrowEnabled.Value;
            settings.IsQuoteConvertionOnPasteEnabled.Value =
                this.IsQuoteConvertionOnPasteEnabled.Value;
            settings.IsNavigationUsingControlArrrowEnabled.Value =
                this.IsNavigationUsingControlArrrowEnabled.Value;
            settings.IsScrollingUsingCtrlUpDownEnabled.Value =
                this.IsScrollingUsingCtrlUpDownEnabled.Value;
            settings.IsScrollingUsingCtrlHomeEndEnabled.Value =
                this.IsScrollingUsingCtrlHomeEndEnabled.Value;
            settings.FontFamily.Value =
                this.TerminalFont.Value.FontFamily.Name;
            settings.FontSizeAsDword.Value =
                TerminalSettingsRepository.DwordFromFontSize(this.TerminalFont.Value.Size);
            settings.ForegroundColorArgb.Value =
                this.TerminalForegroundColor.Value.ToArgb();
            settings.BackgroundColorArgb.Value =
                this.TerminalBackgroundColor.Value.ToArgb();
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public float MaximumFontSize => Controls.TerminalFont.MaximumSize;
        public float MinimumFontSize => Controls.TerminalFont.MinimumSize;

        public ObservableProperty<bool> IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; }

        public ObservableProperty<bool> IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; }

        public ObservableProperty<bool> IsSelectAllUsingCtrlAEnabled { get; }

        public ObservableProperty<bool> IsSelectUsingShiftArrrowEnabled { get; }

        public ObservableProperty<bool> IsQuoteConvertionOnPasteEnabled { get; }

        public ObservableProperty<bool> IsNavigationUsingControlArrrowEnabled { get; }

        public ObservableProperty<bool> IsScrollingUsingCtrlUpDownEnabled { get; }

        public ObservableProperty<bool> IsScrollingUsingCtrlHomeEndEnabled { get; }

        public ObservableProperty<Font> TerminalFont { get; }

        public ObservableProperty<Color> TerminalForegroundColor { get; }

        public ObservableProperty<Color> TerminalBackgroundColor { get; }
    }
}
