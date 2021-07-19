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
    public partial class NetworkOptionsControl : UserControl
    {
        private readonly NetworkOptionsViewModel viewModel;

        public NetworkOptionsControl(NetworkOptionsViewModel viewModel)
        {
            this.viewModel = viewModel;

            InitializeComponent();

            this.proxyBox.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsProxyEditable,
                this.Container);

            this.useSystemRadioButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsSystemProxyServerEnabled,
                this.Container);
            this.openProxyControlPanelAppletButton.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsSystemProxyServerEnabled,
                this.Container);

            this.useCustomRadioButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                this.Container);
            this.addressLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                this.Container);
            this.proxyServerTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                this.Container);
            this.proxyServerTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyServer,
                this.Container);
            this.proxyPortTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerEnabled,
                this.Container);
            this.proxyPortTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyPort,
                this.Container);

            this.usePacRadioButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsProxyAutoConfigurationEnabled,
                this.Container);
            this.pacAddressLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAutoConfigurationEnabled,
                this.Container);
            this.proxyPacTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAutoConfigurationEnabled,
                this.Container);
            this.proxyPacTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyAutoconfigurationAddress,
                this.Container);

            //
            // Proxy auth.
            //

            this.proxyAuthCheckBox.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                this.Container);
            this.proxyAuthCheckBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsCustomProxyServerOrProxyAutoConfigurationEnabled,
                this.Container);

            this.proxyAuthUsernameTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyUsername,
                this.Container);
            this.proxyAuthUsernameTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                this.Container);
            this.proxyAuthUsernameLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                this.Container);

            this.proxyAuthPasswordTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProxyPassword,
                this.Container);
            this.proxyAuthPasswordTextBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                this.Container);
            this.proxyAuthPasswordLabel.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProxyAuthenticationEnabled,
                this.Container);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void openProxyControlPanelAppletButton_Click(object sender, System.EventArgs e)
            => this.viewModel.OpenProxyControlPanelApplet();

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
