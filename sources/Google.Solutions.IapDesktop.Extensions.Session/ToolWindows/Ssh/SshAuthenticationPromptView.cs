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
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    [SkipCodeCoverage("UI code")]
    [Service]
    public partial class SshAuthenticationPromptView
        : Form, IView<SshAuthenticationPromptViewModel>
    {
        public SshAuthenticationPromptView()
        {
            InitializeComponent();
        }

        public void Bind(
            SshAuthenticationPromptViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.BindProperty(
               c => c.Text,
               viewModel,
               m => m.Title,
                bindingContext);
            this.headlineLabel.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Title,
                bindingContext);
            this.descriptionLabel.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Description,
                bindingContext);
            this.inputTextBox.BindProperty(
                c => c.Text,
                viewModel,
                m => m.Input,
                bindingContext);
            this.inputTextBox.BindReadonlyProperty(
                c => c.UseSystemPasswordChar,
                viewModel,
                m => m.IsPasswordMasked,
                bindingContext);
            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsOkButtonEnabled,
                bindingContext);
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
    }
}
