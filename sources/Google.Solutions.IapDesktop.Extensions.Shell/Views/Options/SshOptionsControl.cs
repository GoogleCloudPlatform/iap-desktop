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
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.Ssh.Auth;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    [SkipCodeCoverage("UI code")]
    [Service(typeof(ISshOptionsSheet), ServiceLifetime.Transient, ServiceVisibility.Global)]
    [ServiceCategory(typeof(IPropertiesSheet))]
    public partial class SshOptionsControl : UserControl, ISshOptionsSheet
    {
        private readonly SshOptionsViewModel viewModel;

        public SshOptionsControl(
            SshSettingsRepository settingsRepository)
        {
            this.viewModel = new SshOptionsViewModel(settingsRepository);

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
                c => c.SelectedIndex,
                this.viewModel,
                m => m.PublicKeyTypeIndex,
                this.Container);
            this.publicKeyType.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsPublicKeyTypeEditable,
                this.Container);

            this.publicKeyValidityUpDown.BindProperty(
                c => c.Value,
                this.viewModel,
                m => m.PublicKeyValidityInDays,
                this.Container);
            this.publicKeyValidityUpDown.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsPublicKeyValidityInDaysEditable,
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

        public SshOptionsControl(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<SshSettingsRepository>())
        {
        }

        //---------------------------------------------------------------------
        // IPropertiesSheet.
        //---------------------------------------------------------------------

        public IPropertiesSheetViewModel ViewModel => this.viewModel;
    }
}
