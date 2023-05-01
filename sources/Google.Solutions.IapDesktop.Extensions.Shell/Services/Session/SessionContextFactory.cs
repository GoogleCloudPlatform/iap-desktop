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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Threading;
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
        Task<ISessionContext<SshCredential, ISshSessionParameters>> CreateSshSessionContextAsync(
            IProjectModelInstanceNode node,
            CancellationToken cancellationToken);

        /// <summary>
        /// Create a new RDP session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<RdpCredential, IRdpSessionParameters>> CreateRdpSessionContextAsync(
            IProjectModelInstanceNode node,
            RdpCreateSessionFlags flags,
            CancellationToken cancellationToken);

        /// <summary>
        /// Create a new RDP session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<RdpCredential, IRdpSessionParameters>> CreateRdpSessionContextAsync(
            IapRdpUrl url,
            CancellationToken cancellationToken);
    }

    [Flags]
    public enum RdpCreateSessionFlags
    {
        None = 0,
        ForcePasswordPrompt
    }

    [Service(typeof(ISessionContextFactory))]
    public class SessionContextFactory : ISessionContextFactory
    {
        private readonly IWin32Window window;
        private readonly IAuthorization authorization;
        private readonly IProjectModelService projectModelService;
        private readonly IKeyStoreAdapter keyStoreAdapter;
        private readonly IKeyAuthorizationService keyAuthorizationService;
        private readonly IConnectionSettingsService settingsService;
        private readonly SshSettingsRepository sshSettingsRepository;
        private readonly ITunnelBrokerService tunnelBrokerService;
        private readonly ISelectCredentialsWorkflow credentialDialog;
        private readonly IRdpCredentialCallbackService rdpCredentialCallbackService;

        public SessionContextFactory(
            IWin32Window window,
            IAuthorization authorization,
            IProjectModelService projectModelService,
            IKeyStoreAdapter keyStoreAdapter,
            IKeyAuthorizationService keyAuthService,
            IConnectionSettingsService settingsService,
            ITunnelBrokerService tunnelBrokerService,
            ISelectCredentialsWorkflow credentialDialog,
            IRdpCredentialCallbackService credentialCallbackService,
            SshSettingsRepository sshSettingsRepository)
        {
            this.window = window.ExpectNotNull(nameof(window));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.projectModelService = projectModelService.ExpectNotNull(nameof(projectModelService));
            this.keyStoreAdapter = keyStoreAdapter.ExpectNotNull(nameof(keyStoreAdapter));
            this.keyAuthorizationService = keyAuthService.ExpectNotNull(nameof(keyAuthService));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.tunnelBrokerService = tunnelBrokerService.ExpectNotNull(nameof(tunnelBrokerService));
            this.credentialDialog = credentialDialog;
            this.rdpCredentialCallbackService = credentialCallbackService.ExpectNotNull(nameof(credentialCallbackService));
            this.sshSettingsRepository = sshSettingsRepository.ExpectNotNull(nameof(sshSettingsRepository));
        }

        //---------------------------------------------------------------------
        // ISessionContextFactory.
        //---------------------------------------------------------------------

        private static RdpCredential CreateRdpCredentialFromSettings(
            ConnectionSettingsBase settings)
        {
            return new RdpCredential(
                settings.RdpUsername.StringValue,
                settings.RdpDomain.StringValue,
                (SecureString)settings.RdpPassword.Value);
        }

        private RdpSessionContext CreateRdpSession(
            InstanceLocator instance,
            RdpCredential credential,
            InstanceConnectionSettings settings,
            RdpSessionContext.ParameterSources sources)
        {
            return new RdpSessionContext(
                this.tunnelBrokerService, 
                instance, 
                credential, 
                sources)
            {
                Port = (ushort)settings.RdpPort.IntValue,
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
        }

        public async Task<ISessionContext<RdpCredential, IRdpSessionParameters>> CreateRdpSessionContextAsync(
            IProjectModelInstanceNode node,
            RdpCreateSessionFlags flags,
            CancellationToken _)
        {
            node.ExpectNotNull(nameof(node));
            Debug.Assert(node.IsRdpSupported());

            var settings = this.settingsService.GetConnectionSettings(node);

            if (!flags.HasFlag(RdpCreateSessionFlags.ForcePasswordPrompt))
            {
                //
                // Show prompt, and persist any generated credentials.
                //
                await this.credentialDialog
                    .SelectCredentialsAsync(
                        this.window,
                        node.Instance,
                        settings.TypedCollection,
                        RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                        true)
                    .ConfigureAwait(true);

                settings.Save();
            }

            var instanceSettings = (InstanceConnectionSettings)settings.TypedCollection;
            var credential = CreateRdpCredentialFromSettings(instanceSettings);

            if (flags.HasFlag(RdpCreateSessionFlags.ForcePasswordPrompt))
            {
                //
                // Clear (only!) the password to force the default RDP logon
                // screen to appear.
                //
                credential = new RdpCredential(
                    credential.User,
                    credential.Domain,
                    null);
            }

            return CreateRdpSession(
                node.Instance, 
                credential, 
                instanceSettings,
                RdpSessionContext.ParameterSources.Inventory);
        }

        public async Task<ISessionContext<RdpCredential, IRdpSessionParameters>> CreateRdpSessionContextAsync(
            IapRdpUrl url,
            CancellationToken cancellationToken)
        {
            url.ExpectNotNull(nameof(url));

            RdpSessionContext.ParameterSources sources;
            InstanceConnectionSettings settings;
            var existingNode = await this.projectModelService
                .GetNodeAsync(url.Instance, cancellationToken)
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

                sources = RdpSessionContext.ParameterSources.Inventory
                    | RdpSessionContext.ParameterSources.Url;
            }
            else
            {
                //
                // We don't have that VM in the inventory, all we have is the URL.
                //
                settings = InstanceConnectionSettings.FromUrl(url);
                sources = RdpSessionContext.ParameterSources.Url;
            }


            if (url.TryGetParameter("CredentialCallbackUrl", out string callbackUrlRaw) &&
                Uri.TryCreate(callbackUrlRaw, UriKind.Absolute, out var callbackUrl))
            {
                //
                // Invoke callback to obtain credentials.
                //
                var credential = await this.rdpCredentialCallbackService
                    .GetCredentialsAsync(callbackUrl, cancellationToken)
                    .ConfigureAwait(false);

                return CreateRdpSession(
                    url.Instance,
                    credential,
                    settings,
                    sources);
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
                await this.credentialDialog
                    .SelectCredentialsAsync(
                        this.window,
                        url.Instance,
                        settings,
                        allowedBehavior,
                        false)
                    .ConfigureAwait(true);

                return CreateRdpSession(
                    url.Instance,
                    CreateRdpCredentialFromSettings(settings),
                    settings,
                    sources);
            }
        }

        public Task<ISessionContext<SshCredential, ISshSessionParameters>> CreateSshSessionContextAsync(
            IProjectModelInstanceNode node,
            CancellationToken _)
        {
            node.ExpectNotNull(nameof(node));
            Debug.Assert(node.IsSshSupported());

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

            return Task.FromResult<ISessionContext<SshCredential, ISshSessionParameters>>(context);
        }
    }
}
