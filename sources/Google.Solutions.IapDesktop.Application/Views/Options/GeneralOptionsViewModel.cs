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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public class GeneralOptionsViewModel : ViewModelBase, IOptionsDialogPane
    {
        public const string FriendlyName = "IAP Desktop - Identity-Aware Proxy for Remote Desktop";

        private readonly ApplicationSettingsRepository settingsRepository;
        private readonly IAppProtocolRegistry protocolRegistry;
        private readonly HelpService helpService;

        private bool isUpdateCheckEnabled;
        private string lastUpdateCheck;

        private bool isBrowserIntegrationEnabled;
        private bool isDirty = false;

        public GeneralOptionsViewModel(
            ApplicationSettingsRepository settingsRepository,
            IAppProtocolRegistry protocolRegistry,
            HelpService helpService)
        {
            this.settingsRepository = settingsRepository;
            this.protocolRegistry = protocolRegistry;
            this.helpService = helpService;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because other tabs
            // might apply changes to other application settings.
            //

            var settings = this.settingsRepository.GetSettings();
            this.isUpdateCheckEnabled = settings.IsUpdateCheckEnabled.BoolValue;
            this.lastUpdateCheck = settings.LastUpdateCheck.IsDefault
                ? "never"
                : DateTime.FromBinary(settings.LastUpdateCheck.LongValue).ToString();

            this.isBrowserIntegrationEnabled = this.protocolRegistry.IsRegistered(
                IapRdpUrl.Scheme,
                ExecutableLocation);
        }


        public GeneralOptionsViewModel(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<ApplicationSettingsRepository>(),
                  serviceProvider.GetService<IAppProtocolRegistry>(),
                  serviceProvider.GetService<HelpService>())
        {
        }

        // NB. GetEntryAssembly returns the .exe, but this does not work during tests.
        private static string ExecutableLocation =>
            (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "General";

        public UserControl CreateControl() => new GeneralOptionsControl(this);

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
            // Save changed settings.
            //

            var settings = this.settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = this.isUpdateCheckEnabled;
            this.settingsRepository.SetSettings(settings);

            // Update protocol registration.
            if (this.isBrowserIntegrationEnabled)
            {
                this.protocolRegistry.Register(
                    IapRdpUrl.Scheme,
                    FriendlyName,
                    ExecutableLocation);
            }
            else
            {
                this.protocolRegistry.Unregister(IapRdpUrl.Scheme);
            }

            this.IsDirty = false;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsUpdateCheckEnabled
        {
            get => this.isUpdateCheckEnabled;
            set
            {
                this.isUpdateCheckEnabled = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        public bool IsBrowserIntegrationEnabled
        {
            get => this.isBrowserIntegrationEnabled;
            set
            {
                this.isBrowserIntegrationEnabled = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        public string LastUpdateCheck => this.lastUpdateCheck;

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void OpenBrowserIntegrationDocs()
            => this.helpService.OpenTopic(HelpTopics.BrowserIntegration);
    }
}
