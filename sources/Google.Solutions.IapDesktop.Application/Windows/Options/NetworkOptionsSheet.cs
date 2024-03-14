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

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    [SkipCodeCoverage("UI code")]
    public partial class NetworkOptionsSheet : UserControl, IPropertiesSheetView
    {
        private NetworkOptionsViewModel? viewModel;

        public NetworkOptionsSheet()
        {
            InitializeComponent();
        }

        public Type ViewModel => typeof(NetworkOptionsViewModel);

        public void Bind(PropertiesSheetViewModelBase viewModelBase, IBindingContext bindingContext)
        {
            this.viewModel = (NetworkOptionsViewModel)viewModelBase;

            this.proxyBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyEditable,
                bindingContext);

            this.useSystemRadioButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsSystemProxyServerEnabled,
                bindingContext);
            this.openProxyControlPanelAppletButton.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsSystemProxyServerEnabled,
                bindingContext);

            this.useCustomRadioButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                bindingContext);
            this.addressLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                bindingContext);
            this.proxyServerTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                bindingContext);
            this.proxyServerTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyServer,
                bindingContext);
            this.proxyPortTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                bindingContext);
            this.proxyPortTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyPort,
                bindingContext);

            this.usePacRadioButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsProxyAutoConfigurationEnabled,
                bindingContext);
            this.pacAddressLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAutoConfigurationEnabled,
                bindingContext);
            this.proxyPacTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAutoConfigurationEnabled,
                bindingContext);
            this.proxyPacTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyAutoconfigurationAddress,
                bindingContext);

            //
            // Proxy auth.
            //

            this.proxyAuthCheckBox.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                bindingContext);
            this.proxyAuthCheckBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerOrProxyAutoConfigurationEnabled,
                bindingContext);

            this.proxyAuthUsernameTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyUsername,
                bindingContext);
            this.proxyAuthUsernameTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                bindingContext);
            this.proxyAuthUsernameLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                bindingContext);

            this.proxyAuthPasswordTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyPassword,
                bindingContext);
            this.proxyAuthPasswordTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                bindingContext);
            this.proxyAuthPasswordLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                bindingContext);
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        private void openProxyControlPanelAppletButton_Click(object sender, System.EventArgs e)
            => this.viewModel?.OpenProxyControlPanelApplet();

        private void proxyPortTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) &&
                !this.viewModel.IsValidProxyPort(this.proxyPortTextBox.Text + e.KeyChar))
            {
                // Invalid input -> ignore.
                e.Handled = true;
            }
        }
    }
}
