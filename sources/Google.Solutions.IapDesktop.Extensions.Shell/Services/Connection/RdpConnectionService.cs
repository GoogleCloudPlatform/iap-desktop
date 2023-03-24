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
        private readonly IRdpCredentialCallbackService credentialCallbackService;

        public RdpConnectionService(
            IMainWindow window,
            IProjectModelService projectModelService,
            ITunnelBrokerService tunnelBroker,
            IJobService jobService,
            IConnectionSettingsService settingsService,
            ISelectCredentialsWorkflow credentialPrompt,
            IRdpCredentialCallbackService credentialCallbackService)
            : base(jobService, tunnelBroker)
        {
            this.window = window.ThrowIfNull(nameof(window));
            this.projectModelService = projectModelService.ThrowIfNull(nameof(projectModelService));
            this.settingsService = settingsService.ThrowIfNull(nameof(settingsService));
            this.credentialPrompt = credentialPrompt.ThrowIfNull(nameof(credentialPrompt));
            this.credentialCallbackService = credentialCallbackService.ThrowIfNull(nameof(credentialCallbackService));
        }

        private async Task<ConnectionTemplate<RdpSessionParameters>> PrepareConnectionAsync(
            InstanceLocator instance,
            InstanceConnectionSettings settings)
        {
            var timeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.IntValue);

            var transportParameters = await PrepareTransportAsync(
                    instance,
                    (ushort)settings.RdpPort.IntValue,
                    timeout)
                .ConfigureAwait(false);

            var rdpParameters = new RdpSessionParameters(new RdpCredentials(
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
                //
                // Show prompt, and persist any generated credentials.
                //
                await this.credentialPrompt.SelectCredentialsAsync(
                        this.window,
                        vmNode.Instance,
                        settings.TypedCollection,
                        RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                        true)
                    .ConfigureAwait(true);

                settings.Save();
            }

            var template = await PrepareConnectionAsync(
                    vmNode.Instance,
                    (InstanceConnectionSettings)settings.TypedCollection) 
                .ConfigureAwait(true);

            if (!allowPersistentCredentials)
            {
                //
                // Clear the password to force the default RDP logon
                // screen to appear.
                //
                template.Session.Credentials = new RdpCredentials(
                    template.Session.Credentials.User,
                    template.Session.Credentials.Domain,
                    null);
            }

            return template;
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

            if (url.TryGetParameter("CredentialCallbackUrl", out string callbackUrlRaw) &&
                Uri.TryCreate(callbackUrlRaw, UriKind.Absolute, out var callbackUrl))
            {
                var template = await PrepareConnectionAsync(
                        url.Instance,
                        settings)
                    .ConfigureAwait(true);

                //
                // Invoke callback and replace existing credentials.
                //
                template.Session.Credentials = await this.credentialCallbackService
                    .GetCredentialsAsync(callbackUrl,
                    CancellationToken.None);

                return template;
            }
            else
            {
                if (!url.TryGetParameter<RdpCredentialGenerationBehavior>(
                    "CredentialGenerationBehavior", 
                    out var allowedBehavior))
                {
                    allowedBehavior = RdpCredentialGenerationBehavior._Default;
                }

                //
                // Show prompt, but don't persist any generated credentials.
                //
                await this.credentialPrompt.SelectCredentialsAsync(
                        this.window,
                        url.Instance,
                        settings,
                        allowedBehavior,
                        false)
                    .ConfigureAwait(true);

                return await PrepareConnectionAsync(
                        url.Instance,
                        settings)
                    .ConfigureAwait(true);
            }
        }
    }
}
