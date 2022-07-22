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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using System.Windows.Forms;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    [SkipCodeCoverage("UI code")]
    internal partial class ScreenOptionsControl : UserControl, Properties.IPropertiesSheet
    {
        private readonly ScreenOptionsViewModel viewModel;

        public ScreenOptionsControl(
            ApplicationSettingsRepository settingsRepository)
        {
            this.viewModel = new ScreenOptionsViewModel(settingsRepository);

            InitializeComponent();

            this.screenPicker.BindCollection(this.viewModel.Devices);
        }

        //---------------------------------------------------------------------
        // IPropertiesSheet.
        //---------------------------------------------------------------------

        public IPropertiesSheetViewModel ViewModel => this.viewModel;
    }

    public class ScreenDevicePicker : ScreenPicker<ScreenOptionsViewModel.ScreenDevice>
    {

    }
}
