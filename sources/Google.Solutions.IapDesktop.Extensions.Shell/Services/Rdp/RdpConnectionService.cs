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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp
{
    public interface IRdpConnectionService
    {
        Task<IRemoteDesktopSession> ActivateOrConnectInstanceAsync(
            IProjectModelInstanceNode vmNode,
            bool allowPersistentCredentials);

        Task<IRemoteDesktopSession> ActivateOrConnectInstanceAsync(IapRdpUrl url);
    }

    [Service(typeof(IRdpConnectionService))]
    public class RdpConnectionService : IRdpConnectionService
    {
        private readonly IWin32Window window;
        private readonly IJobService jobService;
        private readonly IRemoteDesktopSessionBroker sessionBroker;
        private readonly ITunnelBrokerService tunnelBroker;
        private readonly ISelectCredentialsWorkflow credentialPrompt;
        private readonly IProjectModelService projectModelService;
        private readonly IConnectionSettingsService settingsService;

        public RdpConnectionService(
            IMainWindow window,
            IProjectModelService projectModelService,
            IRemoteDesktopSessionBroker sessionBroker,
            ITunnelBrokerService tunnelBroker,
            IJobService jobService,
            IConnectionSettingsService settingsService,
            ISelectCredentialsWorkflow credentialPrompt)
        {
            this.window = window.ThrowIfNull(nameof(window));
            this.projectModelService = projectModelService.ThrowIfNull(nameof(projectModelService));
            this.sessionBroker = sessionBroker.ThrowIfNull(nameof(sessionBroker));
            this.tunnelBroker = tunnelBroker.ThrowIfNull(nameof(tunnelBroker));
            this.jobService = jobService.ThrowIfNull(nameof(jobService));
            this.settingsService = settingsService.ThrowIfNull(nameof(sessionBroker));
            this.credentialPrompt = credentialPrompt.ThrowIfNull(nameof(credentialPrompt));
        }

        private async Task<IRemoteDesktopSession> ConnectInstanceAsync(
            InstanceLocator instance,
            InstanceConnectionSettings settings)
        {
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
                        var timeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.IntValue);

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

            return this.sessionBroker.Connect(
                instance,
                "localhost",
                (ushort)tunnel.LocalPort,
                settings);
        }

        //---------------------------------------------------------------------
        // IRdpConnectionService.
        //---------------------------------------------------------------------

        public async Task<IRemoteDesktopSession> ActivateOrConnectInstanceAsync(
            IProjectModelInstanceNode vmNode,
            bool allowPersistentCredentials)
        {
            Debug.Assert(vmNode.IsRdpSupported());

            if (this.sessionBroker.TryActivate(vmNode.Instance, out var activeSession))
            {
                // RDP session was active, nothing left to do.
                Debug.Assert(activeSession != null);
                Debug.Assert(activeSession is IRemoteDesktopSession);

                return (IRemoteDesktopSession)activeSession;
            }

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

            return await ConnectInstanceAsync(
                    vmNode.Instance,
                    (InstanceConnectionSettings)settings.TypedCollection)
                .ConfigureAwait(true);
        }

        public async Task<IRemoteDesktopSession> ActivateOrConnectInstanceAsync(IapRdpUrl url)
        {
            if (this.sessionBroker.TryActivate(url.Instance, out var activeSession))
            {
                // RDP session was active, nothing left to do.
                Debug.Assert(activeSession != null);
                Debug.Assert(activeSession is IRemoteDesktopSession);

                return (IRemoteDesktopSession)activeSession;
            }

            InstanceConnectionSettings settings;
            var existingNode = await this.projectModelService
                .GetNodeAsync(url.Instance, CancellationToken.None)
                .ConfigureAwait(true);
            if (existingNode is IProjectModelInstanceNode vmNode)
            {
                //
                // We have a full set of settings for this VM, so use that as basis
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

            await this.credentialPrompt.SelectCredentialsAsync(
                    this.window,
                    url.Instance,
                    settings,
                    false)
                .ConfigureAwait(true);

            return await ConnectInstanceAsync(
                    url.Instance,
                    settings)
                .ConfigureAwait(true);
        }
    }
}
