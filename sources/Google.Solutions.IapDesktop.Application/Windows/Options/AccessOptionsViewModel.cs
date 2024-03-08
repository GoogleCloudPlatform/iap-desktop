//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Client;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Settings;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    public class AccessOptionsViewModel : OptionsViewModelBase<IAccessSettings>
    {
        internal bool ProbePrivateServiceConnectEndpoint { get; set; } = true;

        public AccessOptionsViewModel(
            IRepository<IAccessSettings> settingsRepository,
            HelpClient helpService)
            : base("Access", settingsRepository)
        {

            this.OpenCertificateAuthenticationHelp = ObservableCommand.Build(
                string.Empty,
                () => helpService.OpenTopic(HelpTopics.CertificateBasedAccessOverview));
            this.OpenPrivateServiceConnectHelp = ObservableCommand.Build(
                string.Empty,
                () => helpService.OpenTopic(HelpTopics.PrivateServiceConnectOverview));

            this.IsDeviceCertificateAuthenticationEditable = ObservableProperty.Build(false);
            this.IsDeviceCertificateAuthenticationEnabled = ObservableProperty.Build(false);

            this.PrivateServiceConnectEndpoint = ObservableProperty.Build<string>(null);
            this.IsPrivateServiceConnectEnabled = ObservableProperty.Build(false);
            this.IsPrivateServiceConnectEditable = ObservableProperty.Build(false);

            this.ConnectionPoolLimit = ObservableProperty.Build<decimal>(0m);

            MarkDirtyWhenPropertyChanges(this.IsDeviceCertificateAuthenticationEnabled);
            MarkDirtyWhenPropertyChanges(this.IsPrivateServiceConnectEnabled);
            MarkDirtyWhenPropertyChanges(this.PrivateServiceConnectEndpoint);
            MarkDirtyWhenPropertyChanges(this.ConnectionPoolLimit);

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Load(IAccessSettings settings)
        {
            this.IsDeviceCertificateAuthenticationEnabled.Value =
                settings.IsDeviceCertificateAuthenticationEnabled.BoolValue;
            this.IsDeviceCertificateAuthenticationEditable.Value =
                !settings.IsDeviceCertificateAuthenticationEnabled.IsReadOnly;

            this.PrivateServiceConnectEndpoint.Value =
                settings.PrivateServiceConnectEndpoint.Value;
            this.IsPrivateServiceConnectEnabled.Value =
                !settings.PrivateServiceConnectEndpoint.IsDefault; 
            this.IsPrivateServiceConnectEditable.Value =
                !settings.PrivateServiceConnectEndpoint.IsReadOnly;

            this.ConnectionPoolLimit.Value = (decimal)
                settings.ConnectionLimit.IntValue;
        }

        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "")]
        protected override void Save(IAccessSettings settings)
        {
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue =
                this.IsDeviceCertificateAuthenticationEnabled.Value;

            settings.PrivateServiceConnectEndpoint.Value =
                this.IsPrivateServiceConnectEnabled.Value
                    ? this.PrivateServiceConnectEndpoint.Value
                    : null;

            settings.ConnectionLimit.IntValue =
                (int)this.ConnectionPoolLimit.Value;

            if (this.ProbePrivateServiceConnectEndpoint &&
                settings.PrivateServiceConnectEndpoint.Value 
                is var pscEndpoint &&
                pscEndpoint != null)
            {
                //
                // Probe the endpoint using a short timeout that doesn't
                // substantially impact UI resposiveness.
                //
                try
                {
                    new ServiceRoute(pscEndpoint)
                        .ProbeAsync(TimeSpan.FromSeconds(2))
                        .Wait();
                }
                catch (Exception e)
                {
                    throw new InvalidOptionsException(
                        $"The endpoint '{pscEndpoint}' is invalid or " +
                        $"can't be reached from your computer",
                        e,
                        HelpTopics.PrivateServiceConnectOverview);
                }
            }

            if (settings.PrivateServiceConnectEndpoint.Value != null &&
                settings.IsDeviceCertificateAuthenticationEnabled.BoolValue)
            {
                throw new InvalidOptionsException(
                    "To use certificate-based access, you must disable " +
                    "Private Service Connect",
                    HelpTopics.CertificateBasedAccessOverview);
            }
        }

        //---------------------------------------------------------------------
        // Observable command.
        //---------------------------------------------------------------------

        public ObservableCommand OpenCertificateAuthenticationHelp { get; }
        public ObservableCommand OpenPrivateServiceConnectHelp { get; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<bool> IsDeviceCertificateAuthenticationEnabled { get; }
        public ObservableProperty<bool> IsDeviceCertificateAuthenticationEditable { get; }

        public ObservableProperty<bool> IsPrivateServiceConnectEnabled { get; }
        public ObservableProperty<string> PrivateServiceConnectEndpoint { get; }
        public ObservableProperty<bool> IsPrivateServiceConnectEditable{ get; }

        public ObservableProperty<decimal> ConnectionPoolLimit { get; }
    }
}
