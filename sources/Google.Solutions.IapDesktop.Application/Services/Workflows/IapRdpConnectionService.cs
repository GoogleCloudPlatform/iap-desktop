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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Workflows
{
    public class IapRdpConnectionService : IIapUrlHandler
    {
        private const int RemoteDesktopPort = 3389;

        private readonly IWin32Window window;
        private readonly IJobService jobService;
        private readonly IRemoteDesktopService remoteDesktopService;
        private readonly ITunnelBrokerService tunnelBrokerService;
        private readonly ICredentialPrompt credentialPrompt;
        private readonly IProjectExplorer projectExplorer;

        public IapRdpConnectionService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.remoteDesktopService = serviceProvider.GetService<IRemoteDesktopService>();
            this.tunnelBrokerService = serviceProvider.GetService<ITunnelBrokerService>();
            this.credentialPrompt = serviceProvider.GetService<ICredentialPrompt>();
            this.projectExplorer = serviceProvider.GetService<IProjectExplorer>();
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
                        var destination = new TunnelDestination(instanceRef, RemoteDesktopPort);

                        // Give IAP the same timeout for probing as RDP itself.
                        // Note that the timeouts are not additive.
                        var timeout = TimeSpan.FromSeconds(settings.ConnectionTimeout);

                        return await this.tunnelBrokerService.ConnectAsync(destination, timeout)
                            .ConfigureAwait(false);
                    }
                    catch (NetworkStreamClosedException e)
                    {
                        throw new ApplicationException(
                            "Connecting to the instance failed. Make sure that you have " +
                            "configured your firewall rules to permit Cloud IAP access " +
                            $"to {instanceRef.Name}",
                            e);
                    }
                    catch (UnauthorizedException)
                    {
                        throw new ApplicationException(
                            "You are not authorized to connect to this VM instance.\n\n" +
                            $"Verify that the Cloud IAP API is enabled in the project {instanceRef.ProjectId} " +
                            "and that your user has the 'IAP-secured Tunnel User' role.");
                    }
                }).ConfigureAwait(true);

            this.remoteDesktopService.Connect(
                instanceRef,
                "localhost",
                (ushort)tunnel.LocalPort,
                settings);
        }

        public async Task ActivateOrConnectInstanceAsync(IProjectExplorerVmInstanceNode vmNode)
        {
            if (this.remoteDesktopService.TryActivate(vmNode.Reference))
            {
                // RDP session was active, nothing left to do.
                return;
            }

            // Select node so that tracking windows are updated.
            vmNode.Select();

            await this.credentialPrompt.ShowCredentialsPromptAsync(
                    this.window,
                    vmNode.Reference,
                    vmNode.SettingsEditor,
                    true)
                .ConfigureAwait(true);

            await ConnectInstanceAsync(
                    vmNode.Reference,
                    vmNode.SettingsEditor.CreateConnectionSettings(vmNode.Reference.Name))
                .ConfigureAwait(true);
        }

        public async Task ActivateOrConnectInstanceAsync(IapRdpUrl url)
        {
            if (this.remoteDesktopService.TryActivate(url.Instance))
            {
                // RDP session was active, nothing left to do.
                return;
            }

            if (this.projectExplorer.TryFindNode(url.Instance) 
                is IProjectExplorerVmInstanceNode vmNode)
            {
                // We have a full set of settings for this VM, so use that.
                await ActivateOrConnectInstanceAsync(vmNode).ConfigureAwait(true);
                return;
            }

            // We do not know anything other than what's in the URL.

            // Create an ephemeral settings editor. We do not persist
            // any changes.
            var settingsEditor = new ConnectionSettingsEditor(
                url.Settings,
                _ => { },
                null);

            await this.credentialPrompt.ShowCredentialsPromptAsync(
                    this.window,
                    url.Instance,
                    settingsEditor,
                    false)
                .ConfigureAwait(true);

            await ConnectInstanceAsync(
                    url.Instance,
                    settingsEditor.CreateConnectionSettings(url.Instance.Name))
                .ConfigureAwait(true);
        }
    }
}
