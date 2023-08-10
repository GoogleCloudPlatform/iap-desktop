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
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Net;
using System;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    internal class GeneralOptionsViewModel : OptionsViewModelBase<IApplicationSettings>
    {
        private readonly IBrowserProtocolRegistry protocolRegistry;

        public GeneralOptionsViewModel(
            IRepository<IApplicationSettings> settingsRepository,
            IBrowserProtocolRegistry protocolRegistry,
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

        protected override void Load(IApplicationSettings settings)
        {
            this.IsUpdateCheckEnabled.Value = settings.IsUpdateCheckEnabled.BoolValue;
            this.IsUpdateCheckEditable.Value = !settings.IsUpdateCheckEnabled.IsReadOnly;

            this.LastUpdateCheck = settings.LastUpdateCheck.IsDefault
                ? "never"
                : DateTime.FromBinary(settings.LastUpdateCheck.LongValue).ToString();

            this.IsBrowserIntegrationEnabled.Value = this.protocolRegistry.IsRegistered(
                IapRdpUrl.Scheme,
                ExecutableLocation);
        }

        protected override void Save(IApplicationSettings settings)
        {
            settings.IsUpdateCheckEnabled.BoolValue =
                this.IsUpdateCheckEnabled.Value;

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

        public ObservableProperty<bool> IsDeviceCertificateAuthenticationEditable { get; }//TODO: Remove

        public ObservableProperty<bool> IsUpdateCheckEnabled { get; }

        public ObservableProperty<bool> IsBrowserIntegrationEnabled { get; }

        public ObservableProperty<bool> IsDeviceCertificateAuthenticationEnabled { get; }//TODO: Remove

        public string LastUpdateCheck { get; private set; }
    }
}
