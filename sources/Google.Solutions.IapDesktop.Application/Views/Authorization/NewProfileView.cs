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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    [SkipCodeCoverage("View code")]
    public partial class NewProfileView : Form, IView<NewProfileViewModel>
    {
        public NewProfileView()
        {
            InitializeComponent();
        }

        public void Bind(NewProfileViewModel viewModel)
        {
            this.profileNameTextBox.BindProperty(
                c => c.Text,
                viewModel,
                m => m.ProfileName,
                this.components);
            this.profileNameInvalidLabel.BindReadonlyProperty(
                c => c.Visible,
                viewModel,
                m => m.IsProfileNameInvalid,
                this.components);

            // Bind buttons.
            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsOkButtonEnabled,
                this.components);
        }
    }
}
