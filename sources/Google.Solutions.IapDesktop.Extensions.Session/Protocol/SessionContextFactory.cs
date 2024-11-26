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
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Settings.Collection;
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
        Task<ISessionContext<ISshCredential, SshParameters>> CreateSshSessionContextAsync(
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
        private readonly IKeyStore keyStore;
        private readonly IPlatformCredentialFactory credentialFactory;
        private readonly IConnectionSettingsService settingsService;
        private readonly IRepository<ISshSettings> sshSettingsRepository;
        private readonly IIapTransportFactory iapTransportFactory;
        private readonly IDirectTransportFactory directTransportFactory;
        private readonly IRdpCredentialEditorFactory rdpCredentialEditor;
        private readonly IRdpCredentialCallback rdpCredentialCallbackService;

        public SessionContextFactory(
            IMainWindow window,
            IAuthorization authorization,
            IKeyStore keyStoreAdapter,
            IPlatformCredentialFactory credentialFactory,
            IConnectionSettingsService settingsService,
            IIapTransportFactory iapTransportFactory,
            IDirectTransportFactory directTransportFactory,
            IRdpCredentialEditorFactory rdpCredentialEditor,
            IRdpCredentialCallback credentialCallbackService,
            IRepository<ISshSettings> sshSettingsRepository)
        {
            this.window = window.ExpectNotNull(nameof(window));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.keyStore = keyStoreAdapter.ExpectNotNull(nameof(keyStoreAdapter));
            this.credentialFactory = credentialFactory.ExpectNotNull(nameof(credentialFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.iapTransportFactory = iapTransportFactory.ExpectNotNull(nameof(iapTransportFactory));
            this.directTransportFactory = directTransportFactory.ExpectNotNull(nameof(directTransportFactory));
            this.rdpCredentialEditor = rdpCredentialEditor;
            this.rdpCredentialCallbackService = credentialCallbackService.ExpectNotNull(nameof(credentialCallbackService));
            this.sshSettingsRepository = sshSettingsRepository.ExpectNotNull(nameof(sshSettingsRepository));
        }

        //---------------------------------------------------------------------
        // ISessionContextFactory.
        //---------------------------------------------------------------------

        private static RdpCredential CreateRdpCredentialFromSettings(
            ConnectionSettings settings)
        {
            return new RdpCredential(
                settings.RdpUsername.Value,
                settings.RdpDomain.Value,
                (SecureString?)settings.RdpPassword.Value);
        }

        private RdpContext CreateRdpContext(
            InstanceLocator instance,
            RdpCredential credential,
            ConnectionSettings settings,
            RdpParameters.ParameterSources sources)
        {
            var context = new RdpContext(
                this.iapTransportFactory,
                this.directTransportFactory,
                instance,
                credential,
                sources);

            context.Parameters.Port = (ushort)settings.RdpPort.Value;
            context.Parameters.TransportType = settings.RdpTransport.Value;
            context.Parameters.ConnectionTimeout = TimeSpan.FromSeconds(settings.RdpConnectionTimeout.Value);
            context.Parameters.ConnectionBar = settings.RdpConnectionBar.Value;
            context.Parameters.AuthenticationLevel = settings.RdpAuthenticationLevel.Value;
            context.Parameters.ColorDepth = settings.RdpColorDepth.Value;
            context.Parameters.AudioMode = settings.RdpAudioMode.Value;
            context.Parameters.NetworkLevelAuthentication = settings.RdpNetworkLevelAuthentication.Value;
            context.Parameters.UserAuthenticationBehavior = settings.RdpAutomaticLogon.Value;
            context.Parameters.RedirectClipboard = settings.RdpRedirectClipboard.Value;
            context.Parameters.RedirectPrinter = settings.RdpRedirectPrinter.Value;
            context.Parameters.RedirectSmartCard = settings.RdpRedirectSmartCard.Value;
            context.Parameters.RedirectPort = settings.RdpRedirectPort.Value;
            context.Parameters.RedirectDrive = settings.RdpRedirectDrive.Value;
            context.Parameters.RedirectDevice = settings.RdpRedirectDevice.Value;
            context.Parameters.RedirectWebAuthn = settings.RdpRedirectWebAuthn.Value;
            context.Parameters.HookWindowsKeys = settings.RdpHookWindowsKeys.Value;
            context.Parameters.RestrictedAdminMode = settings.RdpRestrictedAdminMode.Value;
            context.Parameters.SessionType = settings.RdpSessionType.Value;
            context.Parameters.DpiScaling = settings.RdpDpiScaling.Value;
            context.Parameters.DesktopSize = settings.RdpDesktopSize.Value;

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

            var credentialEditor = this.rdpCredentialEditor.Edit(settings.TypedCollection);

            if (flags.HasFlag(RdpCreateSessionFlags.ForcePasswordPrompt))
            {
                //
                // Force a prompt, even though the settings might
                // contain valid credentials.
                //
                credentialEditor.AllowSave = false;
                credentialEditor.PromptForCredentials();
            }
            else
            {
                //
                // Give the user a chance to amend credentials in case
                // the settings don't contain any credentials yet.
                //
                Debug.Assert(credentialEditor.AllowSave);

                await credentialEditor
                    .AmendCredentialsAsync(RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound)
                    .ConfigureAwait(true);

                if (credentialEditor.AllowSave)
                {
                    settings.Save();
                }
            }

            var instanceSettings = (ConnectionSettings)settings.TypedCollection;
            var credential = CreateRdpCredentialFromSettings(instanceSettings);

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

            var sources = RdpParameters.ParameterSources.Url;
            var settings = this.settingsService.GetConnectionSettings(
                url,
                out var foundInInventory);
            if (foundInInventory)
            {
                //
                // This project/VM exists in the inventory, so the settings
                // represent a merge of stored settings and URL-based
                // settings.
                //
                sources |= RdpParameters.ParameterSources.Inventory;
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
                var credentialEditor = this.rdpCredentialEditor.Edit(settings);
                credentialEditor.AllowSave = false;
                await credentialEditor
                    .AmendCredentialsAsync(allowedBehavior)
                    .ConfigureAwait(true);

                return CreateRdpContext(
                    url.Instance,
                    CreateRdpCredentialFromSettings(settings),
                    settings,
                    sources);
            }
        }

        public Task<ISessionContext<ISshCredential, SshParameters>> CreateSshSessionContextAsync(
            IProjectModelInstanceNode node,
            CancellationToken _)
        {
            node.ExpectNotNull(nameof(node));
            Debug.Assert(node.IsSshSupported());

            var sshSettings = this.sshSettingsRepository.GetSettings();
            var settings = this.settingsService
                .GetConnectionSettings(node)
                .TypedCollection;

            SshContext context;
            if (settings.SshPublicKeyAuthentication.Value == SshPublicKeyAuthentication.Enabled)
            {
                //
                // Use an asymmetric key pair for authentication, and
                // authorize it automatically using whichever mechanism
                // is appropriate for the instance.
                //

                IAsymmetricKeySigner signer;
                TimeSpan validity;
                if (sshSettings.UsePersistentKey.Value)
                {
                    //
                    // Load persistent CNG key. This might pop up dialogs.
                    //
                    var keyName = new CngKeyName(
                        this.authorization.Session,
                        sshSettings.PublicKeyType.Value,
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
                        validity = TimeSpan.FromSeconds(sshSettings.PublicKeyValidity.Value);
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
                    signer = EphemeralKeySigners.Get(sshSettings.PublicKeyType.Value);
                    validity = EphemeralKeyValidity;
                }

                Debug.Assert(signer != null);

                //
                // Initialize a context and pass ownership of the key to it.
                //
                context = new SshContext(
                    this.iapTransportFactory,
                    this.directTransportFactory,
                    this.credentialFactory,
                    signer!,
                    node.Instance);

                context.Parameters.PublicKeyValidity = validity;
                context.Parameters.PreferredUsername = settings.SshUsername.Value;
            }
            else
            {
                //
                // Use password for authentication.
                //

                var username = string.IsNullOrEmpty(settings.SshUsername.Value)
                    ? LinuxUser.SuggestUsername(this.authorization)
                    : settings.SshUsername.Value;

                context = new SshContext(
                    this.iapTransportFactory,
                    this.directTransportFactory,
                    new StaticPasswordCredential(
                        username!,
                        ((SecureString?)settings.SshPassword.Value) ?? SecureStringExtensions.Empty),
                    node.Instance);
            }

            context.Parameters.Port = (ushort)settings.SshPort.Value;
            context.Parameters.TransportType = settings.SshTransport.Value;
            context.Parameters.ConnectionTimeout = TimeSpan.FromSeconds(settings.SshConnectionTimeout.Value);
            context.Parameters.Language = sshSettings.IsPropagateLocaleEnabled.Value
                ? CultureInfo.CurrentUICulture
                : null;

            return Task.FromResult<ISessionContext<ISshCredential, SshParameters>>(context);
        }
    }
}
