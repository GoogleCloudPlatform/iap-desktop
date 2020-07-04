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

using Google.Solutions.Common;
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
    public class RemoteDesktopConnectionService
    {
        private const int RemoteDesktopPort = 3389;

        private readonly IJobService jobService;
        private readonly IRemoteDesktopService remoteDesktopService;
        private readonly ITunnelBrokerService tunnelBrokerService;
        private readonly IConnectionSettingsWindow settingsEditor;
        private readonly ICredentialsService credentialsService;
        private readonly ITaskDialog taskDialog;

        private static string MakeNullIfEmpty(string s)
            => string.IsNullOrEmpty(s) ? null : s;

        public RemoteDesktopConnectionService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.remoteDesktopService = serviceProvider.GetService<IRemoteDesktopService>();
            this.tunnelBrokerService = serviceProvider.GetService<ITunnelBrokerService>();
            this.settingsEditor = serviceProvider.GetService<IConnectionSettingsWindow>();
            this.credentialsService = serviceProvider.GetService<ICredentialsService>();
            this.taskDialog = serviceProvider.GetService<ITaskDialog>();
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

        public async Task ActivateOrConnectInstanceWithCredentialPromptAsync(
            IWin32Window owner,
            VmInstanceNode vmNode)
        {
            if (this.remoteDesktopService.TryActivate(vmNode.Reference))
            {
                // RDP session was active, nothing left to do.
                return;
            }

            // Select node so that tracking windows are updated.
            vmNode.Select();

            var settings = vmNode.SettingsEditor;
            if (string.IsNullOrEmpty(settings.Username) || settings.Password == null || settings.Password.Length == 0)
            {
                int selectedOption = this.taskDialog.ShowOptionsTaskDialog(
                    owner,
                    UnsafeNativeMethods.TD_INFORMATION_ICON,
                    "Credentials",
                    $"You have not configured any credentials for {vmNode.InstanceName}",
                    "Would you like to configure or generate credentials now?",
                    null,
                    new[]
                    {
                        "Configure credentials",
                        "Generate new credentials",     // Same as pressing 'OK'
                        "Connect anyway"                // Same as pressing 'Cancel'
                    },
                    null,//"Do not show this prompt again",
                    out bool donotAskAgain);

                if (selectedOption == 0)
                {
                    // Configure credentials -> jump to settings.
                    this.settingsEditor.ShowWindow();

                    return;
                }
                else if (selectedOption == 1)
                {
                    // Generate new credentials.
                    await this.credentialsService.GenerateCredentialsAsync(
                            owner,
                            vmNode.Reference,
                            vmNode.SettingsEditor)
                        .ConfigureAwait(true);
                }
                else if (selectedOption == 2)
                {
                    // Cancel - just continue connecting.
                }
            }

            await ConnectInstanceAsync(
                    vmNode.Reference,
                    vmNode.CreateConnectionSettings())
                .ConfigureAwait(true);
        }

        public async Task ActivateOrConnectInstanceWithCredentialPromptAsync(
            IWin32Window owner,
            IapRdpUrl url)
        {
            if (this.remoteDesktopService.TryActivate(url.Instance))
            {
                // RDP session was active, nothing left to do.
                return;
            }

            // Create an ephemeral settings editor. We do not persist
            // any changes.
            var settingsEditor = new ConnectionSettingsEditor(
                url.Settings,
                _ => { },
                null);

            int selectedOption = this.taskDialog.ShowOptionsTaskDialog(
                owner,
                UnsafeNativeMethods.TD_INFORMATION_ICON,
                "Credentials",
                $"Would you like to generate credentials for {url.Instance.Name} first?",
                null,
                null,
                new[]
                {
                    "Yes, generate new credentials",     // Same as pressing 'OK'
                    "Enter existing credentials"         // Same as pressing 'Cancel'
                },
                null,
                out bool donotAskAgain);

            if (selectedOption == 0)
            {
                // Generate new credentials using the ephemeral settings editor.
                await this.credentialsService.GenerateCredentialsAsync(
                        owner,
                        url.Instance,
                        settingsEditor,
                        MakeNullIfEmpty(url.Settings.Username))
                    .ConfigureAwait(true);
            }
            else if (selectedOption == 1)
            {
                // Cancel - just continue connecting.
            }

            await ConnectInstanceAsync(
                    url.Instance,
                    settingsEditor.CreateConnectionSettings(url.Instance.Name))
                .ConfigureAwait(true);
        }
    }
}
