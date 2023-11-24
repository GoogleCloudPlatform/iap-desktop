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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    [SkipCodeCoverage("UI code")]
    [Service(ServiceLifetime.Transient)]
    [ServiceCategory(typeof(IPropertiesSheetView))]
    public partial class SshOptionsSheet : UserControl, IPropertiesSheetView
    {
        public SshOptionsSheet()
        {
            InitializeComponent();
        }

        public Type ViewModel => typeof(SshOptionsViewModel);

        public void Bind(PropertiesSheetViewModelBase viewModelBase, IBindingContext bindingContext)
        {
            var viewModel = (SshOptionsViewModel)viewModelBase;

            //
            // Authentication box.
            //
            this.publicKeyType.BindObservableProperty(
                viewModel.PublicKeyType,
                bindingContext);
            this.publicKeyType.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPublicKeyTypeEditable,
                bindingContext);

            this.usePersistentKeyCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.UsePersistentKey,
                bindingContext);

            this.publicKeyValidityUpDown.BindObservableProperty(
                c => c.Value,
                viewModel,
                m => m.PublicKeyValidityInDays,
                bindingContext);
            this.publicKeyValidityUpDown.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPublicKeyValidityInDaysEditable,
                bindingContext);
            this.publicKeyValidityLabel.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPublicKeyValidityInDaysEditable,
                bindingContext);
            this.daysLabel.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPublicKeyValidityInDaysEditable,
                bindingContext);

            //
            // Connection box.
            //
            this.propagateLocaleCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsPropagateLocaleEnabled,
                bindingContext);
        }
    }
}
