//
// Copyright 2023 Google LLC
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
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    [SkipCodeCoverage("UI code")]
    public partial class AccessOptionsSheet : UserControl, IPropertiesSheetView
    {
        public Type ViewModel => typeof(AccessOptionsViewModel);

        public AccessOptionsSheet()
        {
            InitializeComponent();

            this.pscEndpointTextBox.SetCueBanner("IP address of hostname", true);
        }

        public void Bind(
            PropertiesSheetViewModelBase viewModelBase,
            IBindingContext bindingContext)
        {
            Debug.Assert(this.connectionLimitUpDown.Minimum == 1);
            Debug.Assert(this.connectionLimitUpDown.Maximum == 32);

            var viewModel = (AccessOptionsViewModel)viewModelBase;

            //
            // PSC.
            //
            this.pscBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPrivateServiceConnectEditable,
                bindingContext);
            this.enablePscCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsPrivateServiceConnectEnabled,
                bindingContext);
            this.pscEndpointLabel.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPrivateServiceConnectEnabled,
                bindingContext);
            this.pscEndpointTextBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPrivateServiceConnectEnabled,
                bindingContext);
            this.pscEndpointTextBox.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.PrivateServiceConnectEndpoint,
                bindingContext);
            this.pscLink.BindObservableCommand(
                viewModel,
                m => m.OpenPrivateServiceConnectHelp,
                bindingContext);

            //
            // DCA.
            //
            this.dcaBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsDeviceCertificateAuthenticationEditable,
                bindingContext);
            this.enableDcaCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsDeviceCertificateAuthenticationEnabled,
                bindingContext);
            this.secureConnectLink.BindObservableCommand(
                viewModel,
                m => m.OpenCertificateAuthenticationHelp,
                bindingContext);

            //
            // Connection pool.
            //
            this.connectionLimitUpDown.BindObservableProperty(
                c => c.Value,
                viewModel,
                m => m.ConnectionPoolLimit,
                bindingContext);
        }
    }
}
