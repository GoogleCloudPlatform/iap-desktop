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
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using System;
using System.Diagnostics;
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
    public class RdpConnectionService : ConnectionServiceBase, IRdpConnectionService
    {
        private readonly IWin32Window window;
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
            : base(jobService, tunnelBroker)
        {
            this.window = window.ThrowIfNull(nameof(window));
            this.projectModelService = projectModelService.ThrowIfNull(nameof(projectModelService));
            this.settingsService = settingsService.ThrowIfNull(nameof(settingsService));
            this.credentialPrompt = credentialPrompt.ThrowIfNull(nameof(credentialPrompt));
        }

        private async Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(
            InstanceLocator instance,
            InstanceConnectionSettings settings,
            bool allowPersistentCredentials)
        {
            var timeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.IntValue);

            var transportParameters = await PrepareTransportAsync(
                    instance,
                    (ushort)settings.RdpPort.IntValue,
                    timeout)
                .ConfigureAwait(false);

            var credentials = new RdpCredentials(
                settings.RdpUsername.StringValue,
                settings.RdpDomain.StringValue,
                allowPersistentCredentials
                    ? (SecureString)settings.RdpPassword.Value
                    : null);

            var rdpParameters = new RdpSessionParameters(credentials)
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
                transportParameters,
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

            //
            // Select node so that tracking windows are updated.
            //
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

            // TODO: Reset password instead of passing flag

            return await PrepareConnectionAsync(
                    vmNode.Instance,
                    (InstanceConnectionSettings)settings.TypedCollection,
                    allowPersistentCredentials) 
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

            // TODO: use const
            if (url.TryGetParameter("CredentialCallbackUrl", out string callbackUrlRaw) &&
                Uri.TryCreate(callbackUrlRaw, UriKind.Absolute, out var callbackUrl))
            {
                var template = await PrepareConnectionAsync(
                        url.Instance,
                        settings,
                        true)
                    .ConfigureAwait(true);

                // TODO: Invoke callback
                // TODO: Update template.Session.Credentials
                
                return template;
            }
            else
            {
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
                        settings,
                        true)
                    .ConfigureAwait(true);
            }
        }
    }
}
