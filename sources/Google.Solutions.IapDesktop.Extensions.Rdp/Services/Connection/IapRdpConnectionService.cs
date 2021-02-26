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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection
{
    [Service]
    public class IapRdpConnectionService
    {
        private readonly IWin32Window window;
        private readonly IJobService jobService;
        private readonly IRemoteDesktopSessionBroker remoteDesktopService;
        private readonly ITunnelBrokerService tunnelBrokerService;
        private readonly ICredentialPrompt credentialPrompt;
        private readonly IProjectExplorer projectExplorer;
        private readonly IConnectionSettingsService settingsService;

        public IapRdpConnectionService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.remoteDesktopService = serviceProvider.GetService<IRemoteDesktopSessionBroker>();
            this.tunnelBrokerService = serviceProvider.GetService<ITunnelBrokerService>();
            this.credentialPrompt = serviceProvider.GetService<ICredentialPrompt>();
            this.projectExplorer = serviceProvider.GetService<IProjectExplorer>();
            this.settingsService = serviceProvider.GetService<IConnectionSettingsService>();
            this.window = serviceProvider.GetService<IMainForm>().Window;
        }

        private async Task ConnectInstanceAsync(
            InstanceLocator instanceRef,
            VmInstanceConnectionSettings settings)
        {
            var tunnel = await this.jobService.RunInBackground(
                new JobDescription(
                    $"Opening Cloud IAP tunnel to {instanceRef.Name}...",
                    JobUserFeedbackType.BackgroundFeedback),
                async token =>
                {
                    try
                    {
                        var destination = new TunnelDestination(
                            instanceRef,
                            (ushort)settings.RdpPort.IntValue);

                        // Give IAP the same timeout for probing as RDP itself.
                        // Note that the timeouts are not additive.
                        var timeout = TimeSpan.FromSeconds(settings.ConnectionTimeout.IntValue);

                        return await this.tunnelBrokerService.ConnectAsync(
                                destination,
                                new SameProcessRelayPolicy(),
                                timeout)
                            .ConfigureAwait(false);
                    }
                    catch (NetworkStreamClosedException e)
                    {
                        throw new IapRdpConnectionFailedException(
                            "Connecting to the instance failed. Make sure that you have " +
                            "configured your firewall rules to permit Cloud IAP access " +
                            $"to {instanceRef.Name}",
                            HelpTopics.CreateIapFirewallRule,
                            e);
                    }
                    catch (UnauthorizedException)
                    {
                        throw new IapRdpConnectionFailedException(
                            "You are not authorized to connect to this VM instance.\n\n" +
                            $"Verify that the Cloud IAP API is enabled in the project {instanceRef.ProjectId} " +
                            "and that your user has the 'IAP-secured Tunnel User' role.",
                            HelpTopics.IapAccess);
                    }
                }).ConfigureAwait(true);

            this.remoteDesktopService.Connect(
                instanceRef,
                "localhost",
                (ushort)tunnel.LocalPort,
                settings);
        }

        public async Task ActivateOrConnectInstanceAsync(
            IProjectExplorerVmInstanceNode vmNode,
            bool allowPersistentCredentials)
        {
            if (this.remoteDesktopService.TryActivate(vmNode.Reference))
            {
                // RDP session was active, nothing left to do.
                return;
            }

            // Select node so that tracking windows are updated.
            vmNode.Select();

            var settings = this.settingsService.GetConnectionSettings(vmNode);

            if (allowPersistentCredentials)
            {
                await this.credentialPrompt.ShowCredentialsPromptAsync(
                        this.window,
                        vmNode.Reference,
                        settings.TypedCollection,
                        true)
                    .ConfigureAwait(true);

                // Persist new credentials.
                settings.Save();
            }
            else
            {
                //
                //Temporarily clear persisted credentials so that the
                // default credential prompt is triggered.
                //
                // NB. Use an empty string (as opposed to null) to
                // avoid an inherited setting from kicking in.
                //
                settings.TypedCollection.Password.Value = string.Empty;
            }

            await ConnectInstanceAsync(
                    vmNode.Reference,
                    (VmInstanceConnectionSettings)settings.TypedCollection)
                .ConfigureAwait(true);
        }

        public async Task ActivateOrConnectInstanceAsync(IapRdpUrl url)
        {
            if (this.remoteDesktopService.TryActivate(url.Instance))
            {
                // RDP session was active, nothing left to do.
                return;
            }

            VmInstanceConnectionSettings settings;
            if (this.projectExplorer.TryFindNode(url.Instance)
                is IProjectExplorerVmInstanceNode vmNode)
            {
                // We have a full set of settings for this VM, so use that as basis
                settings = (VmInstanceConnectionSettings)
                    this.settingsService.GetConnectionSettings(vmNode).TypedCollection;

                // Apply parameters from URL on top.
                settings.ApplyUrlQuery(url.Parameters);
            }
            else
            {
                settings = VmInstanceConnectionSettings.FromUrl(url);
            }

            await this.credentialPrompt.ShowCredentialsPromptAsync(
                    this.window,
                    url.Instance,
                    settings,
                    false)
                .ConfigureAwait(true);

            await ConnectInstanceAsync(
                    url.Instance,
                    settings)
                .ConfigureAwait(true);
        }
    }

    public class IapRdpConnectionFailedException : ApplicationException, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public IapRdpConnectionFailedException(
            string message,
            IHelpTopic helpTopic) : base(message)
        {
            this.Help = helpTopic;
        }

        public IapRdpConnectionFailedException(
            string message,
            IHelpTopic helpTopic,
            Exception innerException)
            : base(message, innerException)
        {
            this.Help = helpTopic;
        }
    }
}
