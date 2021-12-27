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
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    [SkipCodeCoverage("UI code")]
    internal partial class GeneralOptionsControl : UserControl
    {
        private readonly GeneralOptionsViewModel viewModel;

        public GeneralOptionsControl(GeneralOptionsViewModel viewModel)
        {
            this.viewModel = viewModel;

            InitializeComponent();

            this.updateBox.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsUpdateCheckEditable,
                this.Container);
            this.secureConnectBox.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsDeviceCertificateAuthenticationEditable,
                this.Container);

            this.enableUpdateCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsUpdateCheckEnabled,
                this.Container);
            this.enableDcaCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsDeviceCertificateAuthenticationEnabled,
                this.Container);
            this.lastCheckLabel.BindProperty(
                c => c.Text,
                viewModel,
                m => m.LastUpdateCheck,
                this.Container);
            this.enableBrowserIntegrationCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsBrowserIntegrationEnabled,
                this.Container);
        }

        private void browserIntegrationLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenBrowserIntegrationDocs();

        private void secureConnectLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenSecureConnectDcaOverviewDocs();
    }
}
