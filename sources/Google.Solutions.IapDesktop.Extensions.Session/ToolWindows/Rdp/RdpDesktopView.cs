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

using AxMSTSCLib;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using MSTSCLib;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1031 // catch Exception
#pragma warning disable CA1801 // Review unused parameters

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp
{
    [Service]
    public partial class RdpDesktopView
        : SessionViewBase, IRdpSession, IView<RdpViewModel>
    {
        private const string WebAuthnPlugin = "webauthn.dll";

        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventQueue eventService;
        private readonly IControlTheme theme;

        private RdpViewModel viewModel;
        private bool useAllScreensForFullScreen = false;

        private int keysSent = 0;
        private bool autoResize = false;
        private bool connecting = false;

        // Track the (client area) size of the remote connection.
        private Size currentConnectionSize;
        private Size initialConnectionSize;

        // For testing only.
        internal event EventHandler AuthenticationWarningDisplayed;

        public bool IsFormClosing { get; private set; } = false;

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
                // Make sure we're not fullscreen anymore.
                LeaveFullScreen();

                await this.eventService.PublishAsync(
                    new SessionAbortedEvent(this.Instance, e))
                    .ConfigureAwait(true);
                this.exceptionDialog.Show(this, caption, e);
                Close();
            }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static RdpDesktopView TryGetExistingPane(
            IMainWindow mainForm,
            InstanceLocator vmInstance)
        {
            return mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<RdpDesktopView>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .FirstOrDefault();
        }

        public static RdpDesktopView TryGetActivePane(
            IMainWindow mainForm)
        {
            //
            // NB. The active content might be in a float window.
            //
            return mainForm.MainPanel.ActivePane?.ActiveContent as RdpDesktopView;
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public RdpDesktopView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.eventService = serviceProvider.GetService<IEventQueue>();
            this.theme = serviceProvider.GetService<IThemeService>().ToolWindowTheme;
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

                var advancedSettings = this.rdpClient.AdvancedSettings7;
                var nonScriptable = (IMsRdpClientNonScriptable5)this.rdpClient.GetOcx();
                var securedSettings2 = this.rdpClient.SecuredSettings2;

                //
                // Basic connection settings.
                //
                this.rdpClient.Server = this.viewModel.Server;
                this.rdpClient.Domain = this.viewModel.Credential.Domain;
                this.rdpClient.UserName = this.viewModel.Credential.User;
                advancedSettings.RDPPort = this.viewModel.Port;
                advancedSettings.ClearTextPassword =
                    this.viewModel.Credential.Password?.AsClearText() ?? string.Empty;
                nonScriptable.AllowCredentialSaving = false;

                //
                // Connection security settings.
                //
                nonScriptable.PromptForCredentials = false;
                nonScriptable.NegotiateSecurityLayer = true;

                switch (this.viewModel.Parameters.AuthenticationLevel)
                {
                    case RdpAuthenticationLevel.NoServerAuthentication:
                        advancedSettings.AuthenticationLevel = 0;
                        break;

                    case RdpAuthenticationLevel.RequireServerAuthentication:
                        advancedSettings.AuthenticationLevel = 1;
                        break;

                    case RdpAuthenticationLevel.AttemptServerAuthentication:
                        advancedSettings.AuthenticationLevel = 2;
                        break;
                }

                nonScriptable.AllowPromptingForCredentials =
                    this.viewModel.Parameters.UserAuthenticationBehavior == RdpUserAuthenticationBehavior.PromptOnFailure;

                //
                // Advanced connection settings.
                //
                advancedSettings.keepAliveInterval = 60000;
                advancedSettings.PerformanceFlags = 0; // Enable all features, it's 2020.
                advancedSettings.EnableAutoReconnect = true;
                advancedSettings.MaxReconnectAttempts = 10;

                if (this.viewModel.Parameters.NetworkLevelAuthentication != RdpNetworkLevelAuthentication.Disabled)
                {
                    //
                    // Use NLA.
                    //
                    advancedSettings.EnableCredSspSupport = true;
                }
                else
                {
                    //
                    // Disable NLA. This only works if server authentication is enabled.
                    //
                    advancedSettings.EnableCredSspSupport = false;
                    advancedSettings.AuthenticationLevel = 2;
                }

                //
                // Apply timeouts. Note that the control might take
                // about twice the configured timeout before sending a 
                // OnDisconnected event.
                //
                advancedSettings.singleConnectionTimeout = (int)this.viewModel.Parameters.ConnectionTimeout.TotalSeconds;
                advancedSettings.overallConnectionTimeout = (int)this.viewModel.Parameters.ConnectionTimeout.TotalSeconds;

                //
                // Behavior settings.
                //
                advancedSettings.DisplayConnectionBar =
                    (this.viewModel.Parameters.ConnectionBar != RdpConnectionBarState.Off);
                advancedSettings.ConnectionBarShowMinimizeButton = true;
                advancedSettings.PinConnectionBar =
                    (this.viewModel.Parameters.ConnectionBar == RdpConnectionBarState.Pinned);
                nonScriptable.ConnectionBarText = this.Instance.Name;
                advancedSettings.EnableWindowsKey = 1;

                //
                // Trigger OnRequestGoFullScreen event.
                //
                advancedSettings.ContainerHandledFullScreen = 1;

                //
                // Local resources settings.
                //
                advancedSettings.RedirectClipboard =
                    this.viewModel.Parameters.RedirectClipboard == RdpRedirectClipboard.Enabled;
                advancedSettings.RedirectPrinters =
                    this.viewModel.Parameters.RedirectPrinter == RdpRedirectPrinter.Enabled;
                advancedSettings.RedirectSmartCards =
                    this.viewModel.Parameters.RedirectSmartCard == RdpRedirectSmartCard.Enabled;
                advancedSettings.RedirectPorts =
                    this.viewModel.Parameters.RedirectPort == RdpRedirectPort.Enabled;
                advancedSettings.RedirectDrives =
                    this.viewModel.Parameters.RedirectDrive == RdpRedirectDrive.Enabled;
                advancedSettings.RedirectDevices =
                    this.viewModel.Parameters.RedirectDevice == RdpRedirectDevice.Enabled;

                switch (this.viewModel.Parameters.AudioMode)
                {
                    case RdpAudioMode.PlayLocally:
                        securedSettings2.AudioRedirectionMode = 0;
                        break;
                    case RdpAudioMode.PlayOnServer:
                        securedSettings2.AudioRedirectionMode = 1;
                        break;
                    case RdpAudioMode.DoNotPlay:
                        securedSettings2.AudioRedirectionMode = 2;
                        break;
                }

                //
                // Display settings.
                //
                this.rdpClient.FullScreen = false;

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

                switch (this.viewModel.Parameters.DesktopSize)
                {
                    case RdpDesktopSize.ScreenSize:
                        var screenSize = Screen.GetBounds(this);
                        this.rdpClient.DesktopHeight = screenSize.Height;
                        this.rdpClient.DesktopWidth = screenSize.Width;
                        this.autoResize = false;
                        break;

                    case RdpDesktopSize.ClientSize:
                        this.rdpClient.DesktopHeight = this.Size.Height;
                        this.rdpClient.DesktopWidth = this.Size.Width;
                        this.autoResize = false;
                        break;

                    case RdpDesktopSize.AutoAdjust:
                        this.rdpClient.DesktopHeight = this.Size.Height;
                        this.rdpClient.DesktopWidth = this.Size.Width;
                        this.autoResize = true;
                        break;
                }

                this.currentConnectionSize = this.initialConnectionSize = new Size(
                    this.rdpClient.DesktopWidth,
                    this.rdpClient.DesktopHeight);

                switch (this.viewModel.Parameters.BitmapPersistence)
                {
                    case RdpBitmapPersistence.Disabled:
                        advancedSettings.BitmapPersistence = 0;
                        break;

                    case RdpBitmapPersistence.Enabled:
                        // This setting can cause disconnects when running more than
                        // ~4 sessions in parallel.
                        advancedSettings.BitmapPersistence = 1;
                        break;
                }

                //
                // Keyboard settings.
                //
                this.rdpClient.SecuredSettings2.KeyboardHookMode =
                    (int)this.viewModel.Parameters.HookWindowsKeys;

                advancedSettings.allowBackgroundInput = 1;

                //
                // Set hotkey to trigger OnFocusReleasedEvent. This should be
                // the same as the main window uses to move the focus to the
                // control.
                //
                // NB. The Ctrl+Alt modifiers are implied by the HotKeyFocusRelease properties.
                //
                Debug.Assert(ToggleFocusHotKey.HasFlag(Keys.Control));
                Debug.Assert(ToggleFocusHotKey.HasFlag(Keys.Alt));
                var focusReleaseVirtualKey = (int)(ToggleFocusHotKey & ~(Keys.Control | Keys.Alt));

                advancedSettings.HotKeyFocusReleaseLeft = focusReleaseVirtualKey;
                advancedSettings.HotKeyFocusReleaseRight = focusReleaseVirtualKey;

                //
                // NB. The Ctrl+Alt modifiers are implied by the HotKeyFullScreen properties.
                //
                Debug.Assert(LeaveFullScreenHotKey.HasFlag(Keys.Control));
                Debug.Assert(LeaveFullScreenHotKey.HasFlag(Keys.Alt));
                var leaveFullScreenVirtualKey = (int)(LeaveFullScreenHotKey & ~(Keys.Control | Keys.Alt));

                advancedSettings.HotKeyFullScreen = (int)leaveFullScreenVirtualKey;

                //
                // Enable WebAuthn redirection. This requires at least 22H2, both client- and server-side.
                //
                // Once the plugin DLL is loaded, WebAuthn redirection is enabled automatically
                // unless there's a client- or server-side policy that disabled WebAuthn redirection.
                //
                // See also:
                // https://interopevents.blob.core.windows.net/events/2023/RDP%20IO%20Lab/ \
                // PDF/DavidBelanger_Authentication%20-%20RDP%20IO%20Labs%20March%202023.pdf
                //
                if (this.viewModel.Parameters.RedirectWebAuthn == RdpRedirectWebAuthn.Enabled)
                {
                    var webauthnPluginPath = Path.Combine(Environment.SystemDirectory, WebAuthnPlugin);
                    if (File.Exists(webauthnPluginPath))
                    {
                        try
                        {
                            advancedSettings.PluginDlls = webauthnPluginPath;
                            ApplicationTraceSource.Log.TraceInformation(
                                "Loaded RDP plugin {0}", webauthnPluginPath);
                        }
                        catch (Exception e)
                        {
                            ApplicationTraceSource.Log.TraceWarning(
                                "Loading RDP plugin {0} failed: {1}",
                                webauthnPluginPath,
                                e.Message);
                        }
                    }
                }

                this.connecting = true;
                this.rdpClient.Connect();
                this.viewModel.State.Value = RdpViewModel.ConnectionState.Connecting;
            }
        }

        private void Reconnect()
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.ConnectionLost);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                UpdateLayout(LayoutMode.Wait);

                this.connecting = true;
                this.rdpClient.Connect();
                this.viewModel.State.Value = RdpViewModel.ConnectionState.Connecting;
            }
        }

        private void ReconnectToResize(Size size)
        {
            Debug.Assert( // TODO: Disallow when not logged on yet
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connected ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.ConnectionLost);

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(this.currentConnectionSize, size))
            {
                //
                // Only resize if the size really changed, otherwise we put unnecessary
                // stress on the control (especially if events come in quick succession).
                //
                if (size != this.currentConnectionSize && !this.connecting)
                {
                    if (this.rdpClient.FullScreen)
                    {
                        //
                        // Full-screen requires a classic, reconnect-based resizing.
                        //
                        this.connecting = true;
                        this.viewModel.State.Value = RdpViewModel.ConnectionState.Connecting;
                        this.rdpClient.Reconnect((uint)size.Width, (uint)size.Height);
                    }
                    else
                    {
                        //
                        // Try to adjust settings without reconnecting - this only works when
                        // (1) The server is running 2012R2 or newer
                        // (2) The logon process has completed.
                        //
                        try
                        {
                            this.rdpClient.UpdateSessionDisplaySettings(
                                (uint)size.Width,
                                (uint)size.Height,
                                (uint)size.Width,
                                (uint)size.Height,
                                0,  // Landscape
                                1,  // No desktop scaling
                                1); // No device scaling
                        }
                        catch (COMException e) when (e.HResult == (int)HRESULT.E_UNEXPECTED)
                        {
                            ApplicationTraceSource.Log.TraceWarning("Adjusting desktop size (w/o) reconnect failed.");

                            //
                            // Revert to classic, reconnect-based resizing.
                            //
                            this.connecting = true;
                            this.viewModel.State.Value = RdpViewModel.ConnectionState.Connecting;
                            this.rdpClient.Reconnect((uint)size.Width, (uint)size.Height);
                        }
                    }

                    this.currentConnectionSize = size;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    var value = this.rdpClient.Connected == 1 && !this.connecting;

                    Debug.Assert((this.rdpClient.Connected == 1) ==
                        (this.viewModel.State.Value == RdpViewModel.ConnectionState.Connected ||
                        this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn));

                    return value;
                }
                catch (Exception e) when (e.IsComException())
                {
                    //
                    // During an unorderly disconnect, it's possible that we
                    // get an exception here, cf. b/251163460. This can manifest
                    // itself in a COMException or a InvalidComObjectException.
                    //
                    ApplicationTraceSource.Log.TraceError(e);
                    return false;
                }
            }
        }

        public bool IsConnecting
        {
            get
            {
                try
                {
                    var value = this.rdpClient.Connected == 2 && !this.connecting;

                    Debug.Assert((this.rdpClient.Connected == 2) ==
                        (this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting));

                    return value;
                }
                catch (Exception e) when (e.IsComException())
                {
                    //
                    // During an unorderly disconnect, it's possible that we
                    // get an exception here, cf. b/251163460. This can manifest
                    // itself in a COMException or a InvalidComObjectException.
                    //
                    ApplicationTraceSource.Log.TraceError(e);
                    return false;
                }
            }
        }

        public bool CanEnterFullScreen => this.IsConnected && !IsAnyDocumentInFullScreen;

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void RemoteDesktopPane_SizeChanged(object sender, EventArgs e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                this.autoResize, this.currentConnectionSize, this.Size))
            {
                if (this.Size.Width == 0 || this.Size.Height == 0)
                {
                    // Probably the window is being minimized. Ignore
                    // that event since it merely causes stress on the
                    // RDP control.
                    return;
                }
                else if (this.Size == this.currentConnectionSize)
                {
                    // This event is redundant, ignore.
                    return;
                }
                else if (IsFullscreenMinimized)
                {
                    // During a restore, we might receive a request to resize
                    // to normal size. We must ignore that.
                    return;
                }

                //
                // Rearrange controls based on new size.
                //
                // NB. If any of the above conditions applied, we must skip
                // this step since the size data might be inaccurate.
                //
                UpdateLayout(this.Mode);

                if (this.autoResize)
                {
                    //
                    // Do not resize immediately since there might be another resize
                    // event coming in a few milliseconds. Instead, delay the operation
                    // by deferring it to a timer.
                    //
                    this.reconnectToResizeTimer.Start();
                }
            }
        }

        private async void RemoteDesktopPane_FormClosing(object sender, FormClosingEventArgs args)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                if (this.IsConnecting)
                {
                    ApplicationTraceSource.Log.TraceVerbose(
                        "RemoteDesktopPane: Aborting FormClosing because control is in connecting");

                    args.Cancel = true;
                    return;
                }

                // Stop the timer, otherwise it might touch a disposing control.
                this.reconnectToResizeTimer.Stop();

                if (this.IsConnected)
                {
                    try
                    {
                        ApplicationTraceSource.Log.TraceVerbose(
                            "RemoteDesktopPane: Disconnecting because form is closing");

                        // NB. This does not trigger an OnDisconnected event.
                        this.rdpClient.Disconnect();
                    }
                    catch (Exception e)
                    {
                        ApplicationTraceSource.Log.TraceVerbose(
                            "RemoteDesktopPane: Disconnecting failed");

                        this.exceptionDialog.Show(this, "Disconnecting failed", e);
                    }
                }

                // Mark this pane as being in closing state even though it is still
                // visible at this point. The flag ensures that this pane is
                // not considered by TryGetExistingPane anymore.
                this.IsFormClosing = true;

                await this.eventService.PublishAsync(new SessionEndedEvent(this.Instance))
                    .ConfigureAwait(true);
            }
        }

        private void reconnectToResizeTimer_Tick(object sender, EventArgs e)
        {
            Debug.Assert(this.autoResize);

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(this.autoResize))
            {
                if (!this.Visible)
                {
                    // Form is closing, better not touch anything.
                }
                else if (!this.IsConnecting)
                {
                    // Reconnect to resize remote desktop.
                    ReconnectToResize(this.Size);
                }

                // Do not fire again.
                this.reconnectToResizeTimer.Stop();
            }
        }

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private async void rdpClient_OnFatalError(
            object sender,
            IMsTscAxEvents_OnFatalErrorEvent args)
        {
            this.viewModel.State.Value = RdpViewModel.ConnectionState.ConnectionLost;
            await ShowErrorAndClose(
                    "Fatal error",
                    new RdpFatalException(args.errorCode))
                .ConfigureAwait(true);
        }

        private async void rdpClient_OnLogonError(
            object sender,
            IMsTscAxEvents_OnLogonErrorEvent args)
        {
            var e = new RdpLogonException(args.lError);
            if (!e.IsIgnorable)
            {
                this.viewModel.State.Value = RdpViewModel.ConnectionState.ConnectionLost;

                await ShowErrorAndClose("Logon failed", e)
                    .ConfigureAwait(true); ;
            }
        }

        private void rdpClient_OnLoginComplete(object sender, EventArgs e)
        {
            this.viewModel.State.Value = RdpViewModel.ConnectionState.LoggedOn;
        }

        private async void rdpClient_OnDisconnected(
            object sender,
            IMsTscAxEvents_OnDisconnectedEvent args)
        {
            this.viewModel.State.Value = RdpViewModel.ConnectionState.ConnectionLost;

            var e = new RdpDisconnectedException(
                args.discReason,
                this.rdpClient.GetErrorDescription((uint)args.discReason, 0));

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e.Message))
            {
                LeaveFullScreen();

                //
                // Force focus back to outer window. This ensures that after the pane is
                // closed, the focus can move to the next remaining pane.
                //
                // If we don't do this, the next remaining pane won't invalidate/repaint
                // properly.
                // 
                this.MainWindow.MainPanel.Focus();

                if (!this.connecting && e.IsTimeout)
                {
                    //
                    // An already-established connection timed out, this is common when
                    // connecting to Windows 10 VMs.
                    //
                    // NB. The same error code can occur during the initial connection,
                    // but then it should be treated as an error.
                    //
                    UpdateLayout(LayoutMode.Reconnect);
                }
                else if (e.IsIgnorable)
                {
                    Close();
                }
                else
                {
                    await ShowErrorAndClose("Disconnected", e)
                        .ConfigureAwait(true);
                }
            }
        }

        private async void rdpClient_OnConnected(object sender, EventArgs e)
        {
            this.viewModel.State.Value = RdpViewModel.ConnectionState.Connected;

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(this.rdpClient.ConnectedStatusText))
            {
                Debug.Assert(this.connecting, "Connecting flag must have been set");

                UpdateLayout(LayoutMode.Normal);

                // Notify our listeners.
                await this.eventService.PublishAsync(new SessionStartedEvent(this.Instance))
                    .ConfigureAwait(true);

                // Wait a bit before clearing the connecting flag. The control can
                // get flaky if connect operations are done too soon.
                await Task.Delay(2000).ConfigureAwait(true); ;
                this.connecting = false;
            }
        }


        private void rdpClient_OnConnecting(object sender, EventArgs e)
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnAuthenticationWarningDisplayed(object sender, EventArgs _)
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.AuthenticationWarningDisplayed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void rdpClient_OnWarning(
            object sender,
            IMsTscAxEvents_OnWarningEvent args)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(args.warningCode))
            { }
        }

        private void rdpClient_OnAutoReconnecting2(
            object sender,
            IMsTscAxEvents_OnAutoReconnecting2Event args)
        {
            Debug.Assert(
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connected ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var e = new RdpDisconnectedException(
                    args.disconnectReason,
                    this.rdpClient.GetErrorDescription((uint)args.disconnectReason, 0));

                ApplicationTraceSource.Log.TraceVerbose(
                    "Reconnect attempt {0}/{1} - {2} - {3}",
                    args.attemptCount,
                    args.maxAttemptCount,
                    e.Message,
                    args.networkAvailable);


                this.viewModel.State.Value = RdpViewModel.ConnectionState.Connecting;
            }
        }

        private async void rdpClient_OnAutoReconnected(object sender, EventArgs e)
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                if (this.connecting)
                {
                    // Wait a bit before clearing the connecting flag. The control can
                    // get flaky if connect operations are done too soon.
                    await Task.Delay(2000).ConfigureAwait(true); ;
                    this.connecting = false;


                    this.viewModel.State.Value = RdpViewModel.ConnectionState.LoggedOn;
                }
            }
        }

        private void rdpClient_OnFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                //
                // Release focus and move it to the panel, which ensures
                // that any other shortcuts start applying again.
                //
                this.MainWindow.MainPanel.Focus();
            }
        }

        private void rdpClient_OnRemoteDesktopSizeChange(
            object sender,
            IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
        {
            Debug.Assert(
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connected ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(this.autoResize))
            { }
        }

        private void rdpClient_OnServiceMessageReceived(
            object sender,
            IMsTscAxEvents_OnServiceMessageReceivedEvent e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e.serviceMessage))
            { }
        }

        private async void reconnectButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Debug.Assert(
                this.viewModel.State.Value == RdpViewModel.ConnectionState.ConnectionLost ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connecting);

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

        private void rdpClient_OnRequestGoFullScreen(object sender, EventArgs e)
        {
            Debug.Assert(
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connected ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            EnterFullscreen(this.useAllScreensForFullScreen);

            this.rdpClient.Size = this.rdpClient.Parent.Size;
            ReconnectToResize(this.rdpClient.Size);
        }

        private void rdpClient_OnRequestLeaveFullScreen(object sender, EventArgs e)
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            LeaveFullScreen();

            this.rdpClient.Size = this.rdpClient.Parent.Size;

            ReconnectToResize(this.autoResize
                ? this.rdpClient.Size
                : this.initialConnectionSize);
        }

        private void rdpClient_OnRequestContainerMinimize(object sender, EventArgs e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                MinimizeWindow();
            }
        }

        //---------------------------------------------------------------------
        // IRemoteDesktopSession.
        //---------------------------------------------------------------------

        public bool TrySetFullscreen(FullScreenMode mode)
        {
            Debug.Assert( //TODO: Disallow when not logged on yet
                this.viewModel.State.Value == RdpViewModel.ConnectionState.Connected ||
                this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(mode))
            {
                if (this.IsConnecting)
                {
                    // Do not mess with the control while connecting.
                    return false;
                }

                ApplicationTraceSource.Log.TraceVerbose("Setting full screen mode to {0}", mode);

                //
                // Request full screen - this causes OnRequestGoFullScreen
                // to be fired, which does the actuall full-screen switch.
                //
                this.useAllScreensForFullScreen = (mode == FullScreenMode.AllScreens);
                this.rdpClient.FullScreen = (mode != FullScreenMode.Off);

                return true;
            }
        }

        public void ShowSecurityScreen()
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                SendKeys(
                    Keys.ControlKey,
                    Keys.Menu,
                    Keys.Delete);
            }
        }

        public void ShowTaskManager()
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                SendKeys(
                    Keys.ControlKey,
                    Keys.ShiftKey,
                    Keys.Escape);
            }
        }

        public void SendKeys(params Keys[] keys)
        {
            Debug.Assert(this.viewModel.State.Value == RdpViewModel.ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.rdpClient.Focus();

                var nonScriptable = (IMsRdpClientNonScriptable5)this.rdpClient.GetOcx();

                if (this.keysSent++ == 0)
                {
                    // The RDP control sometimes swallows the first key combination
                    // that is sent. So start by a harmless ESC.
                    SendKeys(Keys.Escape);
                }

                nonScriptable.SendKeys(keys);
            }
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

        protected override Size DefaultFloatWindowClientSize => this.currentConnectionSize;

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
                this.rdpClient.ContainingControl = this.rescueWindow;
            }

            base.OnDockBegin();
        }

        protected override void OnDockEnd()
        {
            if (this.rescueWindow != null && this.rdpClient != null)
            {
                this.rdpClient.Parent = this;
                this.rdpClient.ContainingControl = this;
                this.rdpClient.Size = this.currentConnectionSize;
                this.rescueWindow.Close();
                this.rescueWindow = null;
            }

            base.OnDockEnd();
        }
    }
}
