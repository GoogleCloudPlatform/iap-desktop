//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using System.Windows.Forms;

namespace Google.Solutions.Testing.Application.Mocks
{
    /// <summary>
    /// Mock dialog that returns a predefined result.
    /// </summary>
    public class MockDialog<TView, TViewModel> : IDialog<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly DialogResult result;

        public MockDialog(TViewModel viewModel, DialogResult result)
        {
            this.ViewModel = viewModel;
            this.result = result;
        }

        public IControlTheme? Theme { get; set; }

        public TViewModel ViewModel { get; }

        public void Dispose()
        {
        }

        public DialogResult ShowDialog(IWin32Window? parent)
        {
            return this.result;
        }
    }
}
