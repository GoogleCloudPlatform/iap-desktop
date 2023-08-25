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
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    [SkipCodeCoverage("View code")]
    public partial class AuthorizeOptionsView : Form, IView<AuthorizeOptionsViewModel>
    {
        public AuthorizeOptionsView()
        {
            InitializeComponent();
        }

        public void Bind(
            AuthorizeOptionsViewModel viewModel,
            IBindingContext bindingContext)
        {
            //
            // Gaia.
            //
            this.gaiaRadioButton.BindReadonlyObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsGaiaOptionChecked,
                bindingContext);

            //
            // Workforce identity.
            //
            this.workforceIdentityRadioButton.BindReadonlyObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsWorkforcePoolOptionChecked,
                bindingContext);

            this.wifLocationIdTextBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsWorkforcePoolOptionChecked,
                bindingContext);
            this.wifPoolIdTextBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsWorkforcePoolOptionChecked,
                bindingContext);
            this.wifProviderIdTextBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsWorkforcePoolOptionChecked,
                bindingContext);

            this.wifLocationIdTextBox.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.WorkforcePoolLocationId,
                bindingContext);
            this.wifPoolIdTextBox.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.WorkforcePoolId,
                bindingContext);
            this.wifProviderIdTextBox.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.WorkforcePoolProviderId,
                bindingContext);

            //
            // Bind buttons.
            //
            this.okButton.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsOkButtonEnabled,
                bindingContext);
        }
    }
}
