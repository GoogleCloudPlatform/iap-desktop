//
// Copyright 2022 Google LLC
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
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    [SkipCodeCoverage("View code")]
    public partial class NewProfileDialog : Form
    {
        private readonly NewProfileViewModel viewModel
            = new NewProfileViewModel();

        public NewProfileDialog()
        {
            InitializeComponent();

            this.profileNameTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProfileName,
                this.components);
            this.profileNameInvalidLabel.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsProfileNameInvalid,
                this.components);

            // Bind buttons.
            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsOkButtonEnabled,
                this.components);
        }

        public new NewProfileDialogResult ShowDialog(
            IWin32Window owner)
        {
            var result = base.ShowDialog(owner);
            return new NewProfileDialogResult(
                result,
                this.viewModel.ProfileName);
        }
    }

    public struct NewProfileDialogResult
    {
        public DialogResult Result { get; }
        public string ProfileName { get; }

        public NewProfileDialogResult(
            DialogResult result,
            string profileName)
        {
            Result = result;
            ProfileName = profileName;
        }
    }
}
