﻿//
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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.Mvvm.Binding;
using System.Diagnostics;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    [Service(ServiceLifetime.Transient, ServiceVisibility.Global)]
    public class TerminalOptionsViewModel : OptionsViewModelBase<TerminalSettings>
    {
        public TerminalOptionsViewModel(
            TerminalSettingsRepository settingsRepository)
            : base("Terminal", settingsRepository)
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled = ObservableProperty.Build(false);
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled  = ObservableProperty.Build(false);
            this.IsSelectAllUsingCtrlAEnabled  = ObservableProperty.Build(false);
            this.IsSelectUsingShiftArrrowEnabled  = ObservableProperty.Build(false);
            this.IsQuoteConvertionOnPasteEnabled  = ObservableProperty.Build(false);
            this.IsNavigationUsingControlArrrowEnabled  = ObservableProperty.Build(false);
            this.IsScrollingUsingCtrlUpDownEnabled  = ObservableProperty.Build(false);
            this.IsScrollingUsingCtrlHomeEndEnabled  = ObservableProperty.Build(false);
            this.TerminalFont  = ObservableProperty.Build<Font>(null);
            this.TerminalForegroundColor  = ObservableProperty.Build<Color>(Color.White);
            this.TerminalBackgroundColor  = ObservableProperty.Build<Color>(Color.Black);
            
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

        protected override void Load(TerminalSettings settings)
        {
            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value =
                settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue;
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value =
                settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue;
            this.IsSelectAllUsingCtrlAEnabled.Value =
                settings.IsSelectAllUsingCtrlAEnabled.BoolValue;
            this.IsSelectUsingShiftArrrowEnabled.Value =
                settings.IsSelectUsingShiftArrrowEnabled.BoolValue;
            this.IsQuoteConvertionOnPasteEnabled.Value =
                settings.IsQuoteConvertionOnPasteEnabled.BoolValue;
            this.IsNavigationUsingControlArrrowEnabled.Value =
                settings.IsNavigationUsingControlArrrowEnabled.BoolValue;
            this.IsScrollingUsingCtrlUpDownEnabled.Value =
                settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue;
            this.IsScrollingUsingCtrlHomeEndEnabled.Value =
                settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue;
            this.TerminalFont.Value = new Font(
                settings.FontFamily.StringValue,
                TerminalSettings.FontSizeFromDword(settings.FontSizeAsDword.IntValue));
            this.TerminalForegroundColor.Value = Color.FromArgb(
                settings.ForegroundColorArgb.IntValue);
            this.TerminalBackgroundColor.Value = Color.FromArgb(
                settings.BackgroundColorArgb.IntValue);
        }

        protected override void Save(TerminalSettings settings)
        {
            Debug.Assert(this.IsDirty.Value);

            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue =
                this.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue =
                this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue =
                this.IsSelectAllUsingCtrlAEnabled.Value;
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue =
                this.IsSelectUsingShiftArrrowEnabled.Value;
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue =
                this.IsQuoteConvertionOnPasteEnabled.Value;
            settings.IsNavigationUsingControlArrrowEnabled.BoolValue =
                this.IsNavigationUsingControlArrrowEnabled.Value;
            settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue =
                this.IsScrollingUsingCtrlUpDownEnabled.Value;
            settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue =
                this.IsScrollingUsingCtrlHomeEndEnabled.Value;
            settings.FontFamily.StringValue =
                this.TerminalFont.Value.FontFamily.Name;
            settings.FontSizeAsDword.IntValue =
                TerminalSettings.DwordFromFontSize(this.TerminalFont.Value.Size);
            settings.ForegroundColorArgb.IntValue =
                this.TerminalForegroundColor.Value.ToArgb();
            settings.BackgroundColorArgb.IntValue =
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
