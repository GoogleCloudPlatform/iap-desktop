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
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    public interface ISshConnectionService
    {
        Task ActivateOrConnectInstanceAsync(
            IProjectModelInstanceNode vmNode);

        Task ConnectInstanceAsync(
            IProjectModelInstanceNode vmNode);
    }

    [Service(typeof(ISshConnectionService))]
    public class SshConnectionService : ISshConnectionService
    {
        private readonly IWin32Window window;
        private readonly IJobService jobService;
        private readonly ISshTerminalSessionBroker sessionBroker;
        private readonly ITunnelBrokerService tunnelBroker;
        private readonly IConnectionSettingsService settingsService;
        private readonly IKeyAuthorizationService authorizedKeyService;
        private readonly IKeyStoreAdapter keyStoreAdapter;
        private readonly IAuthorizationSource authorizationSource;
        private readonly SshSettingsRepository sshSettingsRepository;
        private readonly IProjectModelService projectModelService;

        public SshConnectionService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.sessionBroker = serviceProvider.GetService<ISshTerminalSessionBroker>();
            this.tunnelBroker = serviceProvider.GetService<ITunnelBrokerService>();
            this.settingsService = serviceProvider.GetService<IConnectionSettingsService>();
            this.authorizedKeyService = serviceProvider.GetService<IKeyAuthorizationService>();
            this.keyStoreAdapter = serviceProvider.GetService<IKeyStoreAdapter>();
            this.authorizationSource = serviceProvider.GetService<IAuthorizationSource>();
            this.sshSettingsRepository = serviceProvider.GetService<SshSettingsRepository>();
            this.projectModelService = serviceProvider.GetService<IProjectModelService>();
            this.window = serviceProvider.GetService<IMainForm>().Window;
        }

        //---------------------------------------------------------------------
        // ISshConnectionService.
        //---------------------------------------------------------------------

        public async Task ActivateOrConnectInstanceAsync(IProjectModelInstanceNode vmNode)
        {
            Debug.Assert(vmNode.IsSshSupported());

            if (this.sessionBroker.TryActivate(vmNode.Instance))
            {
                // SSH session was active, nothing left to do.
                return;
            }

            await ConnectInstanceAsync(vmNode).ConfigureAwait(true);
        }

        public async Task ConnectInstanceAsync(IProjectModelInstanceNode vmNode)
        {
            Debug.Assert(vmNode.IsSshSupported());

            // Select node so that tracking windows are updated.
            await this.projectModelService.SetActiveNodeAsync(
                    vmNode,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var instance = vmNode.Instance;
            var settings = (InstanceConnectionSettings)this.settingsService
                .GetConnectionSettings(vmNode)
                .TypedCollection;
            var timeout = TimeSpan.FromSeconds(settings.SshConnectionTimeout.IntValue);

            //
            // Start job to create IAP tunnel.
            //

            var tunnelTask = this.jobService.RunInBackground(
                new JobDescription(
                    $"Opening Cloud IAP tunnel to {instance.Name}...",
                    JobUserFeedbackType.BackgroundFeedback),
                async token =>
                {
                    try
                    {
                        var destination = new TunnelDestination(
                            vmNode.Instance,
                            (ushort)settings.SshPort.IntValue);

                        // NB. Give IAP the same timeout for probing as SSH itself.
                        return await this.tunnelBroker.ConnectAsync(
                                destination,
                                new SameProcessRelayPolicy(),
                                timeout)
                            .ConfigureAwait(false);
                    }
                    catch (NetworkStreamClosedException e)
                    {
                        throw new ConnectionFailedException(
                            "Connecting to the instance failed. Make sure that you have " +
                            "configured your firewall rules to permit Cloud IAP access " +
                            $"to {instance.Name}",
                            HelpTopics.CreateIapFirewallRule,
                            e);
                    }
                    catch (UnauthorizedException)
                    {
                        throw new ConnectionFailedException(
                            "You are not authorized to connect to this VM instance.\n\n" +
                            $"Verify that the Cloud IAP API is enabled in the project {instance.ProjectId} " +
                            "and that your user has the 'IAP-secured Tunnel User' role.",
                            HelpTopics.IapAccess);
                    }
                    catch (WebSocketConnectionDeniedException)
                    {
                        throw new ConnectionFailedException(
                            "Establishing an IAP tunnel failed because the server " +
                            "denied access.\n\n" +
                            "If you are using a proxy server, make sure that the proxy " +
                            "server allows WebSocket connections.",
                            HelpTopics.ProxyConfiguration);
                    }
                });


            //
            // Load persistent CNG key. This must be done on the UI thread.
            //
            var sshSettings = this.sshSettingsRepository.GetSettings();
            var sshKey = this.keyStoreAdapter.OpenSshKeyPair(
                sshSettings.PublicKeyType.EnumValue,
                this.authorizationSource.Authorization,
                true,
                this.window);
            Debug.Assert(sshKey != null);

            //
            // Start job to publish key, using whatever mechanism is appropriate
            // for this instance.
            //

            try
            {
                var authorizedKeyTask = this.jobService.RunInBackground(
                    new JobDescription(
                        $"Publishing SSH key for {instance.Name}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    async token =>
                    {
                        //
                        // Authorize the key.
                        //
                        return await this.authorizedKeyService.AuthorizeKeyAsync(
                                vmNode.Instance,
                                sshKey,
                                TimeSpan.FromSeconds(sshSettings.PublicKeyValidity.IntValue),
                                settings.SshUsername.StringValue.NullIfEmpty(),
                                KeyAuthorizationMethods.All,
                                token)
                            .ConfigureAwait(true);
                    });

                //
                // Wait for both jobs to continue (they are both fairly slow).
                //

                await Task.WhenAll(tunnelTask, authorizedKeyTask)
                    .ConfigureAwait(true);

                var language = sshSettings.IsPropagateLocaleEnabled.BoolValue
                     ? CultureInfo.CurrentUICulture
                     : null;

                //
                // NB. ConnectAsync takes ownership of the key and will retain
                // it for the lifetime of the session.
                //
                await this.sessionBroker.ConnectAsync(
                        instance,
                        new IPEndPoint(IPAddress.Loopback, tunnelTask.Result.LocalPort),
                        authorizedKeyTask.Result,
                        language,
                        timeout)
                    .ConfigureAwait(true);
            }
            catch (Exception)
            {
                sshKey.Dispose();
                throw;
            }
        }
    }
}
