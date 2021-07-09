//
// Copyright 2019 Google LLC
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
using Google.Solutions.IapDesktop.Application.Views;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials
{
    public interface IGenerateCredentialsDialog
    {
        GenerateCredentialsDialogResult ShowDialog(
            IWin32Window owner,
            string suggestedUsername);
    }

    [Service(typeof(IGenerateCredentialsDialog))]
    [SkipCodeCoverage("View code")]
    public partial class GenerateCredentialsDialog : Form, IGenerateCredentialsDialog
    {
        private readonly GenerateCredentialsViewModel viewModel
            = new GenerateCredentialsViewModel();

        public GenerateCredentialsDialog()
        {
            InitializeComponent();

            this.usernameText.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.Username,
                this.components);

            // Bind buttons.
            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsOkButtonEnabled,
                this.components);

            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;
        }

        private void usernameText_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Cancel any keypresses of disallowed characters.
            e.Handled = !this.viewModel.IsAllowedCharacterForUsername(e.KeyChar);
        }

        public GenerateCredentialsDialogResult ShowDialog(
            IWin32Window owner,
            string suggestedUsername)
        {
            this.viewModel.Username = suggestedUsername;

            var result = ShowDialog(owner);

            return new GenerateCredentialsDialogResult(
                result,
                this.viewModel.Username);
        }
    }

    public class GenerateCredentialsDialogResult
    {
        public DialogResult Result { get; }
        public string Username { get; }

        public GenerateCredentialsDialogResult(
            DialogResult result,
            string username)
        {
            Result = result;
            Username = username;
        }
    }
}
