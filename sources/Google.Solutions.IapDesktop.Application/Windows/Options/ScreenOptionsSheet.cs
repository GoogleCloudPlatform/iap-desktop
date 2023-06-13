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
using Google.Solutions.Mvvm.Controls;
using System;
using System.Windows.Forms;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    [SkipCodeCoverage("UI code")]
    internal partial class ScreenOptionsSheet : UserControl, IPropertiesSheetView
    {
        public ScreenOptionsSheet()
        {
            InitializeComponent();
        }

        public Type ViewModel => typeof(ScreenOptionsViewModel);

        public void Bind(PropertiesSheetViewModelBase viewModelBase, IBindingContext bindingContext)
        {
            var viewModel = (ScreenOptionsViewModel)viewModelBase;

            this.screenPicker.BindCollection(viewModel.Devices);
        }
    }

    internal class ScreenDevicePicker : ScreenPicker<ScreenOptionsViewModel.ScreenDevice>
    {

    }
}
