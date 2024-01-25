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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    [SkipCodeCoverage("UI code")]
    [Service(ServiceLifetime.Transient)]
    [ServiceCategory(typeof(IPropertiesSheetView))]
    public partial class TerminalOptionsSheet : UserControl, IPropertiesSheetView
    {
        private TerminalOptionsViewModel viewModel;
        private readonly IExceptionDialog exceptionDialog;

        public TerminalOptionsSheet(
            IExceptionDialog exceptionDialog)
        {
            this.exceptionDialog = exceptionDialog.ExpectNotNull(nameof(exceptionDialog));

            InitializeComponent();
        }

        public Type ViewModel => typeof(TerminalOptionsViewModel);

        public void Bind(PropertiesSheetViewModelBase viewModelBase, IBindingContext bindingContext)
        {
            this.viewModel = (TerminalOptionsViewModel)viewModelBase;

            //
            // Clipboard box.
            //
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsCopyPasteUsingCtrlCAndCtrlVEnabled,
                bindingContext);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled,
                bindingContext);
            this.convertTypographicQuotesCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsQuoteConvertionOnPasteEnabled,
                bindingContext);

            //
            // Text selection box.
            //
            this.selectUsingShiftArrrowEnabledCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsSelectUsingShiftArrrowEnabled,
                bindingContext);
            this.selectAllUsingCtrlAEnabledCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsSelectAllUsingCtrlAEnabled,
                bindingContext);
            this.navigationUsingControlArrrowEnabledCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsNavigationUsingControlArrrowEnabled,
                bindingContext);

            //
            // Scrolling box.
            //
            this.scrollUsingCtrlUpDownCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsScrollingUsingCtrlUpDownEnabled,
                bindingContext);
            this.scrollUsingCtrlHomeEndcheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsScrollingUsingCtrlHomeEndEnabled,
                bindingContext);

            //
            // Font box.
            //
            this.terminalLook.BindReadonlyObservableProperty(
                c => c.Font,
                this.viewModel,
                m => m.TerminalFont,
                bindingContext);
            this.terminalLook.BindReadonlyObservableProperty(
                c => c.ForeColor,
                this.viewModel,
                m => m.TerminalForegroundColor,
                bindingContext);
            this.terminalLook.BindReadonlyObservableProperty(
                c => c.BackColor,
                this.viewModel,
                m => m.TerminalBackgroundColor,
                bindingContext);
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        private float PointsToPixelRatio
        {
            get
            {
                using (var graphics = CreateGraphics())
                {
                    return graphics.DpiX / 72;
                }
            }
        }

        private void selectFontButton_Click(object sender, System.EventArgs _)
        {
            try
            {
                using (var dialog = new FontDialog()
                {
                    FixedPitchOnly = true,
                    AllowScriptChange = false,
                    FontMustExist = true,
                    ShowColor = false,
                    ShowApply = false,
                    ShowEffects = false,

                    // Sizes are in pixel, not points.
                    MinSize = (int)Math.Ceiling(this.viewModel.MinimumFontSize * this.PointsToPixelRatio),
                    MaxSize = (int)Math.Floor(this.viewModel.MaximumFontSize * this.PointsToPixelRatio),

                    Font = this.viewModel.TerminalFont.Value
                })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        // Strip the font style.
                        this.viewModel.TerminalFont.Value = new Font(
                            dialog.Font.FontFamily,
                            dialog.Font.Size);
                    }
                }
            }
            catch (Exception e)
            {
                this.exceptionDialog.Show(this, "Selecting font failed", e);
            }
        }

        private void selectTerminalColorButton_Click(object sender, EventArgs _)
        {
            try
            {
                using (var dialog = new ColorDialog()
                {
                    AllowFullOpen = true,
                    AnyColor = true,
                    SolidColorOnly = true,
                    Color = sender == this.selectBackgroundColorButton
                        ? this.viewModel.TerminalBackgroundColor.Value
                        : this.viewModel.TerminalForegroundColor.Value
                })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        if (sender == this.selectBackgroundColorButton)
                        {
                            this.viewModel.TerminalBackgroundColor.Value = dialog.Color;
                        }
                        else
                        {
                            this.viewModel.TerminalForegroundColor.Value = dialog.Color;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.exceptionDialog.Show(this, "Selecting font failed", e);
            }
        }
    }
}
