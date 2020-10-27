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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public class NetworkOptionsViewModel : ViewModelBase, IOptionsDialogPane
    {
        private readonly IHttpProxyAdapter proxyAdapter;
        private readonly ApplicationSettingsRepository settingsRepository;

        private string proxyServer = null;
        private string proxyPort = null;
        private bool isDirty = false;

        public NetworkOptionsViewModel(
            ApplicationSettingsRepository settingsRepository,
            IHttpProxyAdapter proxyAdapter)
        {
            this.settingsRepository = settingsRepository;
            this.proxyAdapter = proxyAdapter;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because other tabs
            // might apply changes to other application settings.
            //

            var settings = this.settingsRepository.GetSettings();
            if (!string.IsNullOrEmpty(settings.ProxyUrl.StringValue) &&
                Uri.TryCreate(settings.ProxyUrl.StringValue, UriKind.Absolute, out Uri proxyUrl))
            {
                this.proxyServer = proxyUrl.Host;
                this.proxyPort = proxyUrl.Port.ToString();
            }
        }

        public NetworkOptionsViewModel(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<ApplicationSettingsRepository>(),
                  serviceProvider.GetService<IHttpProxyAdapter>())
        { 
        }

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "Network";

        public UserControl CreateControl() => new NetworkOptionsControl(this);

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

            if (this.proxyServer != null && !IsValidProxyHost(this.proxyServer))
            {
                throw new ArgumentException($"'{this.proxyServer}' is not a valid host name");
            }
            if (this.proxyPort != null && !IsValidProxyPort(this.proxyPort))
            {
                throw new ArgumentException($"'{this.proxyPort}' is not a valid port number");
            }

            //
            // Save changed settings.
            //

            var settings = this.settingsRepository.GetSettings();
            settings.ProxyUrl.StringValue = this.proxyServer != null
                ? $"http://{this.proxyServer}:{this.proxyPort}"
                : null;
            this.settingsRepository.SetSettings(settings);

            //
            // Activate changed proxy settings so that the app
            // does not need to be restarted.
            //

            this.proxyAdapter.ActivateSettings(settings);

            this.IsDirty = false;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsCustomProxyServerEnabled
        {
            get => this.proxyServer != null;
            set
            {
                if (value)
                {
                    // Initialize to a sane default.
                    this.ProxyServer = this.ProxyServer ?? "proxy";
                    this.ProxyPort = this.ProxyPort ?? "3128";
                }
                else
                {
                    this.ProxyServer = null;
                    this.ProxyPort = null;
                }

                RaisePropertyChange();
                RaisePropertyChange((NetworkOptionsViewModel m) => m.IsSystemProxyServerEnabled);
            }
        }

        public bool IsSystemProxyServerEnabled
        {
            get => !IsCustomProxyServerEnabled;
            set => IsCustomProxyServerEnabled = !value;
        }

        public string ProxyServer
        {
            get => this.proxyServer;
            set
            {
                this.proxyServer = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        public string ProxyPort
        {
            get => this.proxyPort;
            set
            {
                this.proxyPort = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public bool IsValidProxyPort(string port)
            => int.TryParse(port, out int portNumber) &&
                portNumber > 0 &&
                portNumber <= ushort.MaxValue;

        public bool IsValidProxyHost(string host)
            => Uri.TryCreate($"http://{host}", UriKind.Absolute, out Uri _);

        public void OpenProxyControlPanelApplet()
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = false,
                Verb = "open",
                FileName = "rundll32.exe",
                Arguments = "shell32.dll,Control_RunDLL inetcpl.cpl,,4"
            }))
            { };
        }
    }
}
