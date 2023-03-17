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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    internal class GeneralOptionsViewModel : OptionsViewModelBase<ApplicationSettings>
    {
        private readonly IAppProtocolRegistry protocolRegistry;

        public GeneralOptionsViewModel(
            ApplicationSettingsRepository settingsRepository,
            IAppProtocolRegistry protocolRegistry,
            HelpAdapter helpService)
            : base("General", settingsRepository)
        {
            this.protocolRegistry = protocolRegistry;

            this.OpenSecureConnectHelp = ObservableCommand.Build(
                string.Empty,
                () => helpService.OpenTopic(HelpTopics.SecureConnectDcaOverview));
            this.OpenBrowserIntegrationHelp = ObservableCommand.Build(
                string.Empty,
                () => helpService.OpenTopic(HelpTopics.BrowserIntegration));

            this.IsUpdateCheckEditable = ObservableProperty.Build(false);
            this.IsDeviceCertificateAuthenticationEditable = ObservableProperty.Build(false);

            this.IsUpdateCheckEnabled = ObservableProperty.Build(false);
            this.IsBrowserIntegrationEnabled = ObservableProperty.Build(false);
            this.IsDeviceCertificateAuthenticationEnabled = ObservableProperty.Build(false);

            MarkDirtyWhenPropertyChanges(this.IsUpdateCheckEnabled);
            MarkDirtyWhenPropertyChanges(this.IsBrowserIntegrationEnabled);
            MarkDirtyWhenPropertyChanges(this.IsDeviceCertificateAuthenticationEnabled);

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Load(ApplicationSettings settings)
        {
            this.IsUpdateCheckEnabled.Value = settings.IsUpdateCheckEnabled.BoolValue;
            this.IsUpdateCheckEditable.Value = !settings.IsUpdateCheckEnabled.IsReadOnly;

            this.IsDeviceCertificateAuthenticationEnabled.Value =
                settings.IsDeviceCertificateAuthenticationEnabled.BoolValue;
            this.IsDeviceCertificateAuthenticationEditable.Value =
                !settings.IsDeviceCertificateAuthenticationEnabled.IsReadOnly;

            this.LastUpdateCheck = settings.LastUpdateCheck.IsDefault
                ? "never"
                : DateTime.FromBinary(settings.LastUpdateCheck.LongValue).ToString();

            this.IsBrowserIntegrationEnabled.Value = this.protocolRegistry.IsRegistered(
                IapRdpUrl.Scheme,
                ExecutableLocation);
        }

        protected override void Save(ApplicationSettings settings)
        {
            settings.IsUpdateCheckEnabled.BoolValue =
                this.IsUpdateCheckEnabled.Value;
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue =
                this.IsDeviceCertificateAuthenticationEnabled.Value;

            //
            // Update protocol registration.
            //
            if (this.IsBrowserIntegrationEnabled.Value)
            {
                this.protocolRegistry.Register(
                    IapRdpUrl.Scheme,
                    Install.FriendlyName,
                    ExecutableLocation);
            }
            else
            {
                this.protocolRegistry.Unregister(IapRdpUrl.Scheme);
            }
        }


        // NB. GetEntryAssembly returns the .exe, but this does not work during tests.
        private static string ExecutableLocation =>
            (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;

        //---------------------------------------------------------------------
        // Observable command.
        //---------------------------------------------------------------------

        public ObservableCommand OpenSecureConnectHelp { get; }
        public ObservableCommand OpenBrowserIntegrationHelp { get; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<bool> IsUpdateCheckEditable { get; }

        public ObservableProperty<bool> IsDeviceCertificateAuthenticationEditable { get; }

        public ObservableProperty<bool> IsUpdateCheckEnabled { get; }

        public ObservableProperty<bool> IsBrowserIntegrationEnabled { get; }

        public ObservableProperty<bool> IsDeviceCertificateAuthenticationEnabled { get; }

        public string LastUpdateCheck { get; private set; }
    }
}
