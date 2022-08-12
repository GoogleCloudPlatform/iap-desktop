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
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public class GeneralOptionsViewModel : ViewModelBase, IPropertiesSheetViewModel
    {
        private readonly ApplicationSettingsRepository settingsRepository;
        private readonly IAppProtocolRegistry protocolRegistry;
        private readonly HelpService helpService;

        private bool isUpdateCheckEnabled;
        private readonly string lastUpdateCheck;

        private bool isBrowserIntegrationEnabled;
        private bool isDcaEnabled;
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
            this.IsUpdateCheckEditable = !settings.IsUpdateCheckEnabled.IsReadOnly;

            this.isDcaEnabled = settings.IsDeviceCertificateAuthenticationEnabled.BoolValue;
            this.IsDeviceCertificateAuthenticationEditable =
                !settings.IsDeviceCertificateAuthenticationEnabled.IsReadOnly;

            this.lastUpdateCheck = settings.LastUpdateCheck.IsDefault
                ? "never"
                : DateTime.FromBinary(settings.LastUpdateCheck.LongValue).ToString();

            this.isBrowserIntegrationEnabled = this.protocolRegistry.IsRegistered(
                IapRdpUrl.Scheme,
                ExecutableLocation);
        }

        // NB. GetEntryAssembly returns the .exe, but this does not work during tests.
        private static string ExecutableLocation =>
            (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;

        //---------------------------------------------------------------------
        // IPropertiesSheetViewModel.
        //---------------------------------------------------------------------

        public string Title => "General";

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public DialogResult ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            //
            // Save changed settings.
            //
            var settings = this.settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = this.isUpdateCheckEnabled;
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = this.isDcaEnabled;
            this.settingsRepository.SetSettings(settings);

            // Update protocol registration.
            if (this.isBrowserIntegrationEnabled)
            {
                this.protocolRegistry.Register(
                    IapRdpUrl.Scheme,
                    Globals.FriendlyName,
                    ExecutableLocation);
            }
            else
            {
                this.protocolRegistry.Unregister(IapRdpUrl.Scheme);
            }

            this.IsDirty = false;

            return DialogResult.OK;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsUpdateCheckEditable { get; }
        public bool IsDeviceCertificateAuthenticationEditable { get; }

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

        public bool IsDeviceCertificateAuthenticationEnabled
        {
            get => this.isDcaEnabled;
            set
            {
                this.isDcaEnabled = value;
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

        public void OpenSecureConnectDcaOverviewDocs()
            => this.helpService.OpenTopic(HelpTopics.SecureConnectDcaOverview);
    }
}
