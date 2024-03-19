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

using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using System;
using System.Diagnostics;

#pragma warning disable CA1822 // Mark members as static
#nullable disable

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    internal class NetworkOptionsViewModel : OptionsViewModelBase<IApplicationSettings>
    {
        private readonly IHttpProxyAdapter proxyAdapter;

        private string proxyPacAddress = null;
        private string proxyServer = null;
        private string proxyPort = null;
        private string proxyUsername = null;
        private string proxyPassword = null;

        public NetworkOptionsViewModel(
            IRepository<IApplicationSettings> settingsRepository,
            IHttpProxyAdapter proxyAdapter)
            : base("Network", settingsRepository)
        {
            this.proxyAdapter = proxyAdapter;

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Load(IApplicationSettings settings)
        {
            this.IsProxyEditable =
                !settings.ProxyUrl.IsReadOnly &&
                !settings.ProxyPacUrl.IsReadOnly;

            if (!string.IsNullOrEmpty(settings.ProxyUrl.Value) &&
                Uri.TryCreate(settings.ProxyUrl.Value, UriKind.Absolute, out var proxyUrl))
            {
                this.proxyServer = proxyUrl.Host;
                this.proxyPort = proxyUrl.Port.ToString();
            }

            if (!string.IsNullOrEmpty(settings.ProxyPacUrl.Value) &&
                IsValidProxyAutoConfigurationAddress(settings.ProxyPacUrl.Value))
            {
                this.proxyPacAddress = settings.ProxyPacUrl.Value;
            }

            if (this.proxyServer != null || this.proxyPacAddress != null)
            {
                this.proxyUsername = settings.ProxyUsername.Value;
                this.proxyPassword = settings.ProxyPassword.GetClearTextValue();
            }
        }

        protected override void Save(IApplicationSettings settings)
        {
            Debug.Assert(this.IsDirty.Value);

            switch (this.Proxy)
            {
                case ProxyType.Custom:
                    if (!IsValidProxyHost(this.proxyServer?.Trim()))
                    {
                        throw new ArgumentException(
                            $"'{this.proxyServer}' is not a valid host name");
                    }

                    if (!IsValidProxyPort(this.proxyPort?.Trim()))
                    {
                        throw new ArgumentException(
                            $"'{this.proxyPort}' is not a valid port number");
                    }

                    settings.ProxyUrl.Value = $"http://{this.proxyServer?.Trim()}:{this.proxyPort?.Trim()}";
                    settings.ProxyPacUrl.Reset();
                    break;

                case ProxyType.Autoconfig:

                    if (!IsValidProxyAutoConfigurationAddress(this.proxyPacAddress?.Trim()))
                    {
                        throw new ArgumentException(
                            $"'{this.proxyPacAddress?.Trim()}' is not a valid proxy autoconfiguration URL");
                    }

                    settings.ProxyUrl.Reset();
                    settings.ProxyPacUrl.Value = this.proxyPacAddress?.Trim();
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

            settings.ProxyUsername.Value = this.proxyUsername;

            if (this.proxyPassword != null)
            {
                settings.ProxyPassword.SetClearTextValue(this.proxyPassword);
            }
            else
            {
                settings.ProxyPassword.Value = null;
            }

            //
            // Activate changed proxy settings so that the app
            // does not need to be restarted.
            //

            this.proxyAdapter.ActivateSettings(settings);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsProxyEditable { get; private set; }

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
                this.IsDirty.Value = true;
                RaisePropertyChange();
            }
        }

        public string ProxyServer
        {
            get => this.proxyServer;
            set
            {
                this.proxyServer = value;
                this.IsDirty.Value = true;
                RaisePropertyChange();
            }
        }

        public string ProxyPort
        {
            get => this.proxyPort;
            set
            {
                this.proxyPort = value;
                this.IsDirty.Value = true;
                RaisePropertyChange();
            }
        }

        public bool IsProxyAuthenticationEnabled
        {
            get => !this.IsSystemProxyServerEnabled && !string.IsNullOrEmpty(this.proxyUsername);
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
                this.IsDirty.Value = true;
                RaisePropertyChange();
            }
        }

        public string ProxyPassword
        {
            get => this.proxyPassword;
            set
            {
                this.proxyPassword = value;
                this.IsDirty.Value = true;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public bool IsValidProxyPort(string port)
            => int.TryParse(port, out var portNumber) &&
                portNumber > 0 &&
                portNumber <= ushort.MaxValue;

        public bool IsValidProxyHost(string host)
            => Uri.TryCreate($"http://{host}", UriKind.Absolute, out var _);

        public bool IsValidProxyAutoConfigurationAddress(string pacAddress)
            => Uri.TryCreate(pacAddress, UriKind.Absolute, out var uri) &&
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
