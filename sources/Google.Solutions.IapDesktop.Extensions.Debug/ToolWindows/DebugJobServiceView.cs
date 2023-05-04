﻿//
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
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Runtime.InteropServices;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Extensions.Debug.ToolWindows
{
    [Service]
    public partial class DebugJobServiceView : ToolWindowViewBase, IView<DebugJobServiceViewModel>
    {
        public DebugJobServiceView(IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.DockRightAutoHide)
        {
            InitializeComponent();
        }

        public void Bind(
            DebugJobServiceViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.label.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.StatusText,
                bindingContext);
            this.runInBackgroundCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.IsBackgroundJob,
                bindingContext);
            this.spinner.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsSpinnerVisible,
                bindingContext);

            this.slowOpButton.Click += async (_, __) => await viewModel.RunJobAsync();
            this.slowNonCanelOpButton.Click += async (_, __) => await viewModel.RunCancellableJobAsync();
            this.throwExceptionButton.Click += async (_, __) => await viewModel.RunFailingJobAsync();
            this.reauthButton.Click += async (_, __) => await viewModel.TriggerReauthAsync();
        }
    }
}
