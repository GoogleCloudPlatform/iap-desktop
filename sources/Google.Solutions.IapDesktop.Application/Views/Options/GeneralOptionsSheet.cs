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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    [SkipCodeCoverage("UI code")]
    internal partial class GeneralOptionsSheet : UserControl, IPropertiesSheet
    {
        private readonly GeneralOptionsViewModel viewModel;

        public GeneralOptionsSheet(
            ApplicationSettingsRepository settingsRepository,
            IAppProtocolRegistry protocolRegistry,
            HelpAdapter helpService)
        {
            this.viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                protocolRegistry,
                helpService);

            InitializeComponent();


            // TODO: Use shared binding context.
            var bindingContext = ViewBindingContext.CreateDummy();

            this.updateBox.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsUpdateCheckEditable,
                bindingContext);
            this.secureConnectBox.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsDeviceCertificateAuthenticationEditable,
                bindingContext);

            this.enableUpdateCheckBox.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsUpdateCheckEnabled,
                this.Container);
            this.enableDcaCheckBox.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsDeviceCertificateAuthenticationEnabled,
                this.Container);
            this.lastCheckLabel.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.LastUpdateCheck,
                this.Container);
            this.enableBrowserIntegrationCheckBox.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsBrowserIntegrationEnabled,
                this.Container);
        }

        //---------------------------------------------------------------------
        // IPropertiesSheet.
        //---------------------------------------------------------------------

        public IPropertiesSheetViewModel ViewModel => this.viewModel;

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        private void browserIntegrationLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenBrowserIntegrationDocs();

        private void secureConnectLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenSecureConnectDcaOverviewDocs();
    }
}
