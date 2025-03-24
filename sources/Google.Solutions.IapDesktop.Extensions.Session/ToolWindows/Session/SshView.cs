//
// Copyright 2024 Google LLC
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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Ssh.Native;
using Google.Solutions.Terminal.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    [Service]
    public class SshView
        : ClientViewBase<SshHybridClient>, ISshSession, IView<SshViewModel>
    {
        private Bound<SshViewModel> viewModel;
        private readonly ITerminalSettingsRepository settingsRepository;
        private readonly IInputDialog inputDialog;

        private void ApplyTerminalSettings(ITerminalSettings settings)
        {
            if (this.Client == null)
            {
                return;
            }

            var terminal = this.Client.Terminal;
            terminal.EnableCtrlC = settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;
            terminal.EnableCtrlV = settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;

            terminal.EnableCtrlInsert = settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;
            terminal.EnableShiftInsert = settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;
            terminal.EnableTypographicQuoteConversion = settings.IsQuoteConvertionOnPasteEnabled.Value;
            terminal.EnableBracketedPaste = settings.IsBracketedPasteEnabled.Value;
            terminal.EnableCtrlHomeEnd = settings.IsScrollingUsingCtrlHomeEndEnabled.Value;
            terminal.Caret = settings.CaretStyle.Value;

            terminal.Font = new Font(
                settings.FontFamily.Value,
                TerminalSettings.FontSizeFromDword(settings.FontSizeAsDword.Value));

            terminal.BackColor = Color.FromArgb(settings.BackgroundColorArgb.Value);
            terminal.ForeColor = Color.FromArgb(settings.ForegroundColorArgb.Value);
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshView(
            IMainWindow mainWindow,
            ITerminalSettingsRepository settingsRepository,
            ToolWindowStateRepository stateRepository,
            IEventQueue eventQueue,
            IExceptionDialog exceptionDialog,
            IInputDialog inputDialog,
            IBindingContext bindingContext)
            : base(
                  mainWindow,
                  stateRepository,
                  eventQueue,
                  exceptionDialog,
                  bindingContext)
        {
            this.Icon = Resources.ConsoleBlue_16;
            this.settingsRepository = settingsRepository;
            this.inputDialog = inputDialog;

            this.settingsRepository.SettingsChanged += (_, args) =>
                ApplyTerminalSettings(args.Data);
        }

        public void Bind(
            SshViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel.Value = viewModel;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override InstanceLocator Instance
        {
            get => this.viewModel.Value.Instance!;
        }

        public override string Text
        {
            get => this.viewModel.TryGet()?.Instance?.Name ?? "SSH";
            set { }
        }

        protected override void ConnectCore()
        {
            var viewModel = this.viewModel.Value;
            var client = this.Client.ExpectNotNull(nameof(this.Client));

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                viewModel.Endpoint,
                viewModel.Parameters!.ConnectionTimeout))
            {
                //
                // Identify as IAP-Desktop, not plain libssh2.
                // Replace dashes as those aren't allowed in SSH
                // banners.
                //
                client.Banner = Install.UserAgent
                    .ToApplicationName()
                    .Replace("-", string.Empty);

                //
                // Basic connection settings.
                //
                client.ServerEndpoint = viewModel.Endpoint;
                client.Credential = viewModel.Credential;
                client.ConnectionTimeout = viewModel.Parameters.ConnectionTimeout;
                client.Locale = viewModel.Parameters.Language;
                client.EnableFileBrowser = viewModel.Parameters.EnableFileAccess;
                client.KeyboardInteractiveHandler = new SshKeyboardInteractiveHandler(
                    this,
                    this.inputDialog,
                    viewModel.Instance!);

                client.Terminal.Focus();

                //
                // Apply terminal settings. These can change at any
                // time.
                //
                ApplyTerminalSettings(this.settingsRepository.GetSettings());

                client.FileBrowsingFailed += (_, args)
                    => OnError("Unable to complete file operation", args.Exception);

                //
                // Start establishing a connection and react to events.
                //
                client.Connect();
            }
        }

        protected override void OnFatalError(Exception e)
        {
            //
            // Translate common exceptions to make them more actionable.
            //
            if (e.Unwrap() is Libssh2Exception unverifiedEx &&
                unverifiedEx.ErrorCode == LIBSSH2_ERROR.PUBLICKEY_UNVERIFIED &&
                this.viewModel.Value.Credential is PlatformCredential unverifiedPlatformCredential &&
                unverifiedPlatformCredential.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
            {
                OnFatalError(new OsLoginAuthenticationFailedException(
                    "Authenticating to the VM failed. Possible reasons for this " +
                    "error include:\n\n" +
                    " - The VM is configured to require 2-step verification,\n" +
                    "   and you haven't set up 2SV for your user account\n" +
                    " - The guest environment is misconfigured or not running",
                    e,
                    HelpTopics.TroubleshootingOsLogin));
            }
            else if (e.Unwrap() is Libssh2Exception authEx &&
                authEx.ErrorCode == LIBSSH2_ERROR.AUTHENTICATION_FAILED &&
                this.viewModel.Value.Credential is PlatformCredential failedPlatformCredential)
            {
                if (failedPlatformCredential.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
                {
                    var outdatedMessage = failedPlatformCredential.Signer is OsLoginCertificateSigner
                        ? " - The VM is running an outdated version of the guest environment \n" +
                          "   that doesn't support certificate-based authentication\n"
                        : string.Empty;

                    var message =
                        "Authenticating to the VM failed. Possible reasons for this " +
                        "error include:\n\n" +
                        " - You don't have sufficient access to log in\n" +
                        outdatedMessage +
                        " - The VM's guest environment is misconfigured or not running\n\n" +
                        "To log in, you need all of the following roles:\n\n" +
                        " 1. 'Compute OS Login' or 'Compute OS Admin Login'\n" +
                        " 2. 'Service Account User' (if the VM uses a service account)\n" +
                        " 3. 'Compute OS Login External User'\n" +
                        "    (if the VM belongs to a different GCP organization)\n\n" +
                        "Note that it might take several minutes for IAM policy changes to take effect.";

                    OnFatalError(new OsLoginAuthenticationFailedException(
                        message,
                        e,
                        HelpTopics.GrantingOsLoginRoles));
                }
                else
                {
                    OnFatalError(new MetadataKeyAuthenticationFailedException(
                        "Authentication failed. Verify that the Compute Engine guest environment " +
                        "is installed on the VM and that the agent is running.",
                        e,
                        HelpTopics.ManagingMetadataAuthorizedKeys));
                }
            }
            else if (e.Unwrap() is Libssh2Exception kexEx &&
                kexEx.ErrorCode == LIBSSH2_ERROR.KEY_EXCHANGE_FAILURE &&
                Environment.OSVersion.Version.Build <= 10000)
            {
                //
                // Libssh2's CNG support requires Windows 10+.
                //
                OnFatalError(new PlatformNotSupportedException(
                    "SSH is not supported on this version of Windows"));
            }
            else
            {
                base.OnFatalError(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyData == ToggleFocusHotKey)
            {
                //
                // Release focus and move it to the panel, which ensures
                // that any other shortcuts start applying again.
                //
                this.MainWindow.MainPanel.Focus();
                e.Handled = true;
            }
        }

        public bool CanTransferFiles
        {
            get =>
                this.Client != null &&
                this.Client.CanShowFileBrowser;
        }

        //---------------------------------------------------------------------
        // ISshSession.
        //---------------------------------------------------------------------

        public Task TransferFilesAsync()
        {
            Debug.Assert(this.CanTransferFiles);
            this.Client?.BrowseFiles();
            return Task.CompletedTask;
        }

        //---------------------------------------------------------------------
        // Exceptions.
        //---------------------------------------------------------------------

        public class OsLoginAuthenticationFailedException : Exception, IExceptionWithHelpTopic
        {
            public IHelpTopic Help { get; }

            internal OsLoginAuthenticationFailedException(
                string message,
                Exception inner,
                IHelpTopic helpTopic)
                : base(message, inner)
            {
                this.Help = helpTopic;
            }
        }

        public class MetadataKeyAuthenticationFailedException : Exception, IExceptionWithHelpTopic
        {
            public IHelpTopic Help { get; }

            internal MetadataKeyAuthenticationFailedException(
                string message,
                Exception inner,
                IHelpTopic helpTopic)
                : base(message, inner)
            {
                this.Help = helpTopic;
            }
        }
    }
}
