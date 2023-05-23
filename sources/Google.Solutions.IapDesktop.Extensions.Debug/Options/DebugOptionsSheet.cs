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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Debug.Options
{
    [Service]
    [ServiceCategory(typeof(IPropertiesSheetView))]
    public partial class DebugOptionsSheet : UserControl, IPropertiesSheetView
    {
        public DebugOptionsSheet()
        {
            InitializeComponent();
        }

        public Type ViewModel => typeof(DebugOptionsSheetViewModel);

        public void Bind(PropertiesSheetViewModelBase viewModelBase, IBindingContext context)
        {
            var viewModel = (DebugOptionsSheetViewModel)viewModelBase;

            this.dirtyCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsDirty,
                context);
            this.failToApplyChangesCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.FailToApplyChanges,
                context);
        }
    }
}
