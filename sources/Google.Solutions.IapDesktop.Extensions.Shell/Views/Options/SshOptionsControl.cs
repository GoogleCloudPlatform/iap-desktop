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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Ssh.Auth;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    [SkipCodeCoverage("UI code")]
    public partial class SshOptionsControl : UserControl
    {
        private readonly SshOptionsViewModel viewModel;

        public SshOptionsControl(SshOptionsViewModel viewModel)
        {
            this.viewModel = viewModel;

            InitializeComponent();

            //
            // Authentication box.
            //
            this.publicKeyType.Items.AddRange(
                this.viewModel
                    .AllPublicKeyTypes
                    .Cast<object>()
                    .ToArray());
            this.publicKeyType.BindProperty(
                c => c.SelectedItem,
                this.viewModel,
                m => m.PublicKeyType,
                this.Container);
            this.publicKeyValidityUpDown.BindProperty(
                c => c.Value,
                this.viewModel,
                m => m.PublicKeyValidityInDays,
                this.Container);


            this.publicKeyType.FormattingEnabled = true;
            this.publicKeyType.Format += delegate (object sender, ListControlConvertEventArgs e)
            {
                var v = ((SshKeyType)e.Value);
                e.Value = v.GetAttribute<DisplayAttribute>()?.Name ?? v.ToString();
            };

            //
            // Connection box.
            //
            this.propagateLocaleCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsPropagateLocaleEnabled,
                this.Container);
        }
    }
}
