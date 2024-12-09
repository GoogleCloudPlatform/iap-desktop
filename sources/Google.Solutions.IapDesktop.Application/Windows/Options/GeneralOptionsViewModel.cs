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

using Google.Solutions.Common;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Net;
using Google.Solutions.Settings.Collection;
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
            HelpClient helpService)
            : base("General", settingsRepository)
        {
            var settings = settingsRepository.GetSettings();

            this.protocolRegistry = protocolRegistry;

            this.OpenBrowserIntegrationHelp = ObservableCommand.Build(
                string.Empty,
                () => helpService.OpenTopic(HelpTopics.BrowserIntegration));
            this.OpenTelemetryHelp = ObservableCommand.Build(
                string.Empty,
                () => helpService.OpenTopic(HelpTopics.Privacy));

            this.IsUpdateCheckEditable = ObservableProperty.Build(
                !settings.IsUpdateCheckEnabled.IsReadOnly);
            this.IsUpdateCheckEnabled = ObservableProperty.Build(
                settings.IsUpdateCheckEnabled.Value);
            this.IsBrowserIntegrationEnabled = ObservableProperty.Build(
                this.protocolRegistry.IsRegistered(
                    IapRdpUrl.Scheme,
                    ExecutableLocation));
            this.IsTelemetryEditable = ObservableProperty.Build(
                !settings.IsTelemetryEnabled.IsReadOnly);
            this.IsTelemetryEnabled = ObservableProperty.Build(
                settings.IsTelemetryEnabled.Value);

            this.LastUpdateCheck = settings.LastUpdateCheck.IsDefault
                ? "never"
                : DateTime
                    .FromBinary(settings.LastUpdateCheck.Value)
                    .ToLocalTime()
                    .ToString();

            MarkDirtyWhenPropertyChanges(this.IsUpdateCheckEnabled);
            MarkDirtyWhenPropertyChanges(this.IsBrowserIntegrationEnabled);
            MarkDirtyWhenPropertyChanges(this.IsTelemetryEnabled);

            base.OnInitializationCompleted();
        }

        protected override void Save(IApplicationSettings settings)
        {
            settings.IsUpdateCheckEnabled.Value = this.IsUpdateCheckEnabled.Value;
            settings.IsTelemetryEnabled.Value = this.IsTelemetryEnabled.Value;

            //
            // Update protocol registration.
            //
            if (this.IsBrowserIntegrationEnabled.Value)
            {
                this.protocolRegistry.Register(
                    IapRdpUrl.Scheme,
                    Install.ProductName,
                    ExecutableLocation);
            }
            else
            {
                this.protocolRegistry.Unregister(IapRdpUrl.Scheme);
            }

            //
            // Apply telemetry settings so that we don't have
            // to relaunch.
            //
            TelemetryLog.Current.Enabled = this.IsTelemetryEnabled.Value;
        }

        private static string ExecutableLocation
        {
            //
            // NB. GetEntryAssembly returns the .exe, but this does not work during tests.
            //
            get => (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;
        }

        //---------------------------------------------------------------------
        // Observable command.
        //---------------------------------------------------------------------

        public ObservableCommand OpenBrowserIntegrationHelp { get; }
        public ObservableCommand OpenTelemetryHelp { get; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<bool> IsUpdateCheckEditable { get; }

        public ObservableProperty<bool> IsUpdateCheckEnabled { get; }

        public ObservableProperty<bool> IsBrowserIntegrationEnabled { get; }

        public string LastUpdateCheck { get; private set; }

        public ObservableProperty<bool> IsTelemetryEditable { get; }

        public ObservableProperty<bool> IsTelemetryEnabled { get; }
    }
}
