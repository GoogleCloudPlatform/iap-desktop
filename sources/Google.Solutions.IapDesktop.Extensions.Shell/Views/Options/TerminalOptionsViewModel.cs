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
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Controls;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    public interface ITerminalOptionsSheet : IPropertiesSheet
    { }

    public class TerminalOptionsViewModel : ViewModelBase, IPropertiesSheetViewModel
    {
        private bool isCopyPasteUsingCtrlCAndCtrlVEnabled;
        private bool isSelectAllUsingCtrlAEnabled;
        private bool isCopyPasteUsingShiftInsertAndCtrlInsertEnabled;
        private bool isSelectUsingShiftArrrowEnabled;
        private bool isQuoteConvertionOnPasteEnabled;
        private bool isNavigationUsingControlArrrowEnabled;
        private bool isScrollingUsingCtrlUpDownEnabled;
        private bool isScrollingUsingCtrlHomeEndEnabled;
        private Font terminalFont;
        private Color terminalForegroundColor;
        private Color terminalBackgroundColor;

        private readonly TerminalSettingsRepository settingsRepository;

        private bool isDirty;

        public TerminalOptionsViewModel(
            TerminalSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because other tabs
            // might apply changes to other application settings.
            //
            var settings = this.settingsRepository.GetSettings();

            this.IsCopyPasteUsingCtrlCAndCtrlVEnabled =
                settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue;
            this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled =
                settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue;
            this.IsSelectAllUsingCtrlAEnabled =
                settings.IsSelectAllUsingCtrlAEnabled.BoolValue;
            this.IsSelectUsingShiftArrrowEnabled =
                settings.IsSelectUsingShiftArrrowEnabled.BoolValue;
            this.IsQuoteConvertionOnPasteEnabled =
                settings.IsQuoteConvertionOnPasteEnabled.BoolValue;
            this.IsNavigationUsingControlArrrowEnabled =
                settings.IsNavigationUsingControlArrrowEnabled.BoolValue;
            this.IsScrollingUsingCtrlUpDownEnabled =
                settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue;
            this.IsScrollingUsingCtrlHomeEndEnabled =
                settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue;
            this.TerminalFont = new Font(
                settings.FontFamily.StringValue,
                TerminalSettings.FontSizeFromDword(settings.FontSizeAsDword.IntValue));
            this.TerminalForegroundColor = Color.FromArgb(
                settings.ForegroundColorArgb.IntValue);
            this.TerminalBackgroundColor = Color.FromArgb(
                settings.BackgroundColorArgb.IntValue);

            this.isDirty = false;
        }

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "Terminal";

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public DialogResult ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            //
            // Save settings.
            //
            var settings = this.settingsRepository.GetSettings();

            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue =
                this.IsCopyPasteUsingCtrlCAndCtrlVEnabled;
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue =
                this.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled;
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue =
                this.IsSelectAllUsingCtrlAEnabled;
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue =
                this.IsSelectUsingShiftArrrowEnabled;
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue =
                this.IsQuoteConvertionOnPasteEnabled;
            settings.IsNavigationUsingControlArrrowEnabled.BoolValue =
                this.IsNavigationUsingControlArrrowEnabled;
            settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue =
                this.IsScrollingUsingCtrlUpDownEnabled;
            settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue =
                this.IsScrollingUsingCtrlHomeEndEnabled;
            settings.FontFamily.StringValue =
                this.terminalFont.FontFamily.Name;
            settings.FontSizeAsDword.IntValue =
                TerminalSettings.DwordFromFontSize(this.terminalFont.Size);
            settings.ForegroundColorArgb.IntValue =
                this.TerminalForegroundColor.ToArgb();
            settings.BackgroundColorArgb.IntValue =
                this.TerminalBackgroundColor.ToArgb();

            this.settingsRepository.SetSettings(settings);

            this.IsDirty = false;

            return DialogResult.OK;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsCopyPasteUsingCtrlCAndCtrlVEnabled
        {
            get => this.isCopyPasteUsingCtrlCAndCtrlVEnabled;
            set
            {
                this.IsDirty = true;
                this.isCopyPasteUsingCtrlCAndCtrlVEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled
        {
            get => this.isCopyPasteUsingShiftInsertAndCtrlInsertEnabled;
            set
            {
                this.IsDirty = true;
                this.isCopyPasteUsingShiftInsertAndCtrlInsertEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsSelectAllUsingCtrlAEnabled
        {
            get => this.isSelectAllUsingCtrlAEnabled;
            set
            {
                this.IsDirty = true;
                this.isSelectAllUsingCtrlAEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsSelectUsingShiftArrrowEnabled
        {
            get => this.isSelectUsingShiftArrrowEnabled;
            set
            {
                this.IsDirty = true;
                this.isSelectUsingShiftArrrowEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsQuoteConvertionOnPasteEnabled
        {
            get => this.isQuoteConvertionOnPasteEnabled;
            set
            {
                this.IsDirty = true;
                this.isQuoteConvertionOnPasteEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsNavigationUsingControlArrrowEnabled
        {
            get => this.isNavigationUsingControlArrrowEnabled;
            set
            {
                this.IsDirty = true;
                this.isNavigationUsingControlArrrowEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsScrollingUsingCtrlUpDownEnabled
        {
            get => this.isScrollingUsingCtrlUpDownEnabled;
            set
            {
                this.IsDirty = true;
                this.isScrollingUsingCtrlUpDownEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsScrollingUsingCtrlHomeEndEnabled
        {
            get => this.isScrollingUsingCtrlHomeEndEnabled;
            set
            {
                this.IsDirty = true;
                this.isScrollingUsingCtrlHomeEndEnabled = value;
                RaisePropertyChange();
            }
        }

        public Font TerminalFont
        {
            get => this.terminalFont;
            set
            {
                this.IsDirty = true;
                this.terminalFont = value;
                RaisePropertyChange();
            }
        }

        public float MaximumFontSize => Controls.TerminalFont.MaximumSize;
        public float MinimumFontSize => Controls.TerminalFont.MinimumSize;

        public Color TerminalForegroundColor
        {
            get => this.terminalForegroundColor;
            set
            {
                this.IsDirty = true;
                this.terminalForegroundColor = value;
                RaisePropertyChange();
            }
        }

        public Color TerminalBackgroundColor
        {
            get => this.terminalBackgroundColor;
            set
            {
                this.IsDirty = true;
                this.terminalBackgroundColor = value;
                RaisePropertyChange();
            }
        }
    }
}
