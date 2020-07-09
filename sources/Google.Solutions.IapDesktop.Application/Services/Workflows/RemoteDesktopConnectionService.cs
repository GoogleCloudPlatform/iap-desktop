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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private struct CredentialOption
        {
            public string Title;
            public Func<Task> Apply;
        }

        private CredentialOption ShowCredentialOptionsTaskDialog(
            IWin32Window owner,
            string prompt,
            string details,
            IList<CredentialOption> options)
        {
            Debug.Assert(options.Count > 0);
            if (options.Count == 1)
            {
                // If there is only one option, do not prompt at all.
                return options[0];
            }

            // NB. The sequence of options determines the behavior of 
            // Enter/ESC and OK/Cancel:
            //
            //  Enter/OK   -> first option.
            //  ESC/Cancel -> last option.

            int selectedOption = this.taskDialog.ShowOptionsTaskDialog(
                owner,
                UnsafeNativeMethods.TD_INFORMATION_ICON,
                "Credentials",
                prompt,
                details,
                null,
                options.Select(o => o.Title).ToList(),
                null,   //"Do not show this prompt again",
                out bool donotAskAgain);
            return options[selectedOption];
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
                var options = new List<CredentialOption>()
                {
                    new CredentialOption()
                    {
                        Title = "Configure credentials",
                        Apply = () =>
                        {
                            // Configure credentials -> jump to settings.
                            this.settingsEditor.ShowWindow();
                            return Task.CompletedTask;
                        }
                    },
                    new CredentialOption()
                    {
                        Title = "Generate new credentials",
                        Apply = () => this.credentialsService.GenerateCredentialsAsync(
                            owner,
                            vmNode.Reference,
                            vmNode.SettingsEditor)
                    },
                    new CredentialOption()
                    {
                        Title = "Connect anyway",
                        Apply = () => Task.CompletedTask
                    }
                };

                await ShowCredentialOptionsTaskDialog(
                        owner,
                        $"You have not configured any credentials for {vmNode.InstanceName}",
                        "Would you like to configure or generate credentials now?",
                        options)
                    .Apply()
                    .ConfigureAwait(true);
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

            var options = new List<CredentialOption>()
                {
                    new CredentialOption()
                    {
                        Title = "Yes, generate new credentials",
                        Apply = () => this.credentialsService.GenerateCredentialsAsync(
                            owner,
                            url.Instance,
                            settingsEditor,
                            MakeNullIfEmpty(url.Settings.Username))
                    },
                    new CredentialOption()
                    {
                        Title = "Enter existing credentials",
                        Apply = () => Task.CompletedTask
                    }
                };

            await ShowCredentialOptionsTaskDialog(
                    owner,
                    $"Would you like to generate credentials for {url.Instance.Name} first?",
                    null,
                    options)
                .Apply()
                .ConfigureAwait(true);

            await ConnectInstanceAsync(
                    url.Instance,
                    settingsEditor.CreateConnectionSettings(url.Instance.Name))
                .ConfigureAwait(true);
        }
    }
}
