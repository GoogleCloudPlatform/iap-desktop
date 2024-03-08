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
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Settings;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp
{
    [Service]
    public partial class RdpView
        : SessionViewBase, IRdpSession, IView<RdpViewModel>
    {
        /// <summary>
        /// Hotkey to toggle full-screen.
        /// </summary>
        public const Keys ToggleFullScreenHotKey = Keys.Control | Keys.Alt | Keys.F11;

        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventQueue eventService;
        private readonly IControlTheme theme;
        private readonly IRepository<IApplicationSettings> settingsRepository;

        private RdpViewModel viewModel;

        // For testing only.
        internal event EventHandler AuthenticationWarningDisplayed;

        public bool IsClosing { get; private set; } = false;

        internal enum LayoutMode
        {
            Normal,
            Reconnect,
            Wait
        }

        internal LayoutMode Mode { get; private set; }

        private void UpdateLayout(LayoutMode mode)
        {
            if (this.rdpClient == null)
            {
                return;
            }

            //
            // NB. Docking does not work reliably with the OCX, so keep the size
            // in sync programmatically.
            //
            this.rdpClient.Size = this.Size;

            //
            // Resize the overlay pane to cover the OCX.
            //
            this.overlayPanel.Location = Point.Empty;
            this.overlayPanel.Size = this.Size;

            //
            // Center the other panels.
            //
            this.waitPanel.Location = new Point(
                (this.Size.Width - this.waitPanel.Width) / 2,
                (this.Size.Height - this.waitPanel.Height) / 2);

            this.reconnectPanel.Location = new Point(
                (this.Size.Width - this.reconnectPanel.Width) / 2,
                (this.Size.Height - this.reconnectPanel.Height) / 2);

            this.overlayPanel.Visible = mode == LayoutMode.Reconnect || mode == LayoutMode.Wait;
            this.waitPanel.Visible = mode == LayoutMode.Wait;
            this.reconnectPanel.Visible = mode == LayoutMode.Reconnect;

            this.Mode = mode;
        }

        private async Task ShowErrorAndClose(string caption, Exception e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e.Message))
            {
                await this.eventService
                    .PublishAsync(new SessionAbortedEvent(this.Instance, e))
                    .ConfigureAwait(true);

                this.exceptionDialog.Show(this, caption, e);

                Close();
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public RdpView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.eventService = serviceProvider.GetService<IEventQueue>();
            this.theme = serviceProvider.GetService<IThemeService>().ToolWindowTheme;
            this.settingsRepository = serviceProvider.GetService<IRepository<IApplicationSettings>>();
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public InstanceLocator Instance => this.viewModel?.Instance;

        public override string Text
        {
            get => this.viewModel?.Instance?.Name ?? "Remote Desktop";
            set { }
        }

        public void Bind(
            RdpViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel = viewModel;
        }

        public void Connect()
        {
            Debug.Assert(this.rdpClient == null, "Not initialized yet");
            Debug.Assert(this.viewModel != null, "View bound");

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                this.viewModel.Server,
                this.viewModel.Port,
                this.viewModel.Parameters.ConnectionTimeout))
            {
                //
                // NB. The initialization needs to happen after the pane is shown, otherwise
                // an error happens indicating that the control does not have a Window handle.
                //
                InitializeComponent();
                Debug.Assert(this.rdpClient != null);

                //
                // Because we're not initializing controls in the constructor, the
                // theme isn't applied by default.
                //
                Debug.Assert(this.theme != null || Install.IsExecutingTests);

                SuspendLayout();
                this.theme?.ApplyTo(this);
                UpdateLayout(LayoutMode.Wait);
                ResumeLayout();

                this.rdpClient.MainWindow = (Form)this.MainWindow;

                //
                // Basic connection settings.
                //
                this.rdpClient.Server = this.viewModel.Server;
                this.rdpClient.Domain = this.viewModel.Credential.Domain;
                this.rdpClient.Username = this.viewModel.Credential.User;
                this.rdpClient.ServerPort = this.viewModel.Port;
                this.rdpClient.Password = this.viewModel.Credential.Password?.AsClearText() ?? string.Empty;
                this.rdpClient.ConnectionTimeout = this.viewModel.Parameters.ConnectionTimeout;

                //
                // Connection security settings.
                //
                switch (this.viewModel.Parameters.AuthenticationLevel)
                {
                    case RdpAuthenticationLevel.NoServerAuthentication:
                        this.rdpClient.ServerAuthenticationLevel = 0;
                        break;

                    case RdpAuthenticationLevel.RequireServerAuthentication:
                        this.rdpClient.ServerAuthenticationLevel = 1;
                        break;

                    case RdpAuthenticationLevel.AttemptServerAuthentication:
                        this.rdpClient.ServerAuthenticationLevel = 2;
                        break;
                }

                this.rdpClient.EnableCredentialPrompt =
                    (this.viewModel.Parameters.UserAuthenticationBehavior == RdpUserAuthenticationBehavior.PromptOnFailure);
                this.rdpClient.EnableNetworkLevelAuthentication =
                    (this.viewModel.Parameters.NetworkLevelAuthentication != RdpNetworkLevelAuthentication.Disabled);
                this.rdpClient.EnableRestrictedAdminMode =
                    (this.viewModel.Parameters.RestrictedAdminMode == RdpRestrictedAdminMode.Enabled);

                //
                // Connection bar settings.
                //
                this.rdpClient.EnableConnectionBar =
                    (this.viewModel.Parameters.ConnectionBar != RdpConnectionBarState.Off);
                this.rdpClient.EnableConnectionBarMinimizeButton = true;
                this.rdpClient.EnableConnectionBarPin =
                    (this.viewModel.Parameters.ConnectionBar == RdpConnectionBarState.Pinned);
                this.rdpClient.ConnectionBarText = this.Instance.Name;

                //
                // Local resources settings.
                //
                this.rdpClient.EnableClipboardRedirection =
                    this.viewModel.Parameters.RedirectClipboard == RdpRedirectClipboard.Enabled;
                this.rdpClient.EnablePrinterRedirection =
                    this.viewModel.Parameters.RedirectPrinter == RdpRedirectPrinter.Enabled;
                this.rdpClient.EnableSmartCardRedirection =
                    this.viewModel.Parameters.RedirectSmartCard == RdpRedirectSmartCard.Enabled;
                this.rdpClient.EnablePortRedirection =
                    this.viewModel.Parameters.RedirectPort == RdpRedirectPort.Enabled;
                this.rdpClient.EnableDriveRedirection =
                    this.viewModel.Parameters.RedirectDrive == RdpRedirectDrive.Enabled;
                this.rdpClient.EnableDeviceRedirection =
                    this.viewModel.Parameters.RedirectDevice == RdpRedirectDevice.Enabled;

                switch (this.viewModel.Parameters.AudioMode)
                {
                    case RdpAudioMode.PlayLocally:
                        this.rdpClient.AudioRedirectionMode = 0;
                        break;
                    case RdpAudioMode.PlayOnServer:
                        this.rdpClient.AudioRedirectionMode = 1;
                        break;
                    case RdpAudioMode.DoNotPlay:
                        this.rdpClient.AudioRedirectionMode = 2;
                        break;
                }

                //
                // Display settings.
                //
                switch (this.viewModel.Parameters.ColorDepth)
                {
                    case RdpColorDepth.HighColor:
                        this.rdpClient.ColorDepth = 16;
                        break;
                    case RdpColorDepth.TrueColor:
                        this.rdpClient.ColorDepth = 24;
                        break;
                    case RdpColorDepth.DeepColor:
                        this.rdpClient.ColorDepth = 32;
                        break;
                }

                //
                // Keyboard settings.
                //
                this.rdpClient.KeyboardHookMode =
                    (int)this.viewModel.Parameters.HookWindowsKeys;


                //
                // Set hotkey to trigger OnFocusReleasedEvent. This should be
                // the same as the main window uses to move the focus to the
                // control.
                //
                this.rdpClient.FocusHotKey = ToggleFocusHotKey;
                this.rdpClient.FullScreenHotKey = ToggleFullScreenHotKey;

                this.rdpClient.EnableWebAuthnRedirection =
                    (this.viewModel.Parameters.RedirectWebAuthn == RdpRedirectWebAuthn.Enabled);

                this.rdpClient.Connect();
            }
        }

        private void Reconnect()
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                UpdateLayout(LayoutMode.Wait);

                this.rdpClient.Connect();
            }
        }

        public bool IsConnected
        {
            get =>
                this.rdpClient.State == RdpClient.ConnectionState.Connected ||
                this.rdpClient.State == RdpClient.ConnectionState.LoggedOn;
        }

        public bool CanEnterFullScreen => this.rdpClient.CanEnterFullScreen;

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        protected override void OnSizeChanged(EventArgs e)
        {
            if (this.rdpClient != null && this.rdpClient.IsFullScreen)
            {
                //
                // Ignore, any attempted size change might
                // just screw up full-screen mode.
                //
                return;
            }

            base.OnSizeChanged(e);

            //
            // Rearrange controls based on new size.
            //
            UpdateLayout(this.Mode);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            //
            // Mark this pane as being in closing state even though it is still
            // visible at this point. The flag ensures that this pane is
            // not considered by TryGetExistingPane anymore.
            //
            this.IsClosing = true;

            this.eventService
                .PublishAsync(new SessionEndedEvent(this.Instance))
                .ContinueWith(_ => { });
        }

        private async void reconnectButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                try
                {
                    //
                    // Occasionally, reconnecting fails with a non-descriptive
                    // E_FAIL error. There isn't much to do about it, so treat
                    // it as fatal error and close the window.
                    //
                    Reconnect();
                }
                catch (Exception ex)
                {
                    await ShowErrorAndClose("Failed to reconnect", ex)
                        .ConfigureAwait(true);
                }
            }
        }

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private void rdpClient_ConnectionClosed(object sender, RdpClient.ConnectionClosedEventArgs e)
        {
            switch (e.Reason)
            {
                case RdpClient.DisconnectReason.FormClosed:
                    //
                    // User closed the form.
                    //
                    break;

                case RdpClient.DisconnectReason.DisconnectedByUser:
                    //
                    // User-initiated signout.
                    //
                    Close();
                    break;

                default:
                    //
                    // Something else - allow user to reconnect.
                    //
                    UpdateLayout(LayoutMode.Reconnect);
                    break;
            }
        }

        private async void rdpClient_ConnectionFailed(object _, ExceptionEventArgs e)
        {
            await ShowErrorAndClose(
                    "Connect Remote Desktop session failed",
                    e.Exception)
                .ConfigureAwait(true);
        }

        private void rdpClient_StateChanged(object _, System.EventArgs e)
        {
            if (this.rdpClient.State == RdpClient.ConnectionState.Connected)
            {
                this.eventService
                    .PublishAsync(new SessionStartedEvent(this.Instance))
                    .ContinueWith(_ => { });
            }

            if (this.rdpClient.State == RdpClient.ConnectionState.Connected ||
                this.rdpClient.State == RdpClient.ConnectionState.LoggedOn)
            {
                UpdateLayout(LayoutMode.Normal);
            }
        }

        private void rdpClient_ServerAuthenticationWarningDisplayed(object _, System.EventArgs e)
        {
            this.AuthenticationWarningDisplayed?.Invoke(this, e);
        }

        //---------------------------------------------------------------------
        // IRemoteDesktopSession.
        //---------------------------------------------------------------------

        public bool TrySetFullscreen(FullScreenMode mode)
        {
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
                // Use all configured screns.
                //
                // NB. The list of devices might include devices that
                // do not exist anymore. 
                //
                var selectedDevices = (this.settingsRepository.GetSettings()
                    .FullScreenDevices.StringValue ?? string.Empty)
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

            return this.rdpClient.TryEnterFullScreen(customBounds);
        }

        public void ShowSecurityScreen()
        {
            this.rdpClient.ShowSecurityScreen();
        }

        public void ShowTaskManager()
        {
            this.rdpClient.ShowTaskManager();
        }

        public void SendKeys(params Keys[] keys)
        {
            this.rdpClient.SendKeys(keys);
        }

        public bool CanTransferFiles
        {
            get => (this.viewModel.Parameters.RedirectClipboard == RdpRedirectClipboard.Enabled);
        }

        public Task DownloadFilesAsync()
        {
            ShowTooltip(
                "Copy and paste files here",
                "Use copy and paste to transfer files between " +
                "your local computer and the VM.");

            return Task.CompletedTask;
        }

        public Task UploadFilesAsync()
        {
            ShowTooltip(
                "Paste files to upload",
                "Use copy and paste to transfer files between " +
                "your local computer and the VM.");

            return Task.CompletedTask;
        }

        //---------------------------------------------------------------------
        // Drag/docking.
        //
        // The RDP control must always have a parent. But when a document is
        // dragged to become a floating window, or when a window is re-docked,
        // then its parent is temporarily set to null.
        // 
        // To "rescue" the RDP control in these situations, we temporarily
        // move the the control to a rescue form when the drag begins, and
        // restore it when it ends.
        //---------------------------------------------------------------------

        private Form rescueWindow = null;

        protected override Size DefaultFloatWindowClientSize => this.Size;

        protected override void OnDockBegin()
        {
            //
            // NB. It's possible that another rescue operation is still in
            // progress. So don't create a window if there is one already.
            //
            if (this.rescueWindow == null && this.rdpClient != null)
            {
                this.rescueWindow = new Form();
                this.rdpClient.Parent = this.rescueWindow;
            }

            base.OnDockBegin();
        }

        protected override void OnDockEnd()
        {
            if (this.rescueWindow != null && this.rdpClient != null)
            {
                this.rdpClient.Parent = this;
                this.rdpClient.Size = this.Size;
                this.rescueWindow.Close();
                this.rescueWindow = null;
            }

            base.OnDockEnd();
        }
    }
}
