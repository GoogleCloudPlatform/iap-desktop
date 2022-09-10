//
// Copyright 2021 Google LLC
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
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    [SkipCodeCoverage("UI code")]
    public partial class SshAuthenticationPromptDialog : Form
    {
        public SshAuthenticationPromptDialog(
            SshAuthenticationPromptViewModel viewModel)
        {
            InitializeComponent();

            viewModel.View = this;

            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;

            this.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Title,
                this.Container);
            this.headlineLabel.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Title,
                this.Container);
            this.descriptionLabel.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Description,
                this.Container);
            this.inputTextBox.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Input,
                this.Container);
            this.inputTextBox.BindReadonlyProperty(
                c => c.UseSystemPasswordChar,
                viewModel,
                m => m.IsPasswordMasked,
                this.Container);
            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsOkButtonEnabled,
                this.Container);
        }

        private void descriptionLabel_SizeChanged(object sender, EventArgs e)
        {
            //
            // Resize window to make space for label.
            //
            this.Size = new Size(
                this.Size.Width,
                230 - 16 + this.descriptionLabel.Height);
        }

        public static string ShowPrompt(
            IWin32Window parent,
            string title,
            string description,
            bool isPasswordMasked)
        {
            var viewModel = new SshAuthenticationPromptViewModel()
            {
                Title = title,
                Description = description,
                IsPasswordMasked = isPasswordMasked
            };

            if (new SshAuthenticationPromptDialog(viewModel).ShowDialog(parent)
                == DialogResult.OK)
            {
                return viewModel.Input;
            }
            else
            {
                throw new OperationCanceledException();
            }
        }
    }
}
