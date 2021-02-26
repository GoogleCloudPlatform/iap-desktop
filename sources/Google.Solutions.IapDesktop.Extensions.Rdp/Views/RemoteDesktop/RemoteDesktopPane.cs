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
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using MSTSCLib;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1031 // catch Exception
#pragma warning disable CA1801 // Review unused parameters

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop
{
    [ComVisible(false)]
    public partial class RemoteDesktopPane : DocumentWindow, IRemoteDesktopSession
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventService eventService;

        private bool useAllScreensForFullScreen = false;

        private int keysSent = 0;
        private bool autoResize = false;
        private bool connecting = false;

        // Track the (client area) size of the remote connection.
        private Size connectionSize;

        public InstanceLocator Instance { get; }

        public bool IsFormClosing { get; private set; } = false;

        private void UpdateLayout()
        {
            // NB. Docking does not work reliably with the OCX, so keep the size
            // in sync programmatically.
            this.rdpClient.Size = this.Size;

            // It would be nice to update the desktop size as well, but that's not
            // supported by the control.

            this.spinner.Location = new Point(
                (this.Size.Width - this.spinner.Width) / 2,
                (this.Size.Height - this.spinner.Height) / 2);

            this.reconnectPanel.Location = new Point(
                (this.Size.Width - this.reconnectPanel.Width) / 2,
                (this.Size.Height - this.reconnectPanel.Height) / 2);
        }

        private async Task ShowErrorAndClose(string caption, RdpException e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(e.Message))
            {
                // Make sure we're not fullscreen anymore.
                LeaveFullScreen();

                await this.eventService.FireAsync(
                    new SessionAbortedEvent(this.Instance, e))
                    .ConfigureAwait(true);
                this.exceptionDialog.Show(this, caption, e);
                Close();
            }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static RemoteDesktopPane TryGetExistingPane(
            IMainForm mainForm,
            InstanceLocator vmInstance)
        {
            return mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<RemoteDesktopPane>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .FirstOrDefault();
        }

        public static RemoteDesktopPane TryGetActivePane(
            IMainForm mainForm)
        {
            return mainForm.MainPanel.ActiveDocument as RemoteDesktopPane;
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public RemoteDesktopPane(
            IServiceProvider serviceProvider,
            InstanceLocator vmInstance) 
            : base(serviceProvider)
        {
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.eventService = serviceProvider.GetService<IEventService>();
            this.Instance = vmInstance;

            // The ActiveX fails when trying to drag/dock a window, so disable
            // that feature.
            this.AllowEndUserDocking = false;

            var singleScreenFullScreenMenuItem = new ToolStripMenuItem("&Full screen");
            singleScreenFullScreenMenuItem.Click += (sender, _) 
                => TrySetFullscreen(FullScreenMode.SingleScreen);
            this.TabContextStrip.Items.Add(singleScreenFullScreenMenuItem);
            this.TabContextStrip.Opening += tabContextStrip_Opening;

            var allScreensFullScreenMenuItem = new ToolStripMenuItem("&Full screen (multiple displays)");
            allScreensFullScreenMenuItem.Click += (sender, _) 
                => TrySetFullscreen(FullScreenMode.AllScreens);
            this.TabContextStrip.Items.Add(allScreensFullScreenMenuItem);
            this.TabContextStrip.Opening += tabContextStrip_Opening;
        }

        public override string Text 
        { 
            get => this.Instance?.Name ?? "Remote Desktop"; 
            set { }
        }

        public void Connect(
            string server,
            ushort port,
            RdpInstanceSettings settings
            )
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(
                server,
                port,
                settings.ConnectionTimeout))
            {
                // NB. The initialization needs to happen after the pane is shown, otherwise
                // an error happens indicating that the control does not have a Window handle.
                InitializeComponent();
                UpdateLayout();

                var advancedSettings = this.rdpClient.AdvancedSettings7;
                var nonScriptable = (IMsRdpClientNonScriptable5)this.rdpClient.GetOcx();
                var securedSettings2 = this.rdpClient.SecuredSettings2;

                //
                // Basic connection settings.
                //
                this.rdpClient.Server = server;
                this.rdpClient.Domain = settings.Domain.StringValue;
                this.rdpClient.UserName = settings.Username.StringValue;
                advancedSettings.RDPPort = port;
                advancedSettings.ClearTextPassword =
                    settings.Password.ClearTextValue ?? string.Empty;
                nonScriptable.AllowCredentialSaving = false;

                //
                // Connection security settings.
                //
                advancedSettings.EnableCredSspSupport = true;
                nonScriptable.PromptForCredentials = false;
                nonScriptable.NegotiateSecurityLayer = true;

                switch (settings.AuthenticationLevel.EnumValue)
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
                    settings.UserAuthenticationBehavior.EnumValue == RdpUserAuthenticationBehavior.PromptOnFailure;

                //
                // Advanced connection settings.
                //
                advancedSettings.keepAliveInterval = 60000;
                advancedSettings.PerformanceFlags = 0; // Enable all features, it's 2020.
                advancedSettings.EnableAutoReconnect = true;
                advancedSettings.MaxReconnectAttempts = 10;

                //
                // Apply timeouts. Note that the control might take
                // about twice the configured timeout before sending a 
                // OnDisconnected event.
                //
                advancedSettings.singleConnectionTimeout = settings.ConnectionTimeout.IntValue;
                advancedSettings.overallConnectionTimeout = settings.ConnectionTimeout.IntValue;

                //
                // Behavior settings.
                //
                advancedSettings.DisplayConnectionBar =
                    (settings.ConnectionBar.EnumValue != RdpConnectionBarState.Off);
                advancedSettings.ConnectionBarShowMinimizeButton = false;
                advancedSettings.PinConnectionBar =
                    (settings.ConnectionBar.EnumValue == RdpConnectionBarState.Pinned);
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
                    settings.RedirectClipboard.EnumValue == RdpRedirectClipboard.Enabled;

                switch (settings.AudioMode.EnumValue)
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

                switch (settings.ColorDepth.EnumValue)
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

                switch (settings.DesktopSize.EnumValue)
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

                switch (settings.BitmapPersistence.EnumValue)
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
                // NB. Apply key combinations to the remote server only when the client is running 
                // in full-screen mode.
                this.rdpClient.SecuredSettings2.KeyboardHookMode = 2;

                advancedSettings.allowBackgroundInput = 1;

                this.connecting = true;
                this.connectionSize = this.Size;
                this.rdpClient.Connect();
            }
        }

        private void Reconnect()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                UpdateLayout();

                // Reset visibility to default values.
                this.reconnectPanel.Visible = false;
                this.spinner.Visible = true;

                this.connecting = true;
                this.rdpClient.Connect();
            }
        }

        private void ReconnectToResize(Size size)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(this.connectionSize, size))
            {
                // Only resize if the size really changed, otherwise we put unnecessary
                // stress on the control (especially if events come in quick succession).
                if (size != this.connectionSize && !this.connecting)
                {
                    if (this.rdpClient.FullScreen)
                    {
                        //
                        // Full-screen requires a classic, reconnect-based resizing.
                        //
                        this.connecting = true;
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
                                (uint)this.Width,
                                (uint)this.Height,
                                (uint)this.Width,
                                (uint)this.Height,
                                0,  // Landscape
                                1,  // No desktop scaling
                                1); // No device scaling
                        }
                        catch (COMException e) when ((uint)e.HResult == UnsafeNativeMethods.E_UNEXPECTED)
                        {
                            ApplicationTraceSources.Default.TraceWarning("Adjusting desktop size (w/o) reconnect failed.");

                            //
                            // Revert to classic, reconnect-based resizing.
                            //
                            this.connecting = true;
                            this.rdpClient.Reconnect((uint)size.Width, (uint)size.Height);
                        }
                    }

                    this.connectionSize = size;
                }
            }
        }

        public bool IsConnected => this.rdpClient.Connected == 1 && !this.connecting;
        public bool IsConnecting => this.rdpClient.Connected == 2 && !this.connecting;

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void RemoteDesktopPane_SizeChanged(object sender, EventArgs e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(
                this.autoResize, this.connectionSize, this.Size))
            {
                if (this.Size.Width == 0 || this.Size.Height == 0)
                {
                    // Probably the window is being minimized. Ignore
                    // that event since it merely causes stress on the
                    // RDP control.
                    return;
                }
                else if (this.Size == this.connectionSize)
                {
                    // This event is redundant, ignore.
                    return;
                }

                UpdateLayout();

                if (this.autoResize)
                {
                    // Do not resize immediately since there might be another resitze
                    // event coming in a few miliseconds. Instead, delay the operation
                    // by deferring it to a timer.
                    this.reconnectToResizeTimer.Start();
                }
            }
        }

        private async void RemoteDesktopPane_FormClosing(object sender, FormClosingEventArgs args)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                if (this.IsConnecting)
                {
                    ApplicationTraceSources.Default.TraceVerbose(
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
                        ApplicationTraceSources.Default.TraceVerbose(
                            "RemoteDesktopPane: Disconnecting because form is closing");

                        // NB. This does not trigger an OnDisconnected event.
                        this.rdpClient.Disconnect();
                    }
                    catch (Exception e)
                    {
                        ApplicationTraceSources.Default.TraceVerbose(
                            "RemoteDesktopPane: Disconnecting failed");

                        this.exceptionDialog.Show(this, "Disconnecting failed", e);
                    }
                }

                // Mark this pane as being in closing state even though it is still
                // visible at this point.
                this.IsFormClosing = true;
                await this.eventService.FireAsync(new SessionEndedEvent(this.Instance))
                    .ConfigureAwait(true);
            }
        }

        private void tabContextStrip_Opening(object sender, CancelEventArgs e)
        {
            foreach (var menuItem in this.TabContextStrip.Items.Cast<ToolStripDropDownItem>())
            {
                // Disable everything while we are connecting.
                menuItem.Enabled = !this.IsConnecting;
            }
        }

        private void reconnectToResizeTimer_Tick(object sender, EventArgs e)
        {
            Debug.Assert(this.autoResize);

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(this.autoResize))
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
                reconnectToResizeTimer.Stop();
            }
        }

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private async void rdpClient_OnFatalError(
            object sender,
            IMsTscAxEvents_OnFatalErrorEvent args)
        {
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
                await ShowErrorAndClose("Logon failed", e)
                    .ConfigureAwait(true); ;
            }
        }

        private async void rdpClient_OnDisconnected(
            object sender,
            IMsTscAxEvents_OnDisconnectedEvent args)
        {
            var e = new RdpDisconnectedException(
                args.discReason,
                this.rdpClient.GetErrorDescription((uint)args.discReason, 0));

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(e.Message))
            {
                LeaveFullScreen();

                if (!this.connecting && e.IsTimeout)
                {
                    // An already-established connection timed out, this is common when
                    // connecting to Windows 10 VMs.
                    //
                    // NB. The same error code can occur during the initial connection,
                    // but then it should be treated as an error.

                    this.reconnectPanel.Visible = true;
                }
                else if (e.IsIgnorable)
                {
                    Close();
                }
                else
                {
                    await ShowErrorAndClose("Disconnected", e)
                        .ConfigureAwait(true); ;
                }
            }
        }

        private async void rdpClient_OnConnected(object sender, EventArgs e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(this.rdpClient.ConnectedStatusText))
            {
                Debug.Assert(this.connecting, "Connecting flag must have been set");

                this.spinner.Visible = false;

                // Notify our listeners.
                await this.eventService.FireAsync(new SessionStartedEvent(this.Instance))
                    .ConfigureAwait(true);

                // Wait a bit before clearing the connecting flag. The control can
                // get flaky if connect operations are done too soon.
                await Task.Delay(2000).ConfigureAwait(true); ;
                this.connecting = false;
            }
        }


        private void rdpClient_OnConnecting(object sender, EventArgs e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnAuthenticationWarningDisplayed(object sender, EventArgs _)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnWarning(
            object sender,
            IMsTscAxEvents_OnWarningEvent args)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(args.warningCode))
            { }
        }

        private void rdpClient_OnAutoReconnecting2(
            object sender,
            IMsTscAxEvents_OnAutoReconnecting2Event args)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                var e = new RdpDisconnectedException(
                    args.disconnectReason,
                    this.rdpClient.GetErrorDescription((uint)args.disconnectReason, 0));

                ApplicationTraceSources.Default.TraceVerbose(
                    "Reconnect attempt {0}/{1} - {2} - {3}",
                    args.attemptCount,
                    args.maxAttemptCount,
                    e.Message,
                    args.networkAvailable);
            }
        }

        private async void rdpClient_OnAutoReconnected(object sender, EventArgs e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                if (this.connecting)
                {
                    // Wait a bit before clearing the connecting flag. The control can
                    // get flaky if connect operations are done too soon.
                    await Task.Delay(2000).ConfigureAwait(true); ;
                    this.connecting = false;
                }
            }
        }

        private void rdpClient_OnFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnRemoteDesktopSizeChange(
            object sender,
            IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(this.autoResize))
            { }
        }

        private void rdpClient_OnServiceMessageReceived(
            object sender,
            IMsTscAxEvents_OnServiceMessageReceivedEvent e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(e.serviceMessage))
            { }
        }

        private void reconnectButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                Reconnect();
            }
        }

        private void rdpClient_OnRequestGoFullScreen(object sender, EventArgs e)
        {
            EnterFullscreen(this.useAllScreensForFullScreen);

            this.rdpClient.Size = this.rdpClient.Parent.Size;
            ReconnectToResize(this.rdpClient.Size);
        }

        private void rdpClient_OnRequestLeaveFullScreen(object sender, EventArgs e)
        {
            LeaveFullScreen();

            this.rdpClient.Size = this.rdpClient.Parent.Size;
            ReconnectToResize(this.rdpClient.Size);
        }

        //---------------------------------------------------------------------
        // IRemoteDesktopSession.
        //---------------------------------------------------------------------

        public bool TrySetFullscreen(FullScreenMode mode)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(mode))
            {
                if (this.IsConnecting)
                {
                    // Do not mess with the control while connecting.
                    return false;
                }

                ApplicationTraceSources.Default.TraceVerbose("Setting full screen mode to {0}", mode);

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
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                SendKeys(
                    Keys.ControlKey,
                    Keys.Menu,
                    Keys.Delete);
            }
        }

        public void ShowTaskManager()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                SendKeys(
                    Keys.ControlKey,
                    Keys.ShiftKey,
                    Keys.Escape);
            }
        }

        public void SendKeys(params Keys[] keys)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
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
    }
}
