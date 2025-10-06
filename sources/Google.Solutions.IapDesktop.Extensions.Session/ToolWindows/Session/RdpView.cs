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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Terminal.Controls;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    [Service]
    public class RdpView
        : ClientViewBase<RdpClient>, IRdpSession, IView<RdpViewModel>
    {
        /// <summary>
        /// Hotkey to toggle full-screen.
        /// </summary>
        public const Keys ToggleFullScreenHotKey = Keys.Control | Keys.Alt | Keys.F11;

        private readonly IRepository<IApplicationSettings> settingsRepository;

        private Bound<RdpViewModel> viewModel;

        // For testing only.
        internal event EventHandler? AuthenticationWarningDisplayed;

        private bool IsRdsSessionHostRedirectionError(Exception e)
        {
            try
            {
                //
                // When connecting to an RDSH in non-admin mode, we might
                // be redirected to a different RDSH. This redirect always
                // fails because it's using the internal IP address, not
                // the tunnel address.
                //
                // The control sets the Server property to the redirect
                // address, and we can use that to detect this situation.
                //
                return e is RdpDisconnectedException disconnected &&
                    disconnected.DisconnectReason == 516 && // Unable to establish a connection
                    this.Client!.Server != this.viewModel.Value.Server;
            }
            catch
            {
                return false;
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public RdpView(
            IMainWindow mainWindow,
            IRepository<IApplicationSettings> settingsRepository,
            ToolWindowStateRepository stateRepository,
            IEventQueue eventQueue,
            IExceptionDialog exceptionDialog,
            IBindingContext bindingContext)
            : base(
                  mainWindow,
                  stateRepository,
                  eventQueue,
                  exceptionDialog,
                  bindingContext)
        {
            this.settingsRepository = settingsRepository;
            this.Icon = Resources.ComputerBlue_16;
        }

        public void Bind(
            RdpViewModel viewModel,
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
            get => this.viewModel.TryGet()?.Instance?.Name ?? "Remote Desktop";
            set { }
        }

        protected override void OnFatalError(Exception e)
        {
            if (IsRdsSessionHostRedirectionError(e))
            {
                base.OnFatalError(new RdsRedirectException(
                    "The server initiated a redirect to a different " +
                    "server. IAP Desktop does not support redirects.\n\n" +
                    "To connect to a RD Session Host, change your connection settings " +
                    "to use an 'Admin' session.",
                    e));
            }
            else
            {
                base.OnFatalError(e);
            }
        }

        protected override void ConnectCore()
        {
            var viewModel = this.viewModel.Value;

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                viewModel.Server,
                viewModel.Port,
                viewModel.Parameters!.ConnectionTimeout))
            {
                this.Client!.MainWindow = (Form)this.MainWindow;
                this.Client.ServerAuthenticationWarningDisplayed += (_, args)
                    => this.AuthenticationWarningDisplayed?.Invoke(this, args);

                //
                // Basic connection settings.
                //
                this.Client.Server = viewModel.Server;
                this.Client.Domain = viewModel.Credential!.Domain;
                this.Client.Username = viewModel.Credential.User;
                this.Client.ServerPort = viewModel.Port!.Value;
                this.Client.ConnectionTimeout = viewModel.Parameters.ConnectionTimeout;
                this.Client.EnableAdminMode = viewModel.Parameters.SessionType == RdpSessionType.Admin;

                //
                // Connection security settings.
                //
                switch (viewModel.Parameters.AuthenticationLevel)
                {
                    case RdpAuthenticationLevel.NoServerAuthentication:
                        this.Client.ServerAuthenticationLevel = 0;
                        break;

                    case RdpAuthenticationLevel.RequireServerAuthentication:
                        this.Client.ServerAuthenticationLevel = 1;
                        break;

                    case RdpAuthenticationLevel.AttemptServerAuthentication:
                        this.Client.ServerAuthenticationLevel = 2;
                        break;
                }

                switch (viewModel.Parameters.UserAuthenticationBehavior)
                {
                    case RdpAutomaticLogon.Enabled:
                        //
                        // Use stored credentials, but allow prompting in case
                        // they're incomplete or wrong.
                        //
                        this.Client.EnableCredentialPrompt = true;
                        this.Client.Password = 
                            viewModel.Credential.Password?.ToClearText() ?? string.Empty;
                        break;

                    case RdpAutomaticLogon.Disabled:
                        //
                        // Allow (and expect) a prompt.
                        //
                        // We "shouldn't" have a stored password -- but we might 
                        // have one anyway:
                        //
                        // - Automatic logons might be auto-disabled by group policy,
                        //   but the user might have stored a credential before that 
                        //   group policy took effect (or before IAP Desktop started 
                        //   considering that group policy).
                        // - Automatic logons might be auto-disabled by group policy 
                        //   (causing prompts to be suppressed), but the user might 
                        //   have stored credentials manually.
                        //
                        // So if there is a password, use it.
                        //
                        this.Client.EnableCredentialPrompt = true;
                        this.Client.Password = 
                            viewModel.Credential.Password?.ToClearText() ?? string.Empty;
                        break;

                    case RdpAutomaticLogon.LegacyAbortOnFailure:
                        this.Client.EnableCredentialPrompt = false;
                        this.Client.Password = 
                            viewModel.Credential.Password?.ToClearText() ?? string.Empty;
                        break;
                }

                this.Client.EnableNetworkLevelAuthentication =
                    viewModel.Parameters.NetworkLevelAuthentication != RdpNetworkLevelAuthentication.Disabled;
                this.Client.EnableRestrictedAdminMode =
                    viewModel.Parameters.RestrictedAdminMode == RdpRestrictedAdminMode.Enabled;

                //
                // Connection bar settings.
                //
                this.Client.EnableConnectionBar =
                    viewModel.Parameters.ConnectionBar != RdpConnectionBarState.Off;
                this.Client.EnableConnectionBarMinimizeButton = true;
                this.Client.EnableConnectionBarPin =
                    viewModel.Parameters.ConnectionBar == RdpConnectionBarState.Pinned;
                this.Client.ConnectionBarText = this.Instance.Name;

                //
                // Local resources settings.
                //
                this.Client.EnableClipboardRedirection =
                    viewModel.Parameters.RedirectClipboard == RdpRedirectClipboard.Enabled;
                this.Client.EnablePrinterRedirection =
                    viewModel.Parameters.RedirectPrinter == RdpRedirectPrinter.Enabled;
                this.Client.EnableSmartCardRedirection =
                    viewModel.Parameters.RedirectSmartCard == RdpRedirectSmartCard.Enabled;
                this.Client.EnablePortRedirection =
                    viewModel.Parameters.RedirectPort == RdpRedirectPort.Enabled;
                this.Client.EnableDriveRedirection =
                    viewModel.Parameters.RedirectDrive == RdpRedirectDrive.Enabled;
                this.Client.EnableDeviceRedirection =
                    viewModel.Parameters.RedirectDevice == RdpRedirectDevice.Enabled;
                this.Client.EnableAudioCaptureRedirection = 
                    viewModel.Parameters.AudioInput == RdpAudioInput.Enabled;

                switch (viewModel.Parameters.AudioPlayback)
                {
                    case RdpAudioPlayback.PlayLocally:
                        this.Client.AudioRedirectionMode = 0;
                        break;
                    case RdpAudioPlayback.PlayOnServer:
                        this.Client.AudioRedirectionMode = 1;
                        break;
                    case RdpAudioPlayback.DoNotPlay:
                        this.Client.AudioRedirectionMode = 2;
                        break;
                }

                //
                // Display settings.
                //
                this.Client.EnableDpiScaling =
                    viewModel.Parameters.DpiScaling == RdpDpiScaling.Enabled;
                this.Client.EnableAutoResize =
                    viewModel.Parameters.DesktopSize == RdpDesktopSize.AutoAdjust;

                switch (viewModel.Parameters.ColorDepth)
                {
                    case RdpColorDepth.HighColor:
                        this.Client.ColorDepth = 16;
                        break;
                    case RdpColorDepth.TrueColor:
                        this.Client.ColorDepth = 24;
                        break;
                    case RdpColorDepth.DeepColor:
                        this.Client.ColorDepth = 32;
                        break;
                }

                //
                // Keyboard settings.
                //
                this.Client.KeyboardHookMode =
                    (int)viewModel.Parameters.HookWindowsKeys;

                //
                // Set hotkey to trigger OnFocusReleasedEvent. This should be
                // the same as the main window uses to move the focus to the
                // control.
                //
                this.Client.FocusHotKey = ToggleFocusHotKey;
                this.Client.FullScreenHotKey = ToggleFullScreenHotKey;

                this.Client.EnableWebAuthnRedirection =
                    viewModel.Parameters.RedirectWebAuthn == RdpRedirectWebAuthn.Enabled;

                //
                // Start establishing a connection and react to events.
                //
                this.Client.Connect();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (this.Client != null && this.Client.IsFullScreen)
            {
                //
                // Ignore, any attempted size change might
                // just screw up full-screen mode.
                //
                return;
            }

            base.OnSizeChanged(e);
        }

        //---------------------------------------------------------------------
        // IRdpSession.
        //---------------------------------------------------------------------

        public bool CanEnterFullScreen
        {
            get => this.Client != null && this.Client.CanEnterFullScreen;
        }

        public bool TrySetFullscreen(FullScreenMode mode)
        {
            this.Client.ExpectNotNull("Client connected");

            Rectangle? customBounds;
            if (mode == FullScreenMode.SingleScreen)
            {
                //
                // Normal full screen.
                //
                customBounds = null;
            }
            else
            {
                //
                // Use all configured screens.
                //
                // NB. The list of devices might include devices that
                // do not exist anymore. 
                //
                var selectedDevices = (this.settingsRepository.GetSettings()
                    .FullScreenDevices.Value ?? string.Empty)
                        .Split(ApplicationSettingsRepository.FullScreenDevicesSeparator)
                        .ToHashSet();

                var screens = Screen.AllScreens
                    .Where(s => selectedDevices.Contains(s.DeviceName));

                if (!screens.Any())
                {
                    //
                    // Default to all screens.
                    //
                    screens = Screen.AllScreens;
                }

                var r = new Rectangle();
                foreach (var s in screens)
                {
                    r = Rectangle.Union(r, s.Bounds);
                }

                customBounds = r;
            }

            return this.Client!.TryEnterFullScreen(customBounds);
        }

        public void ShowSecurityScreen()
        {
            this.Client
                .ExpectNotNull("Client connected")
                .ShowSecurityScreen();
        }

        public void ShowTaskManager()
        {
            this.Client
                .ExpectNotNull("Client connected")
                .ShowTaskManager();
        }

        public void Logoff()
        {
            this.Client
                .ExpectNotNull("Client connected")
                .Logoff();
        }

        public void Reconnect()
        {
            this.Client
                .ExpectNotNull("Client connected")
                .Reconnect();
        }

        public void SendText(string text)
        {
            this.Client
                .ExpectNotNull("Client connected")
                .SendText(text);
        }

        public bool CanTransferFiles
        {
            get => this.viewModel
                .Value
                .Parameters!
                .RedirectClipboard == RdpRedirectClipboard.Enabled;
        }

        public Task TransferFilesAsync()
        {
            ShowTooltip(
                "Copy and paste files here",
                "Use copy and paste to transfer files between " +
                "your local computer and the VM.");

            return Task.CompletedTask;
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class RdsRedirectException : RdpException
        {
            public RdsRedirectException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}
