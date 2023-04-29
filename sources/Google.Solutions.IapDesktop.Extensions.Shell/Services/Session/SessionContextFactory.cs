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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    public interface ISessionContextFactory
    {
        /// <summary>
        /// Create a new SSH session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<SshCredential>> CreateSshSessionAsync(IProjectModelInstanceNode node);

        /// <summary>
        /// Create a new RDP session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<RdpCredential>> CreateRdpSessionAsync(IProjectModelInstanceNode node);

        /// <summary>
        /// Create a new RDP session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<RdpCredential>> CreateRdpSessionAsync(IapRdpUrl url);
    }

    [Service(typeof(ISessionContextFactory))]
    public class SessionContextFactory : ISessionContextFactory
    {
        private readonly IWin32Window window;
        private readonly IAuthorization authorization;
        private readonly IKeyStoreAdapter keyStoreAdapter;
        private readonly IKeyAuthorizationService keyAuthorizationService;
        private readonly IConnectionSettingsService settingsService;
        private readonly SshSettingsRepository sshSettingsRepository;
        private readonly ITunnelBrokerService tunnelBrokerService;

        public SessionContextFactory(
            IWin32Window window,
            IAuthorization authorization,
            IKeyStoreAdapter keyStoreAdapter,
            IKeyAuthorizationService keyAuthService,
            IConnectionSettingsService settingsService,
            ITunnelBrokerService tunnelBrokerService,
            SshSettingsRepository sshSettingsRepository)
        {
            this.window = window.ExpectNotNull(nameof(window));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.keyStoreAdapter = keyStoreAdapter.ExpectNotNull(nameof(keyStoreAdapter));
            this.keyAuthorizationService = keyAuthService.ExpectNotNull(nameof(keyAuthService));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.tunnelBrokerService = tunnelBrokerService.ExpectNotNull(nameof(tunnelBrokerService));
            this.sshSettingsRepository = sshSettingsRepository.ExpectNotNull(nameof(sshSettingsRepository));
        }

        //---------------------------------------------------------------------
        // ISessionContextFactory.
        //---------------------------------------------------------------------

        public Task<ISessionContext<RdpCredential>> CreateRdpSessionAsync(IProjectModelInstanceNode node)
        {
            throw new System.NotImplementedException();
        }

        public Task<ISessionContext<RdpCredential>> CreateRdpSessionAsync(IapRdpUrl url)
        {
            throw new System.NotImplementedException();
        }

        public Task<ISessionContext<SshCredential>> CreateSshSessionAsync(IProjectModelInstanceNode node)
        {
            var settings = (InstanceConnectionSettings)this.settingsService
                .GetConnectionSettings(node)
                .TypedCollection;

            //
            // Load persistent CNG key. This might pop up dialogs.
            //
            var sshSettings = this.sshSettingsRepository.GetSettings();
            var localKeyPair = this.keyStoreAdapter.OpenSshKeyPair(
                sshSettings.PublicKeyType.EnumValue,
                this.authorization,
                true,
                this.window);
            Debug.Assert(localKeyPair != null);

            //
            // Initialize a context and pass ownership of the key to it.
            //
            var context = new SshSessionContext(
                this.tunnelBrokerService,
                this.keyAuthorizationService,
                node.Instance,
                localKeyPair)
            {
                ConnectionTimeout = TimeSpan.FromSeconds(settings.SshConnectionTimeout.IntValue),
                Port = (ushort)settings.SshPort.IntValue,
                PreferredUsername = settings.SshUsername.StringValue,
                PublicKeyValidity = TimeSpan.FromSeconds(sshSettings.PublicKeyValidity.IntValue),
                Language = sshSettings.IsPropagateLocaleEnabled.BoolValue
                    ? CultureInfo.CurrentUICulture
                    : null
            };

            return Task.FromResult<ISessionContext<SshCredential>>(context);
        }
    }
}
