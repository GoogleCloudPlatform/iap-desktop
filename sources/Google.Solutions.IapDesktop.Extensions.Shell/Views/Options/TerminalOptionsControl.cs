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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    [SkipCodeCoverage("UI code")]
    public partial class TerminalOptionsControl : UserControl
    {
        private readonly TerminalOptionsViewModel viewModel;

        public TerminalOptionsControl(TerminalOptionsViewModel viewModel)
        {
            this.viewModel = viewModel;

            InitializeComponent();

            //
            // Clipboard box.
            //
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsCopyPasteUsingCtrlCAndCtrlVEnabled,
                this.Container);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled,
                this.Container);
            this.convertTypographicQuotesCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsQuoteConvertionOnPasteEnabled,
                this.Container);

            //
            // Text selection box.
            //
            this.selectUsingShiftArrrowEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsSelectUsingShiftArrrowEnabled,
                this.Container);
            this.selectAllUsingCtrlAEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsSelectAllUsingCtrlAEnabled,
                this.Container);
            this.navigationUsingControlArrrowEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsNavigationUsingControlArrrowEnabled,
                this.Container);

            //
            // Scrolling box.
            //
            this.scrollUsingCtrlUpDownCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsScrollingUsingCtrlUpDownEnabled,
                this.Container);
            this.scrollUsingCtrlHomeEndcheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsScrollingUsingCtrlHomeEndEnabled,
                this.Container);

            //
            // Font box.
            //
            this.terminalLook.BindReadonlyProperty(
                c => c.Font,
                viewModel,
                m => m.Font,
                this.Container);
        }

        private float PointsToPixelRatio
        {
            get
            {
                using (var graphics = this.CreateGraphics())
                {
                    return graphics.DpiX / 72;
                }
            }
        }

        private void selectFontButton_Click(object sender, System.EventArgs e)
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
                MinSize = (int)Math.Ceiling(this.viewModel.MinimumFontSize * PointsToPixelRatio),
                MaxSize = (int)Math.Floor(this.viewModel.MaximumFontSize * PointsToPixelRatio),

                Font = this.viewModel.Font
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Strip the font style.
                    this.viewModel.Font = new Font(
                        dialog.Font.FontFamily,
                        dialog.Font.Size);
                }
            }
        }
    }
}
