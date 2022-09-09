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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Windows.Forms;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public class NetworkOptionsViewModel : ViewModelBase, IPropertiesSheetViewModel
    {
        private readonly IHttpProxyAdapter proxyAdapter;
        private readonly ApplicationSettingsRepository settingsRepository;

        private string proxyPacAddress = null;
        private string proxyServer = null;
        private string proxyPort = null;
        private string proxyUsername = null;
        private string proxyPassword = null;
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

            this.IsProxyEditable =
                !settings.ProxyUrl.IsReadOnly &&
                !settings.ProxyPacUrl.IsReadOnly;

            if (!string.IsNullOrEmpty(settings.ProxyUrl.StringValue) &&
                Uri.TryCreate(settings.ProxyUrl.StringValue, UriKind.Absolute, out Uri proxyUrl))
            {
                this.proxyServer = proxyUrl.Host;
                this.proxyPort = proxyUrl.Port.ToString();
            }

            if (!string.IsNullOrEmpty(settings.ProxyPacUrl.StringValue) &&
                IsValidProxyAutoConfigurationAddress(settings.ProxyPacUrl.StringValue))
            {
                this.proxyPacAddress = settings.ProxyPacUrl.StringValue;
            }

            if (this.proxyServer != null || this.proxyPacAddress != null)
            {
                this.proxyUsername = settings.ProxyUsername.StringValue;
                this.proxyPassword = settings.ProxyPassword.ClearTextValue;
            }
        }

        //---------------------------------------------------------------------
        // IPropertiesSheetViewModel.
        //---------------------------------------------------------------------

        public string Title => "Network";

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
            // Validate ans save settings.
            //
            var settings = this.settingsRepository.GetSettings();

            switch (this.Proxy)
            {
                case ProxyType.Custom:
                    if (!IsValidProxyHost(this.proxyServer))
                    {
                        throw new ArgumentException(
                            $"'{this.proxyServer}' is not a valid host name");
                    }

                    if (!IsValidProxyPort(this.proxyPort))
                    {
                        throw new ArgumentException(
                            $"'{this.proxyPort}' is not a valid port number");
                    }

                    settings.ProxyUrl.StringValue = $"http://{this.proxyServer}:{this.proxyPort}";
                    settings.ProxyPacUrl.Reset();
                    break;

                case ProxyType.Autoconfig:

                    if (!IsValidProxyAutoConfigurationAddress(this.proxyPacAddress))
                    {
                        throw new ArgumentException(
                            $"'{this.proxyPacAddress}' is not a valid proxy autoconfiguration URL");
                    }

                    settings.ProxyUrl.Reset();
                    settings.ProxyPacUrl.StringValue = this.proxyPacAddress;
                    break;

                case ProxyType.System:
                    settings.ProxyUrl.Reset();
                    settings.ProxyPacUrl.Reset();
                    break;
            }

            if (string.IsNullOrEmpty(this.proxyUsername) != string.IsNullOrEmpty(this.proxyPassword))
            {
                throw new ArgumentException("Proxy credentials are incomplete");
            }

            settings.ProxyUsername.StringValue = this.proxyUsername;

            if (this.proxyPassword != null)
            {
                settings.ProxyPassword.ClearTextValue = this.proxyPassword;
            }
            else
            {
                settings.ProxyPassword.Value = null;
            }

            this.settingsRepository.SetSettings(settings);

            //
            // Activate changed proxy settings so that the app
            // does not need to be restarted.
            //

            this.proxyAdapter.ActivateSettings(settings);

            this.IsDirty = false;

            return DialogResult.OK;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsProxyEditable { get; }

        public enum ProxyType
        {
            System,
            Custom,
            Autoconfig
        }

        public ProxyType Proxy
        {
            set
            {
                switch (value)
                {
                    case ProxyType.System:
                        // Reset everything.
                        this.ProxyAutoconfigurationAddress = null;
                        this.ProxyServer = null;
                        this.ProxyPort = null;
                        this.ProxyUsername = null;
                        this.ProxyPassword = null;
                        break;

                    case ProxyType.Custom:
                        // Initialize to a sane default.
                        this.ProxyAutoconfigurationAddress = null;
                        this.ProxyServer = this.ProxyServer ?? "proxy";
                        this.ProxyPort = this.ProxyPort ?? "3128";
                        break;

                    case ProxyType.Autoconfig:
                        // Initialize to a sane default.
                        this.ProxyAutoconfigurationAddress = "http://proxy/proxy.pac";
                        this.ProxyServer = null;
                        this.ProxyPort = null;
                        break;
                }

                RaisePropertyChange((NetworkOptionsViewModel m) => m.IsSystemProxyServerEnabled);
                RaisePropertyChange((NetworkOptionsViewModel m) => m.IsCustomProxyServerEnabled);
                RaisePropertyChange((NetworkOptionsViewModel m) => m.IsProxyAutoConfigurationEnabled);
                RaisePropertyChange((NetworkOptionsViewModel m) => m.IsCustomProxyServerOrProxyAutoConfigurationEnabled);
                RaisePropertyChange((NetworkOptionsViewModel m) => m.IsProxyAuthenticationEnabled);
            }
            get
            {
                if (this.proxyPacAddress != null)
                {
                    return ProxyType.Autoconfig;
                }
                else if (this.proxyServer != null)
                {
                    return ProxyType.Custom;
                }
                else
                {
                    return ProxyType.System;
                }
            }
        }

        public bool IsCustomProxyServerEnabled
        {
            get => this.Proxy == ProxyType.Custom;
            set
            {
                if (value)
                {
                    this.Proxy = ProxyType.Custom;
                }
            }
        }

        public bool IsProxyAutoConfigurationEnabled
        {
            get => this.Proxy == ProxyType.Autoconfig;
            set
            {
                if (value)
                {
                    this.Proxy = ProxyType.Autoconfig;
                }
            }
        }

        public bool IsSystemProxyServerEnabled
        {
            get => this.Proxy == ProxyType.System;
            set
            {
                if (value)
                {
                    this.Proxy = ProxyType.System;
                }
            }
        }

        public bool IsCustomProxyServerOrProxyAutoConfigurationEnabled
            => this.IsCustomProxyServerEnabled ||
               this.IsProxyAutoConfigurationEnabled;

        public string ProxyAutoconfigurationAddress
        {
            get => this.proxyPacAddress;
            set
            {
                this.proxyPacAddress = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
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

        public bool IsProxyAuthenticationEnabled
        {
            get => !IsSystemProxyServerEnabled && !string.IsNullOrEmpty(this.proxyUsername);
            set
            {
                if (value)
                {
                    // Initialize to a sane default.
                    this.ProxyUsername = Environment.UserName;
                }
                else
                {
                    this.ProxyUsername = null;
                    this.ProxyPassword = null;
                }

                RaisePropertyChange();
            }
        }

        public string ProxyUsername
        {
            get => this.proxyUsername;
            set
            {
                this.proxyUsername = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        public string ProxyPassword
        {
            get => this.proxyPassword;
            set
            {
                this.proxyPassword = value;
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

        public bool IsValidProxyAutoConfigurationAddress(string pacAddress)
            => Uri.TryCreate(pacAddress, UriKind.Absolute, out Uri uri) &&
               (uri.Scheme == "http" || uri.Scheme == "https");

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
