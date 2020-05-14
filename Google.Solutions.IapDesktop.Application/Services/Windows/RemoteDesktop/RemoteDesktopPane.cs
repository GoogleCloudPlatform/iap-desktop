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
using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Util;
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

namespace Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop
{
    [ComVisible(false)]
    public partial class RemoteDesktopPane : ToolWindow, IRemoteDesktopSession
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventService eventService;

        private int keysSent = 0;
        private bool autoResize = false;
        private bool connecting = false;

        public VmInstanceReference Instance { get; }

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
        }

        private async Task ShowErrorAndClose(string caption, RdpException e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(e.Message))
            {
                await this.eventService.FireAsync(
                    new RemoteDesktopConnectionFailedEvent(this.Instance, e));
                this.exceptionDialog.Show(this, caption, e);
                Close();
            }
        }
        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public RemoteDesktopPane(
            IEventService eventService,
            IExceptionDialog exceptionDialog,
            VmInstanceReference vmInstance)
        {
            this.exceptionDialog = exceptionDialog;
            this.eventService = eventService;
            this.Instance = vmInstance;

            this.TabText = vmInstance.InstanceName;
            this.DockAreas = DockAreas.Document;

            var fullScreenMenuItem = new ToolStripMenuItem("&Full screen");
            fullScreenMenuItem.Click += fullScreenMenuItem_Click;
            this.TabContextStrip.Items.Add(fullScreenMenuItem);
            this.TabContextStrip.Opening += tabContextStrip_Opening;
        }

        public void Connect(
            string server,
            ushort port,
            VmInstanceConnectionSettings settings
            )
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(
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
                this.rdpClient.Domain = settings.Domain;
                this.rdpClient.UserName = settings.Username;
                advancedSettings.RDPPort = port;
                advancedSettings.ClearTextPassword = settings.Password.AsClearText();

                //
                // Connection security settings.
                //
                advancedSettings.EnableCredSspSupport = true;
                nonScriptable.PromptForCredentials = false;
                nonScriptable.NegotiateSecurityLayer = true;

                switch (settings.AuthenticationLevel)
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
                    settings.UserAuthenticationBehavior == RdpUserAuthenticationBehavior.PromptOnFailure;

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
                advancedSettings.singleConnectionTimeout = settings.ConnectionTimeout;
                advancedSettings.overallConnectionTimeout = settings.ConnectionTimeout;

                //
                // Behavior settings.
                //
                advancedSettings.DisplayConnectionBar =
                    (settings.ConnectionBar != RdpConnectionBarState.Off);
                advancedSettings.ConnectionBarShowMinimizeButton = false;
                advancedSettings.PinConnectionBar =
                    (settings.ConnectionBar == RdpConnectionBarState.Pinned);
                advancedSettings.EnableWindowsKey = 1;
                advancedSettings.GrabFocusOnConnect = false;

                //
                // Local resources settings.
                //
                advancedSettings.RedirectClipboard =
                    settings.RedirectClipboard == RdpRedirectClipboard.Enabled;

                switch (settings.AudioMode)
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

                switch (settings.ColorDepth)
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

                switch (settings.DesktopSize)
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

                switch (settings.BitmapPersistence)
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
                this.rdpClient.Connect();
            }
        }

        public bool IsConnected => this.rdpClient.Connected == 1 && !this.connecting;
        public bool IsConnecting => this.rdpClient.Connected == 2 && !this.connecting;

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void RemoteDesktopPane_SizeChanged(object sender, EventArgs e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(
                this.autoResize, this.Size))
            {
                if (this.Size.Width == 0 || this.Size.Height == 0)
                {
                    // Probably the window is being minimized. Ignore
                    // that event since it merely causes stress on the
                    // RDP control.
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
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                if (this.IsConnecting)
                {
                    TraceSources.IapDesktop.TraceVerbose(
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
                        TraceSources.IapDesktop.TraceVerbose(
                            "RemoteDesktopPane: Disconnecting because form is closing");

                        // NB. This does not trigger an OnDisconnected event.
                        this.rdpClient.Disconnect();
                    }
                    catch (Exception e)
                    {
                        TraceSources.IapDesktop.TraceVerbose(
                            "RemoteDesktopPane: Disconnecting failed");

                        // TODO: Ignore?
                        this.exceptionDialog.Show(this, "Disconnecting failed", e);
                    }
                }

                await this.eventService.FireAsync(
                    new RemoteDesktopWindowClosedEvent(this.Instance));
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

        private void fullScreenMenuItem_Click(object sender, EventArgs e)
        {
            TrySetFullscreen(true);
        }

        private void reconnectToResizeTimer_Tick(object sender, EventArgs e)
        {
            Debug.Assert(this.autoResize);

            using (TraceSources.IapDesktop.TraceMethod().WithParameters(this.autoResize))
            {
                if (!this.Visible)
                {
                    // Form is closing, better not touch anything.
                }
                else if (!this.IsConnecting)
                {
                    // Reconnect to resize remote desktop.
                    this.rdpClient.Reconnect((uint)this.Size.Width, (uint)this.Size.Height);
                }

                // Do not fire again.
                reconnectToResizeTimer.Stop();
            }
        }

        private void rdpClient_OnEnterFullScreenMode(object sender, EventArgs e)
        {
            if (!this.IsConnecting && this.autoResize)
            {
                // Adjust desktop size to full screen.
                var screenSize = Screen.GetBounds(this);

                this.connecting = true;
                this.rdpClient.Reconnect((uint)screenSize.Width, (uint)screenSize.Height);
            }
        }

        private void rdpClient_OnLeaveFullScreenMode(object sender, EventArgs e)
        {
            if (!this.IsConnecting && this.autoResize)
            {
                // Return to normal size.

                this.connecting = true;
                this.rdpClient.Reconnect((uint)this.Size.Width, (uint)this.Size.Height);
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
                new RdpFatalException(args.errorCode));
        }

        private async void rdpClient_OnLogonError(
            object sender,
            IMsTscAxEvents_OnLogonErrorEvent args)
        {
            var e = new RdpLogonException(args.lError);
            if (!e.IsIgnorable)
            {
                await ShowErrorAndClose("Logon failed", e);
            }
        }

        private async void rdpClient_OnDisconnected(
            object sender,
            IMsTscAxEvents_OnDisconnectedEvent args)
        {
            var e = new RdpDisconnectedException(
                args.discReason,
                this.rdpClient.GetErrorDescription((uint)args.discReason, 0));

            using (TraceSources.IapDesktop.TraceMethod().WithParameters(e.Message))
            {
                if (e.IsIgnorable)
                {
                    Close();
                }
                else
                {
                    await ShowErrorAndClose("Disconnected", e);
                }
            }
        }

        private async void rdpClient_OnConnected(object sender, EventArgs e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(this.rdpClient.ConnectedStatusText))
            {
                Debug.Assert(this.connecting, "Connecting flag must have been set");

                this.spinner.Visible = false;

                // Notify our listeners.
                await this.eventService.FireAsync(
                    new RemoteDesktopConnectionSuceededEvent(this.Instance));

                // Wait a bit before clearing the connecting flag. The control can
                // get flaky if connect operations are done too soon.
                await Task.Delay(2000);
                this.connecting = false;
            }
        }


        private void rdpClient_OnConnecting(object sender, EventArgs e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnAuthenticationWarningDisplayed(object sender, EventArgs _)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnWarning(
            object sender,
            IMsTscAxEvents_OnWarningEvent args)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(args.warningCode))
            { }
        }

        private void rdpClient_OnAutoReconnecting2(
            object sender,
            IMsTscAxEvents_OnAutoReconnecting2Event args)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                var e = new RdpDisconnectedException(
                    args.disconnectReason,
                    this.rdpClient.GetErrorDescription((uint)args.disconnectReason, 0));

                TraceSources.IapDesktop.TraceVerbose(
                    "Reconnect attempt {0}/{1} - {2} - {3}",
                    args.attemptCount,
                    args.maxAttemptCount,
                    e.Message,
                    args.networkAvailable);
            }
        }

        private async void rdpClient_OnAutoReconnected(object sender, EventArgs e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                if (this.connecting)
                {
                    // Wait a bit before clearing the connecting flag. The control can
                    // get flaky if connect operations are done too soon.
                    await Task.Delay(2000);
                    this.connecting = false;
                }
            }
        }

        private void rdpClient_OnFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            { }
        }

        private void rdpClient_OnRemoteDesktopSizeChange(
            object sender,
            IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(this.autoResize))
            { }
        }

        private void rdpClient_OnServiceMessageReceived(
            object sender,
            IMsTscAxEvents_OnServiceMessageReceivedEvent e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(e.serviceMessage))
            { }
        }


        //---------------------------------------------------------------------
        // IRemoteDesktopSession.
        //---------------------------------------------------------------------

        public bool TrySetFullscreen(bool fullscreen)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                if (this.IsConnecting)
                {
                    // Do not mess with the control while connecting.
                    return false;
                }

                TraceSources.IapDesktop.TraceVerbose("Setting full screen mode to ", fullscreen);
                this.rdpClient.FullScreen = fullscreen;
                return true;
            }
        }

        public void ShowSecurityScreen()
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                SendKeys(
                    Keys.ControlKey,
                    Keys.Menu,
                    Keys.Delete);
            }
        }

        public void ShowTaskManager()
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                SendKeys(
                    Keys.ControlKey,
                    Keys.ShiftKey,
                    Keys.Escape);
            }
        }

        public void SendKeys(params Keys[] keys)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                this.rdpClient.Focus();

                var nonScriptable = (IMsRdpClientNonScriptable5)this.rdpClient.GetOcx();

                if (this.keysSent++ == 0)
                {
                    // The RDP control sometimes swallows the first key combination
                    // that is sent. So start by a harmess ESC.
                    SendKeys(Keys.Escape);
                }

                nonScriptable.SendKeys(keys);
            }
        }
    }
}
