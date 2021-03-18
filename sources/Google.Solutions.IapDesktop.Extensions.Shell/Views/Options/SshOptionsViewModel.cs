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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    public interface ISshDialogPane : IOptionsDialogPane
    { }

    [Service(typeof(ISshDialogPane), ServiceLifetime.Transient, ServiceVisibility.Global)]
    [ServiceCategory(typeof(IOptionsDialogPane))]
    public class SshOptionsViewModel : ViewModelBase, ISshDialogPane
    { 
        private bool isPropagateLocaleEnabled;

        private readonly SshSettingsRepository settingsRepository;

        private bool isDirty;

        public SshOptionsViewModel(
            SshSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because other tabs
            // might apply changes to other application settings.
            //
            var settings = this.settingsRepository.GetSettings();

            this.IsPropagateLocaleEnabled =
                settings.IsPropagateLocaleEnabled.BoolValue;

            this.isDirty = false;
        }


        public SshOptionsViewModel(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<SshSettingsRepository>())
        {
        }

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "SSH";

        public UserControl CreateControl() => new SshOptionsControl(this);

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public void ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            //
            // Save settings.
            //
            var settings = this.settingsRepository.GetSettings();

            settings.IsPropagateLocaleEnabled.BoolValue =
                this.IsPropagateLocaleEnabled;

            this.settingsRepository.SetSettings(settings);

            this.IsDirty = false;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsPropagateLocaleEnabled
        {
            get => this.isPropagateLocaleEnabled;
            set
            {
                this.IsDirty = true;
                this.isPropagateLocaleEnabled = value;
                RaisePropertyChange();
            }
        }
    }
}
