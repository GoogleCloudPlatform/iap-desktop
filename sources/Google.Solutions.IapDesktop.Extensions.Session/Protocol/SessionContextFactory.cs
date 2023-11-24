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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Platform.Cryptography;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol
{
    public interface ISessionContextFactory
    {
        /// <summary>
        /// Create a new SSH session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<SshAuthorizedKeyCredential, SshParameters>> CreateSshSessionContextAsync(
            IProjectModelInstanceNode node,
            CancellationToken cancellationToken);

        /// <summary>
        /// Create a new RDP session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<RdpCredential, RdpParameters>> CreateRdpSessionContextAsync(
            IProjectModelInstanceNode node,
            RdpCreateSessionFlags flags,
            CancellationToken cancellationToken);

        /// <summary>
        /// Create a new RDP session context. The method might require UI
        /// interactiion.
        /// </summary>
        Task<ISessionContext<RdpCredential, RdpParameters>> CreateRdpSessionContextAsync(
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
        internal static readonly TimeSpan EphemeralKeyValidity = TimeSpan.FromDays(1);

        private readonly IWin32Window window;
        private readonly IAuthorization authorization;
        private readonly IProjectWorkspace workspace;
        private readonly IKeyStore keyStore;
        private readonly IKeyAuthorizer keyAuthorizer;
        private readonly IConnectionSettingsService settingsService;
        private readonly IRepository<ISshSettings> sshSettingsRepository;
        private readonly IIapTransportFactory iapTransportFactory;
        private readonly IDirectTransportFactory directTransportFactory;
        private readonly ISelectCredentialsDialog credentialDialog;
        private readonly IRdpCredentialCallback rdpCredentialCallbackService;

        public SessionContextFactory(
            IMainWindow window,
            IAuthorization authorization,
            IProjectWorkspace workspace,
            IKeyStore keyStoreAdapter,
            IKeyAuthorizer keyAuthorizer,
            IConnectionSettingsService settingsService,
            IIapTransportFactory iapTransportFactory,
            IDirectTransportFactory directTransportFactory,
            ISelectCredentialsDialog credentialDialog,
            IRdpCredentialCallback credentialCallbackService,
            IRepository<ISshSettings> sshSettingsRepository)
        {
            this.window = window.ExpectNotNull(nameof(window));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.workspace = workspace.ExpectNotNull(nameof(workspace));
            this.keyStore = keyStoreAdapter.ExpectNotNull(nameof(keyStoreAdapter));
            this.keyAuthorizer = keyAuthorizer.ExpectNotNull(nameof(keyAuthorizer));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.iapTransportFactory = iapTransportFactory.ExpectNotNull(nameof(iapTransportFactory));
            this.directTransportFactory = directTransportFactory.ExpectNotNull(nameof(directTransportFactory));
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

        private RdpContext CreateRdpContext(
            InstanceLocator instance,
            RdpCredential credential,
            InstanceConnectionSettings settings,
            RdpParameters.ParameterSources sources)
        {
            var context = new RdpContext(
                this.iapTransportFactory,
                this.directTransportFactory,
                instance,
                credential,
                sources);

            context.Parameters.Port = (ushort)settings.RdpPort.IntValue;
            context.Parameters.TransportType = settings.RdpTransport.EnumValue;
            context.Parameters.ConnectionTimeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.IntValue);
            context.Parameters.ConnectionBar = settings.RdpConnectionBar.EnumValue;
            context.Parameters.DesktopSize = settings.RdpDesktopSize.EnumValue;
            context.Parameters.AuthenticationLevel = settings.RdpAuthenticationLevel.EnumValue;
            context.Parameters.ColorDepth = settings.RdpColorDepth.EnumValue;
            context.Parameters.AudioMode = settings.RdpAudioMode.EnumValue;
            context.Parameters.BitmapPersistence = settings.RdpBitmapPersistence.EnumValue;
            context.Parameters.NetworkLevelAuthentication = settings.RdpNetworkLevelAuthentication.EnumValue;
            context.Parameters.UserAuthenticationBehavior = settings.RdpUserAuthenticationBehavior.EnumValue;
            context.Parameters.RedirectClipboard = settings.RdpRedirectClipboard.EnumValue;
            context.Parameters.RedirectPrinter = settings.RdpRedirectPrinter.EnumValue;
            context.Parameters.RedirectSmartCard = settings.RdpRedirectSmartCard.EnumValue;
            context.Parameters.RedirectPort = settings.RdpRedirectPort.EnumValue;
            context.Parameters.RedirectDrive = settings.RdpRedirectDrive.EnumValue;
            context.Parameters.RedirectDevice = settings.RdpRedirectDevice.EnumValue;
            context.Parameters.HookWindowsKeys = settings.RdpHookWindowsKeys.EnumValue;

            return context;
        }

        public async Task<ISessionContext<RdpCredential, RdpParameters>> CreateRdpSessionContextAsync(
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

            return CreateRdpContext(
                node.Instance,
                credential,
                instanceSettings,
                RdpParameters.ParameterSources.Inventory);
        }

        public async Task<ISessionContext<RdpCredential, RdpParameters>> CreateRdpSessionContextAsync(
            IapRdpUrl url,
            CancellationToken cancellationToken)
        {
            url.ExpectNotNull(nameof(url));

            RdpParameters.ParameterSources sources;
            InstanceConnectionSettings settings;
            var existingNode = await this.workspace
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

                sources = RdpParameters.ParameterSources.Inventory
                    | RdpParameters.ParameterSources.Url;
            }
            else
            {
                //
                // We don't have that VM in the inventory, all we have is the URL.
                //
                settings = InstanceConnectionSettings.FromUrl(url);
                sources = RdpParameters.ParameterSources.Url;
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

                return CreateRdpContext(
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

                return CreateRdpContext(
                    url.Instance,
                    CreateRdpCredentialFromSettings(settings),
                    settings,
                    sources);
            }
        }

        public Task<ISessionContext<SshAuthorizedKeyCredential, SshParameters>> CreateSshSessionContextAsync(
            IProjectModelInstanceNode node,
            CancellationToken _)
        {
            node.ExpectNotNull(nameof(node));
            Debug.Assert(node.IsSshSupported());

            var settings = (InstanceConnectionSettings)this.settingsService
                .GetConnectionSettings(node)
                .TypedCollection;

            //
            // Load the SSH key to use.
            //
            var sshSettings = this.sshSettingsRepository.GetSettings();

            IAsymmetricKeySigner signer;
            TimeSpan validity;
            if (sshSettings.UsePersistentKey.BoolValue)
            {
                //
                // Load persistent CNG key. This might pop up dialogs.
                //
                var keyName = new CngKeyName(
                    this.authorization.Session,
                    sshSettings.PublicKeyType.EnumValue,
                    this.keyStore.Provider);

                try
                {
                    signer = AsymmetricKeySigner.Create(
                        this.keyStore.OpenKey(
                            this.window.Handle,
                            keyName.Value,
                            keyName.Type,
                            CngKeyUsages.Signing,
                            false),
                        true);
                    validity = TimeSpan.FromSeconds(sshSettings.PublicKeyValidity.IntValue);
                }
                catch (CryptographicException e) when (
                    e is KeyStoreUnavailableException ||
                    e is InvalidKeyContainerException)
                {
                    throw new SessionException(
                        "Creating or opening the SSH key failed because the " +
                        "Windows CNG key container or key store is inaccessible.\n\n" +
                        "If the problem persists, go to Tools > Options > SSH and disable " +
                        "the option to use a persistent key for SSH authentication.",
                        HelpTopics.TroubleshootingSsh,
                        e);
                }
            }
            else
            {
                //
                // Use an ephemeral key and cap its validity.
                //
                signer = EphemeralKeySigners.Get(sshSettings.PublicKeyType.EnumValue);
                validity = EphemeralKeyValidity;
            }

            Debug.Assert(signer != null);

            //
            // Initialize a context and pass ownership of the key to it.
            //
            var context = new SshContext(
                this.iapTransportFactory,
                this.directTransportFactory,
                this.keyAuthorizer,
                node.Instance,
                signer);

            context.Parameters.Port = (ushort)settings.SshPort.IntValue;
            context.Parameters.TransportType = settings.SshTransport.EnumValue;
            context.Parameters.ConnectionTimeout = TimeSpan.FromSeconds(settings.SshConnectionTimeout.IntValue);
            context.Parameters.PreferredUsername = settings.SshUsername.StringValue;
            context.Parameters.PublicKeyValidity = validity;
            context.Parameters.Language = sshSettings.IsPropagateLocaleEnabled.BoolValue
                ? CultureInfo.CurrentUICulture
                : null;

            return Task.FromResult<ISessionContext<SshAuthorizedKeyCredential, SshParameters>>(context);
        }
    }
}
