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

using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.ActiveDirectory
{
    public partial class JoinDialog : Form
    {
        private readonly JoinViewModel viewModel;

        public JoinDialog()
        {
            InitializeComponent();

            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;

            this.viewModel = new JoinViewModel();
            this.domainText.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.DomainName,
                this.Container);
            this.domainWarning.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsDomainNameInvalid,
                this.Container);

            this.computerNameText.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ComputerName,
                this.Container);
            this.computerNameWarning.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsComputerNameInvalid,
                this.Container);

            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsOkButtonEnabled,
                this.Container);
        }

        public ObservableProperty<string> ComputerName => this.viewModel.ComputerName;

        public ObservableProperty<string> DomainName => this.viewModel.DomainName;
    }
}
