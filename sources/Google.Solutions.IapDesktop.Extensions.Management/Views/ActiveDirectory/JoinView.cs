//
// Copyright 2022 Google LLC
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
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.ActiveDirectory
{
    [Service]
    public partial class JoinView : Form, IView<JoinViewModel>
    {
        public JoinView()
        {
            InitializeComponent();
        }

        public void Bind(JoinViewModel viewModel)
        { 
            this.domainText.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.DomainName,
                this.Container);
            this.domainWarning.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsDomainNameInvalid,
                this.Container);

            this.computerNameText.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.ComputerName,
                this.Container);
            this.computerNameWarning.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsComputerNameInvalid,
                this.Container);

            this.okButton.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsOkButtonEnabled,
                this.Container);
        }
    }
}
