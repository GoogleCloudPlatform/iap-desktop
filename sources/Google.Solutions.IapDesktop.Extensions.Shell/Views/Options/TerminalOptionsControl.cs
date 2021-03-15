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

using System.Windows.Forms;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;

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

            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsCopyPasteUsingCtrlCAndCtrlVEnabled,
                this.Container);
            this.selectAllUsingCtrlAEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsSelectAllUsingCtrlAEnabled,
                this.Container);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled,
                this.Container);
            this.selectUsingShiftArrrowEnabledCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsSelectUsingShiftArrrowEnabled,
                this.Container);
        }
    }
}
