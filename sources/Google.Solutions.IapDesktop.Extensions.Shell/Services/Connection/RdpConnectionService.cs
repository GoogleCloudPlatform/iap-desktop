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

using Google.Apis.Util;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
{
    public interface IRdpConnectionService
    {
        Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(
            IProjectModelInstanceNode vmNode,
            bool allowPersistentCredentials);

        Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(IapRdpUrl url);
    }

    [Service(typeof(IRdpConnectionService))]
    public class RdpConnectionService : IRdpConnectionService
    {
        private readonly IWin32Window window;
        private readonly IJobService jobService;
        private readonly ITunnelBrokerService tunnelBroker;
        private readonly ISelectCredentialsWorkflow credentialPrompt;
        private readonly IProjectModelService projectModelService;
        private readonly IConnectionSettingsService settingsService;

        public RdpConnectionService(
            IMainWindow window,
            IProjectModelService projectModelService,
            ITunnelBrokerService tunnelBroker,
            IJobService jobService,
            IConnectionSettingsService settingsService,
            ISelectCredentialsWorkflow credentialPrompt)
        {
            this.window = window.ThrowIfNull(nameof(window));
            this.projectModelService = projectModelService.ThrowIfNull(nameof(projectModelService));
            this.tunnelBroker = tunnelBroker.ThrowIfNull(nameof(tunnelBroker));
            this.jobService = jobService.ThrowIfNull(nameof(jobService));
            this.settingsService = settingsService.ThrowIfNull(nameof(settingsService));
            this.credentialPrompt = credentialPrompt.ThrowIfNull(nameof(credentialPrompt));
        }

        private async Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(
            InstanceLocator instance,
            InstanceConnectionSettings settings)
        {
            var timeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.IntValue);
            var tunnel = await this.jobService.RunInBackground(
                new JobDescription(
                    $"Opening Cloud IAP tunnel to {instance.Name}...",
                    JobUserFeedbackType.BackgroundFeedback),
                async token =>
                {
                    try
                    {
                        var destination = new TunnelDestination(
                            instance,
                            (ushort)settings.RdpPort.IntValue);

                        // Give IAP the same timeout for probing as RDP itself.
                        // Note that the timeouts are not additive.

                        return await this.tunnelBroker.ConnectAsync(
                                destination,
                                new SameProcessRelayPolicy(),
                                timeout)
                            .ConfigureAwait(false);
                    }
                    catch (SshRelayDeniedException e)
                    {
                        throw new ConnectionFailedException(
                            "You are not authorized to connect to this VM instance.\n\n" +
                            $"Verify that the Cloud IAP API is enabled in the project {instance.ProjectId} " +
                            "and that your user has the 'IAP-secured Tunnel User' role.",
                            HelpTopics.IapAccess,
                            e);
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
                    catch (WebSocketConnectionDeniedException)
                    {
                        throw new ConnectionFailedException(
                            "Establishing an IAP tunnel failed because the server " +
                            "denied access.\n\n" +
                            "If you are using a proxy server, make sure that the proxy " +
                            "server allows WebSocket connections.",
                            HelpTopics.ProxyConfiguration);
                    }
                }).ConfigureAwait(true);

            var rdpParameters = new RdpSessionParameters(
                new RdpCredentials(
                    settings.RdpUsername.StringValue,
                    settings.RdpDomain.StringValue,
                    (SecureString)settings.RdpPassword.Value))
            {
                ConnectionTimeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.IntValue),

                ConnectionBar = settings.RdpConnectionBar.EnumValue,
                DesktopSize = settings.RdpDesktopSize.EnumValue,
                AuthenticationLevel = settings.RdpAuthenticationLevel.EnumValue,
                ColorDepth = settings.RdpColorDepth.EnumValue,
                AudioMode = settings.RdpAudioMode.EnumValue,
                BitmapPersistence = settings.RdpBitmapPersistence.EnumValue,
                NetworkLevelAuthentication = settings.RdpNetworkLevelAuthentication.EnumValue,

                UserAuthenticationBehavior = settings.RdpUserAuthenticationBehavior.EnumValue,
                CredentialGenerationBehavior = settings.RdpCredentialGenerationBehavior.EnumValue,
                
                RedirectClipboard = settings.RdpRedirectClipboard.EnumValue,
                RedirectPrinter = settings.RdpRedirectPrinter.EnumValue,
                RedirectSmartCard = settings.RdpRedirectSmartCard.EnumValue,
                RedirectPort = settings.RdpRedirectPort.EnumValue,
                RedirectDrive = settings.RdpRedirectDrive.EnumValue,
                RedirectDevice = settings.RdpRedirectDevice.EnumValue,
                HookWindowsKeys = settings.RdpHookWindowsKeys.EnumValue,
            };

            return new ConnectionTemplate<RdpSessionParameters>(
                new TransportParameters(
                    TransportParameters.TransportType.IapTunnel,
                    instance,
                    new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort)),
                rdpParameters);
        }

        //---------------------------------------------------------------------
        // IRdpConnectionService.
        //---------------------------------------------------------------------

        public async Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(
            IProjectModelInstanceNode vmNode,
            bool allowPersistentCredentials)
        {
            Debug.Assert(vmNode.IsRdpSupported());

            // Select node so that tracking windows are updated.
            await this.projectModelService.SetActiveNodeAsync(
                    vmNode,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var settings = this.settingsService.GetConnectionSettings(vmNode);

            if (allowPersistentCredentials)
            {
                await this.credentialPrompt.SelectCredentialsAsync(
                        this.window,
                        vmNode.Instance,
                        settings.TypedCollection,
                        true)
                    .ConfigureAwait(true);

                // Persist new credentials.
                settings.Save();
            }
            else
            {
                //
                // Temporarily clear persisted credentials so that the
                // default credential prompt is triggered.
                //
                // NB. Use an empty string (as opposed to null) to
                // avoid an inherited setting from kicking in.
                //
                settings.TypedCollection.RdpPassword.Value = string.Empty;
            }

            return await PrepareConnectionAsync(
                    vmNode.Instance,
                    (InstanceConnectionSettings)settings.TypedCollection)
                .ConfigureAwait(true);
        }

        public async Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(IapRdpUrl url)
        {
            InstanceConnectionSettings settings;
            var existingNode = await this.projectModelService
                .GetNodeAsync(url.Instance, CancellationToken.None)
                .ConfigureAwait(true);
            if (existingNode is IProjectModelInstanceNode vmNode)
            {
                //
                // We have a full set of settings for this VM, so use that as basis.
                //
                settings = (InstanceConnectionSettings)
                    this.settingsService.GetConnectionSettings(vmNode).TypedCollection;

                //
                // Apply parameters from URL on top.
                //
                settings.ApplySettingsFromUrl(url);
            }
            else
            {
                settings = InstanceConnectionSettings.FromUrl(url);
            }

            //
            // Show prompt, but don't persist any generated credentials.
            //
            await this.credentialPrompt.SelectCredentialsAsync(
                    this.window,
                    url.Instance,
                    settings,
                    false)
                .ConfigureAwait(true);

            return await PrepareConnectionAsync(
                    url.Instance,
                    settings)
                .ConfigureAwait(true);
        }
    }
}
