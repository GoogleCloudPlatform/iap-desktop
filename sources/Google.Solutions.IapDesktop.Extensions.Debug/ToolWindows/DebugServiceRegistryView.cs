﻿//
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
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Debug.ToolWindows
{
    [Service]
    public partial class DebugServiceRegistryView : DocumentWindow, IView<DebugServiceRegistryViewModel>
    {
        public DebugServiceRegistryView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeComponent();
        }

        public void Bind(
            DebugServiceRegistryViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.list.BindColumn(0, t => t.ServiceType.Assembly.GetName().Name);
            this.list.BindColumn(1, t => t.ServiceType.FullName);
            this.list.BindColumn(2, t => t.Lifetime.ToString());
            this.list.BindCollection(viewModel.RegisteredServices);
        }
    }

    public class ServicesListView : BindableListView<DebugServiceRegistryViewModel.Service>
    { }
}
