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
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    [SkipCodeCoverage("UI code")]
    internal partial class GeneralOptionsSheet : UserControl, IPropertiesSheetView
    {
        public GeneralOptionsSheet()
        {
            InitializeComponent();
        }

        public Type ViewModel => typeof(GeneralOptionsViewModel);

        public void Bind(
            PropertiesSheetViewModelBase viewModelBase,
            IBindingContext bindingContext)
        {
            var viewModel = (GeneralOptionsViewModel)viewModelBase;

            //
            // Update check.
            //
            this.updateBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsUpdateCheckEditable,
                bindingContext);
            this.enableUpdateCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsUpdateCheckEnabled,
                bindingContext);
            this.lastCheckLabel.BindReadonlyProperty(
                c => c.Text,
                viewModel,
                m => m.LastUpdateCheck,
                bindingContext);

            //
            // Browser integration.
            //
            this.enableBrowserIntegrationCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsBrowserIntegrationEnabled,
                bindingContext);
            this.browserIntegrationLink.BindObservableCommand(
                viewModel,
                m => m.OpenBrowserIntegrationHelp,
                bindingContext);

            //
            // Telemetry.
            //
            this.telemetryBox.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsTelemetryEditable,
                bindingContext);
            this.enableTelemetryCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsTelemetryEnabled,
                bindingContext);
            this.telemetryLink.BindObservableCommand(
                viewModel,
                m => m.OpenTelemetryHelp,
                bindingContext);
        }
    }
}
