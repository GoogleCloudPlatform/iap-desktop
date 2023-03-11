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
using Google.Solutions.Mvvm.Binding;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    [SkipCodeCoverage("UI code")]
    internal partial class GeneralOptionsSheet : UserControl, IPropertiesSheetView
    {
        private GeneralOptionsViewModel viewModel;

        public GeneralOptionsSheet()
        {
            InitializeComponent();
        }

        public Type ViewModel => typeof(GeneralOptionsViewModel);

        public void Bind(PropertiesSheetViewModelBase viewModelBase, IBindingContext bindingContext)
        {
            this.viewModel = (GeneralOptionsViewModel)viewModelBase;

            this.updateBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsUpdateCheckEditable,
                bindingContext);
            this.secureConnectBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsDeviceCertificateAuthenticationEditable,
                bindingContext);

            this.enableUpdateCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsUpdateCheckEnabled,
                bindingContext);
            this.enableDcaCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsDeviceCertificateAuthenticationEnabled,
                bindingContext);
            this.lastCheckLabel.BindReadonlyProperty(
                c => c.Text,
                this.viewModel,
                m => m.LastUpdateCheck,
                bindingContext);
            this.enableBrowserIntegrationCheckBox.BindObservableProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsBrowserIntegrationEnabled,
                bindingContext);
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        private void browserIntegrationLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenBrowserIntegrationDocs();

        private void secureConnectLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenSecureConnectDcaOverviewDocs();
    }
}
