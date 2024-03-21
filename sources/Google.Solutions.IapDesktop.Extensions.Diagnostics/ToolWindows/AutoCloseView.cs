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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    [Service]
    public partial class AutoCloseView : DocumentWindow, IView<AutoCloseViewModel>
    {
        public AutoCloseView(
            IMainWindow mainWindow,
            ToolWindowStateRepository stateRepository)
            : base(mainWindow, stateRepository)
        {
            InitializeComponent();
        }

        public void Bind(AutoCloseViewModel viewModel, IBindingContext context)
        {
        }

        private void timer_Tick(object sender, System.EventArgs e)
        {
            CloseSafely();
        }
    }
}
