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

using AxMSTSCLib;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Input;
using Google.Solutions.Platform.Interop;
using MSTSCLib;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Wrapper control for the native RDP client. Implements
    /// a smooth full-screen experience and uses a state machine
    /// to ensure reliable operation.
    /// </summary>
    public partial class RdpClient : ClientBase
    {
        private const string WebAuthnPlugin = "webauthn.dll";

        /// <summary>
        /// Maximum length of strings to use for SendString. Beyond a certain
        /// length, results get dicey.
        /// </summary>
        private const int MaxSendStringLength = 256;

        private readonly Google.Solutions.Tsc.MsRdpClient client;
        private readonly IMsRdpClientNonScriptable5 clientNonScriptable;
        private readonly IMsRdpClientAdvancedSettings6 clientAdvancedSettings;
        private readonly IMsRdpClientSecuredSettings clientSecuredSettings;
        private readonly IMsRdpExtendedSettings clientExtendedSettings;

        private readonly DeferredCallback deferResize;

        private bool reconnectPending = false;

        private int keysSent = 0;

        public RdpClient()
        {
            this.client = new Google.Solutions.Tsc.MsRdpClient
            {
                Enabled = true,
                Location = new Point(0, 0),
                Name = "client",
                Size = new Size(100, 100),
            };
            this.deferResize = new DeferredCallback(PerformDeferredResize, TimeSpan.FromMilliseconds(200));

            ((System.ComponentModel.ISupportInitialize)(this.client)).BeginInit();
            SuspendLayout();

            //
            // Hook up events.
            //
            this.client.OnConnecting += new System.EventHandler(OnRdpConnecting);
            this.client.OnConnected += new System.EventHandler(OnRdpConnected);
            this.client.OnLoginComplete += new System.EventHandler(OnRdpLoginComplete);
            this.client.OnDisconnected += new AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEventHandler(OnRdpDisconnected);
            this.client.OnRequestGoFullScreen += new System.EventHandler(OnRdpRequestGoFullScreen);
            this.client.OnRequestLeaveFullScreen += new System.EventHandler(OnRdpRequestLeaveFullScreen);
            this.client.OnFatalError += new AxMSTSCLib.IMsTscAxEvents_OnFatalErrorEventHandler(OnRdpFatalError);
            this.client.OnWarning += new AxMSTSCLib.IMsTscAxEvents_OnWarningEventHandler(OnRdpWarning);
            this.client.OnRemoteDesktopSizeChange += new AxMSTSCLib.IMsTscAxEvents_OnRemoteDesktopSizeChangeEventHandler(OnRdpRemoteDesktopSizeChange);
            this.client.OnRequestContainerMinimize += new System.EventHandler(OnRdpRequestContainerMinimize);
            this.client.OnAuthenticationWarningDisplayed += new System.EventHandler(OnRdpAuthenticationWarningDisplayed);
            this.client.OnLogonError += new AxMSTSCLib.IMsTscAxEvents_OnLogonErrorEventHandler(OnRdpLogonError);
            this.client.OnFocusReleased += new AxMSTSCLib.IMsTscAxEvents_OnFocusReleasedEventHandler(OnRdpFocusReleased);
            this.client.OnServiceMessageReceived += new AxMSTSCLib.IMsTscAxEvents_OnServiceMessageReceivedEventHandler(OnRdpServiceMessageReceived);
            this.client.OnAutoReconnected += new System.EventHandler(OnRdpAutoReconnected);
            this.client.OnAutoReconnecting2 += new AxMSTSCLib.IMsTscAxEvents_OnAutoReconnecting2EventHandler(OnRdpAutoReconnecting2);

            this.Controls.Add(this.client);
            ((System.ComponentModel.ISupportInitialize)(this.client)).EndInit();

            //
            // Set basic configuration.
            //
            this.clientSecuredSettings = this.client.SecuredSettings2;
            this.clientNonScriptable = (IMsRdpClientNonScriptable5)this.client.GetOcx();

            this.clientNonScriptable.AllowCredentialSaving = false;
            this.clientNonScriptable.PromptForCredentials = false;
            this.clientNonScriptable.NegotiateSecurityLayer = true;

            this.clientAdvancedSettings = this.client.AdvancedSettings7;
            this.clientAdvancedSettings.EnableCredSspSupport = true;
            this.clientAdvancedSettings.keepAliveInterval = 60000;
            this.clientAdvancedSettings.PerformanceFlags = 0; // Enable all features.
            this.clientAdvancedSettings.EnableAutoReconnect = true;
            this.clientAdvancedSettings.MaxReconnectAttempts = 10;
            this.clientAdvancedSettings.EnableWindowsKey = 1;
            this.clientAdvancedSettings.allowBackgroundInput = 1;

            //
            // Bitmap persistence consumes vast amounts of memory, so keep
            // it disabled.
            //
            this.clientAdvancedSettings.BitmapPersistence = 0;

            //
            // Let us handle full-screen mode ourselves.
            //
            this.clientAdvancedSettings.ContainerHandledFullScreen = 1;

            this.clientExtendedSettings = (IMsRdpExtendedSettings)this.client.GetOcx();
        }

        /// <summary>
        /// The server authentication warning has been displayed.
        /// </summary>
        public event EventHandler? ServerAuthenticationWarningDisplayed;

        /// <summary>
        /// Wait until a certain state has been reached. Mainly
        /// intended for testing.
        /// </summary>
        internal override async Task AwaitStateAsync(ConnectionState state)
        {
            await base
                .AwaitStateAsync(state)
                .ConfigureAwait(true);

            //
            // There might be a resize pending, await that too.
            //
            await this.deferResize
                .WaitForCompletionAsync()
                .ConfigureAwait(true);
        }

        protected override void OnCurrentParentFormChanged()
        {
            this.client.ContainingControl = this.CurrentParentForm;
        }

        //---------------------------------------------------------------------
        // Closing & disposing.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.client.Dispose();
            this.deferResize.Dispose();
        }

        protected override void OnFormClosing(object sender, FormClosingEventArgs args)
        {
            base.OnFormClosing(sender, args);

            if (this.State == ConnectionState.Disconnecting)
            {
                //
                // Form is being closed as a result of a disconnect
                // (not the other way round).
                //
            }
            else if (this.State == ConnectionState.Connecting)
            {
                //
                // Veto this event as it might cause the ActiveX to crash.
                //
                TerminalTraceSource.Log.TraceVerbose(
                    "RdpCLient: Aborting FormClosing because control is in connecting");

                args.Cancel = true;
                return;
            }
            else if (this.IsFullScreen)
            {
                //
                // Veto this event as it would leave an orphaned full-screen
                // window.
                //
                TerminalTraceSource.Log.TraceVerbose(
                    "RdpCLient: Aborting FormClosing because control is full-screen");

                args.Cancel = true;
                return;
            }
            else if (
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn)
            {
                //
                // Attempt an orderly disconnect.
                //
                try
                {
                    TerminalTraceSource.Log.TraceVerbose(
                        "RdpCLient: Disconnecting because form is closing");

                    //
                    // NB. This does not trigger an OnDisconnected event.
                    //
                    this.client.Disconnect();

                    OnConnectionClosed(DisconnectReason.FormClosed);
                }
                catch (Exception e)
                {
                    TerminalTraceSource.Log.TraceVerbose(
                        "RdpCLient: Disconnecting failed");

                    OnConnectionFailed(e);
                }
            }

            //
            // Eagerly dispose the control. If we don't do it here,
            // the ActiveX might lock up later.
            //
            this.client.Dispose();
        }

        //---------------------------------------------------------------------
        // Scaling.
        //---------------------------------------------------------------------

        private static readonly uint DefaultScaleFactor = 100;

        /// <summary>
        /// Valid values according to MSDN.
        /// </summary>
        private static readonly uint[] ValidDesktopScaleFactors =
            new uint[] { 500, 400, 300, 250, 200, 175, 150, 125, DefaultScaleFactor };


        /// <summary>
        /// Valid values according to [MS-RDPBCGR].
        /// </summary>
        private static readonly uint[] ValidDeviceScaleFactors =
            new uint[] { 180, 140, DefaultScaleFactor };

        /// <summary>
        /// The scale factor (as a percentage) applied to Windows Desktop
        //  applications. See [MS-RDPBCGR] for details.
        /// </summary>
        internal uint DesktopScaleFactor
        {
            get
            {
                if (!this.EnableDpiScaling)
                {
                    return DefaultScaleFactor;
                }

                //
                // Take local DPI scaling factor and round it
                // to the next lowest valid value.
                //
                var factor = LogicalToDeviceUnits(100);
                return ValidDesktopScaleFactors
                    .SkipWhile(f => f > factor)
                    .First();
            }
        }

        /// <summary>
        /// The scale factor as a percentage is applied to Windows Store apps.
        /// ee [MS-RDPBCGR] for details.
        /// </summary>
        internal uint DeviceScaleFactor
        {
            get
            {
                if (!this.EnableDpiScaling)
                {
                    return DefaultScaleFactor;
                }

                //
                // Take local DPI scaling factor and round it
                // to the next lowest valid value.
                //
                var factor = LogicalToDeviceUnits(100);
                return ValidDeviceScaleFactors
                    .SkipWhile(f => f > factor)
                    .First();
            }
        }

        //---------------------------------------------------------------------
        // Resizing.
        //---------------------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            //
            // Do not resize immediately since there might be another resize
            // event coming in a few milliseconds. 
            //
            this.deferResize.Schedule();
        }

        private void PerformDeferredResize(IDeferredCallbackContext context)
        {
            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                if (this.client.Size == this.Size)
                {
                    //
                    // Nothing to do here.
                    //
                }
                else if (!this.Visible)
                {
                    //
                    // Form is closing, better not touch anything.
                    //
                }
                else if (
                    fullScreenForm != null &&
                    fullScreenForm.WindowState == FormWindowState.Minimized)
                {
                    //
                    // During a restore, we might receive a request to resize
                    // to normal size. We must ignore that.
                    //
                }
                else if (this.State == ConnectionState.NotConnected)
                {
                    //
                    // Resize control only, no RDP involved yet.
                    //
                    this.client.Size = this.Size;
                }
                else if (this.State == ConnectionState.LoggedOn)
                {
                    //
                    // It's safe to resize in this state.
                    //
                    DangerousResizeClient(this.Size);
                }
                else if (
                    this.State == ConnectionState.Connecting ||
                    this.State == ConnectionState.Connected)
                {
                    //
                    // It's not safe to resize now, but it will
                    // be once we're connected. So try again later.
                    //
                    context.Defer();
                }
            }
        }

        private void DangerousResizeClient(Size newSize)
        {
            if (this.Size.Width == 0 || this.Size.Height == 0)
            {
                //
                // Probably the window is being minimized. Ignore
                // that event since it merely causes stress on the
                // RDP control.
                //
                return;
            }

            Debug.Assert(!this.client.IsDisposed);

            //
            // First, resize the control.
            //
            // NB. newSize might be different from this.Size if we're in
            // full-screen mode.
            //
            this.client.Size = newSize;

            //
            // Resize the session.
            //
            try
            {
                //
                // Try to adjust settings without reconnecting - this  works when
                //
                // (1) The server is running 2012R2 or newer
                // (2) The logon process has completed.
                //
                this.client.UpdateSessionDisplaySettings(
                    (uint)newSize.Width,
                    (uint)newSize.Height,
                    (uint)newSize.Width,
                    (uint)newSize.Height,
                    0,  // Landscape
                    this.DesktopScaleFactor,
                    this.DeviceScaleFactor);
            }
            catch (COMException e) when (e.HResult == (int)HRESULT.E_UNEXPECTED)
            {
                TerminalTraceSource.Log.TraceWarning(
                    "Adjusting desktop size (w/o) reconnect failed.");

                //
                // Revert to classic, reconnect-based resizing.
                //
                base.OnBeforeConnect();
                this.client.Reconnect((uint)newSize.Width, (uint)newSize.Height);
            }
        }

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private void OnRdpFatalError(
            object sender,
            IMsTscAxEvents_OnFatalErrorEvent args)
        {
            using (TerminalTraceSource.Log.TraceMethod().WithParameters(args.errorCode))
            {
                //
                // Make sure to leave full-screen mode.
                //
                this.ContainerFullScreen = false;

                OnConnectionFailed(new RdpFatalException(args.errorCode));
            }
        }

        private void OnRdpLogonError(
            object sender,
            IMsTscAxEvents_OnLogonErrorEvent args)
        {
            var e = new RdpLogonException(args.lError);

            using (TerminalTraceSource.Log.TraceMethod().WithParameters(e))
            {
                //
                // Make sure to leave full-screen mode.
                //
                this.ContainerFullScreen = false;

                if (!e.IsIgnorable)
                {
                    OnConnectionFailed(e);
                }
            }
        }

        private void OnRdpLoginComplete(object sender, EventArgs e)
        {
            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                base.OnAfterLogin();
            }
        }

        private void OnRdpDisconnected(
            object sender,
            IMsTscAxEvents_OnDisconnectedEvent args)
        {
            var e = new RdpDisconnectedException(
                    args.discReason,
                    this.client.GetErrorDescription((uint)args.discReason, 0));

            using (TerminalTraceSource.Log.TraceMethod().WithParameters(e.Message))
            {
                //
                // Make sure to leave full-screen mode, otherwise
                // we're showing a dead control full-screen.
                //
                this.ContainerFullScreen = false;

                //
                // Force focus back to main window. 
                // 
                this.MainWindow?.Focus();

                base.OnBeforeDisconnect();

                if (this.State != ConnectionState.Connecting && e.IsTimeout)
                {
                    //
                    // An already-established connection timed out, this is common when
                    // connecting to Windows 10 VMs.
                    //
                    // NB. The same error code can occur during the initial connection,
                    // but then it should be treated as an error.
                    //
                    OnConnectionClosed(DisconnectReason.Timeout);
                }
                else if (e.IsUserDisconnectedRemotely)
                {
                    //
                    // User signed out or clicked Start > Disconnect. 
                    //
                    OnConnectionClosed(DisconnectReason.DisconnectedByUser);
                }
                else if (e.IsUserDisconnectedLocally)
                {
                    //
                    // User clicked X in the connection bar or aborted a reconnect.
                    //
                    OnConnectionClosed(DisconnectReason.DisconnectedByUser);
                }
                else if (e.IsLogonAborted)
                {
                    //
                    // User canceled the logon prompt.
                    //
                    OnConnectionClosed(DisconnectReason.DisconnectedByUser);
                }
                else if (!e.IsIgnorable)
                {
                    OnConnectionFailed(e);
                }

                if (this.reconnectPending)
                {
                    //
                    // This disconnect is part of a "reconnect" sequence.
                    //
                    Connect();
                }
            }
        }

        private void OnRdpConnected(object sender, EventArgs e)
        {
            using (TerminalTraceSource.Log.TraceMethod()
                .WithParameters(this.client.ConnectedStatusText))
            {
                base.OnAfterConnect();
            }
        }

        private void OnRdpConnecting(object sender, EventArgs e)
        {
            Debug.Assert(this.State == ConnectionState.Connecting);

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            { }
        }

        private void OnRdpAuthenticationWarningDisplayed(object sender, EventArgs _)
        {
            Debug.Assert(this.State == ConnectionState.Connecting);

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.ServerAuthenticationWarningDisplayed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnRdpWarning(
            object sender,
            IMsTscAxEvents_OnWarningEvent args)
        {
            using (TerminalTraceSource.Log.TraceMethod().WithParameters(args.warningCode))
            { }
        }

        private void OnRdpAutoReconnecting2(
            object sender,
            IMsTscAxEvents_OnAutoReconnecting2Event args)
        {
            Debug.Assert(
                this.State == ConnectionState.Connecting ||
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var e = new RdpDisconnectedException(
                    args.disconnectReason,
                    this.client.GetErrorDescription((uint)args.disconnectReason, 0));

                TerminalTraceSource.Log.TraceVerbose(
                    "Reconnect attempt {0}/{1} - {2} - {3}",
                    args.attemptCount,
                    args.maxAttemptCount,
                    e.Message,
                    args.networkAvailable);

                if (args.networkAvailable)
                {
                    //
                    // The control is about to connect again.
                    //
                    base.OnBeforeConnect();
                }
                else
                {
                    //
                    // We're now in a limbo state in which the control
                    // might try to connect again, but it might also
                    // be stuck showing a message that the network 
                    // has been lost. If the user cancels, then
                    // the Disconnect procedure is initiated.
                    //
                    // Either way, it's best to leave the state
                    // as is to avoid becoming stuck in Connecting state.
                    //
                }
            }
        }

        private void OnRdpAutoReconnected(object sender, EventArgs e)
        {
            Debug.Assert(this.State == ConnectionState.Connecting);

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                base.OnAfterLogin();
            }
        }

        private void OnRdpFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {
            Debug.Assert(this.MainWindow != null);

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                //
                // Release focus and move it to the main window. This ensures
                // that any other shortcuts start applying again.
                //
                this.MainWindow?.Focus();
            }
        }

        private void OnRdpRemoteDesktopSizeChange(
            object sender,
            IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
        {
            Debug.Assert(
                this.State == ConnectionState.Connecting ||
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            { }
        }

        private void OnRdpServiceMessageReceived(
            object sender,
            IMsTscAxEvents_OnServiceMessageReceivedEvent e)
        {
            using (TerminalTraceSource.Log.TraceMethod().WithParameters(e.serviceMessage))
            { }
        }

        private void OnRdpRequestGoFullScreen(object sender, EventArgs e)
        {
            Debug.Assert(
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            if (this.fullScreenContext == null)
            {
                //
                // Request was initiated by shortcut, not from the
                // application. Use a default context.
                //
                this.fullScreenContext = new FullScreenContext(null);
            }

            this.ContainerFullScreen = true;
        }

        private void OnRdpRequestLeaveFullScreen(object sender, EventArgs e)
        {
            Debug.Assert(
                this.State == ConnectionState.LoggedOn ||
                (this.State == ConnectionState.Connecting && !this.ContainerFullScreen));

            this.ContainerFullScreen = false;
        }

        private void OnRdpRequestContainerMinimize(object sender, EventArgs e)
        {
            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                Debug.Assert(fullScreenForm != null);

                //
                // Minimize this window.
                //
                fullScreenForm!.WindowState = FormWindowState.Minimized;

                //
                // Minimize the main form (which is still running in the 
                // back)
                //
                if (this.MainWindow != null)
                {
                    this.MainWindow.WindowState = FormWindowState.Minimized;
                }
            }
        }

        //---------------------------------------------------------------------
        // Public methods.
        //---------------------------------------------------------------------

        /// <summary>
        /// Outmost window that can be used to pass the focus to. 
        /// In case of an MDI environment, this should be the outmost 
        /// window, not the direct parent window.
        /// </summary>
        [Browsable(false)]
        public Form? MainWindow { get; set; }

        /// <summary>
        /// Connect to server.
        /// </summary>
        /// <remarks>Errors are reported via events, not exceptions</remarks>
        public override void Connect()
        {
            Debug.Assert(!this.client.IsDisposed);

            ExpectState(ConnectionState.NotConnected);
            Precondition.ExpectNotEmpty(this.Server, nameof(this.Server));
            Precondition.ExpectNotEmpty(this.Server, nameof(this.Username));

            //
            // Clear reconnect flag so that it doesn't cause multiple
            // reconnects in sequence.
            //
            this.reconnectPending = false;

            if (this.EnableWebAuthnRedirection)
            {
                //
                // Load WebAuthn plugin. This requires at least 22H2, both
                // client- and server-side.
                //
                // Once the plugin DLL is loaded, WebAuthn redirection is
                // enabled automatically unless there's a client- or server-
                // side policy that disabled WebAuthn redirection.
                //
                // See also:
                // https://interopevents.blob.core.windows.net/events/2023/RDP%20IO%20Lab/ \
                // PDF/DavidBelanger_Authentication%20-%20RDP%20IO%20Labs%20March%202023.pdf
                //
                var webauthnPluginPath = Path.Combine(Environment.SystemDirectory, WebAuthnPlugin);
                if (File.Exists(webauthnPluginPath))
                {
                    try
                    {
                        this.clientAdvancedSettings.PluginDlls = webauthnPluginPath;
                        TerminalTraceSource.Log.TraceInformation(
                            "Loaded RDP plugin {0}", webauthnPluginPath);
                    }
                    catch (Exception e)
                    {
                        TerminalTraceSource.Log.TraceWarning(
                            "Loading RDP plugin {0} failed: {1}",
                            webauthnPluginPath,
                            e.Message);
                    }
                }
            }

            //
            // Adjust DPI settings of remote session.
            //
            // NB. Values must be uint-typed.
            // NB. The factors must be reapplied when the session
            //     is resized.
            //
            if (this.DesktopScaleFactor is var desktopFactor &&
                desktopFactor != DefaultScaleFactor)
            {
                this.clientExtendedSettings.set_Property(
                    "DesktopScaleFactor",
                    desktopFactor);
            }

            if (this.DeviceScaleFactor is var deviceFactor &&
                deviceFactor != DefaultScaleFactor)
            {
                this.clientExtendedSettings.set_Property(
                    "DeviceScaleFactor",
                    deviceFactor);
            }

            //
            // Reset state in case we're connecting for the second time.
            //
            base.OnBeforeConnect();
            this.client.FullScreen = false;
            this.client.Size = this.Size;
            this.client.DesktopHeight = this.Size.Height;
            this.client.DesktopWidth = this.Size.Width;

            try
            {
                this.client.Connect();
            }
            catch (Exception e)
            {
                this.OnConnectionFailed(e);
            }
        }

        /// <summary>
        /// Disconnect and connect again.
        /// </summary>
        /// <remarks>Errors are reported via events, not exceptions</remarks>
        public void Reconnect()
        {
            Debug.Assert(this.State == ConnectionState.LoggedOn);
            if (this.State != ConnectionState.LoggedOn)
            {
                return;
            }

            //
            // Set reconnect flag and disconnect. This triggers
            // an Disconnected event, which in turn should trigger
            // a new connection attempt.
            //
            this.reconnectPending = true;
            this.client.Disconnect();
        }

        //---------------------------------------------------------------------
        // Synthetic input.
        //---------------------------------------------------------------------

        private void SendScanCodes(
            short[] keyUp,
            int[] keyData)
        {
            //
            // NB. It's crucial to set the focus here again, otherwise
            // key chords don't work.
            //
            this.client.Focus();

            //
            // NB. The tlbimp-generated IMsRdpClientNonScriptable5 uses
            // a signature for SendKeys that doesn't work with C-style arrays.
            // Therefore, we use a manually fixed version of IMsRdpClientNonScriptable5.
            //
            // According to MSDN, scan codes need to be sent in
            // "WM_KEYDOWN lParam format", but that seems incorrect. Instead,
            // the API expects raw scan codes.
            //

            var obj = (IMsRdpClientNonScriptable5_SendKeys)this.client.GetOcx();
            obj.SendKeys(keyData.Length, keyUp, keyData);
        }

        /// <summary>
        /// Send a sequence of virtual keys. Keys may use modifiers.
        /// </summary>
        private void SendVirtualKey(Keys virtualKey)
        {
            var keyboard = KeyboardLayout.Current;

            //
            // The RDP control sometimes swallows the first key combination
            // that is sent. So start by a harmless ESC.
            //
            if (this.keysSent++ == 0)
            {
                var escScanCode = keyboard.ToScanCodes(Keys.Escape).First();
                SendScanCodes(
                     new short[] { 0 },
                     new int[] { (int)escScanCode });
            }

            //
            // Convert virtual key code (which might contain modifiers)
            // into a sequence of scan codes.
            //
            var scanCodes = keyboard
                .ToScanCodes(virtualKey)
                .Select(c => (int)c)
                .ToArray();

            //
            // If the key has modifers other than Shift, we have to send
            // separate DOWN and UP keystrokes for each scan code.
            //
            // Curiously, we must not do this for "normal" characters 
            // (single scan code), otherwise we end up with duplicate
            // characters.
            //

            short[] keyUp;
            int[] keyData;

            if ((virtualKey & (Keys.Control | Keys.Alt)) != 0 &&
                scanCodes.Length > 1)
            {
                //
                // Convert scan codes into simulated DOWN- and UP- key 
                // presses.
                //
                keyUp = new short[scanCodes.Length * 2];
                keyData = new int[scanCodes.Length * 2];

                for (var i = 0; i < scanCodes.Length; i++)
                {
                    //
                    // Generate DOWN key presses.
                    //
                    keyUp[i] = VariantBool.False;
                    keyData[i] = scanCodes[i];

                    //
                    // Generate UP key presses (in reverse order).
                    //
                    keyUp[keyUp.Length - 1 - i] = VariantBool.True;
                    keyData[keyData.Length - 1 - i] = scanCodes[i];
                }
            }
            else
            {
                //
                // Generate DOWN key press only.
                //
                keyUp = new short[scanCodes.Length]; // DOWN.
                keyData = scanCodes;
            }

            SendScanCodes(keyUp, keyData);
        }

        /// <summary>
        /// Simulate a key chord to show the security screen.
        /// </summary>
        public void ShowSecurityScreen()
        {
            Debug.Assert(this.State == ConnectionState.LoggedOn);
            if (this.State != ConnectionState.LoggedOn)
            {
                return;
            }

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                SendVirtualKey(Keys.Control | Keys.Alt | Keys.Delete);
            }
        }

        /// <summary>
        /// Simulate a key chord toopen task manager.
        /// </summary>
        public void ShowTaskManager()
        {
            Debug.Assert(this.State == ConnectionState.LoggedOn);
            if (this.State != ConnectionState.LoggedOn)
            {
                return;
            }

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                SendVirtualKey(Keys.Control | Keys.Shift | Keys.Escape);
            }
        }

        /// <summary>
        /// Log off user (as opposed to disconnecting the session).
        /// </summary>
        /// <remarks>
        /// There's no API to log off the user programatically, so
        /// we have to make a best-effort attempt of initiating a logoff
        /// by sending keystrokes.
        /// </remarks>
        public void Logoff()
        {
            Debug.Assert(this.State == ConnectionState.LoggedOn);
            if (this.State != ConnectionState.LoggedOn)
            {
                return;
            }

            using (TerminalTraceSource.Log.TraceMethod().WithoutParameters())
            {
                SendVirtualKey(Keys.Control | Keys.Alt | Keys.Delete);

                //
                // We have to wait a bit before we can send the next 
                // keys.
                //
                DeferredCallback? deferredCallback = null;
                deferredCallback = new DeferredCallback(
                    ctx =>
                    {
                        // 
                        // Select the second option on the list.
                        //
                        // NB. Navigating to the second item is slightly
                        // better than sending an Alt+S, because accelerators
                        // might vary by display language.
                        //
                        try
                        {
                            SendVirtualKey(Keys.Down);
                            SendVirtualKey(Keys.Enter);
                        }
                        catch
                        { }

                        deferredCallback?.Dispose();
                    },
                    TimeSpan.FromSeconds(1));
                deferredCallback.Schedule();
            }
        }

        /// <summary>
        /// Simulate key strokes to send a piece of text.
        /// </summary>
        public override void SendText(string text)
        {
            Debug.Assert(this.State == ConnectionState.LoggedOn);
            if (this.State != ConnectionState.LoggedOn)
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var keyboardLayout = KeyboardLayout.Current;

            for (var i = 0; i < text.Length && i < MaxSendStringLength; i++)
            {
                var ch = text[i];
                if (ch == '\r' && i < text.Length - 2 && text[i + 1] == '\n')
                {
                    //
                    // Ignore a CR if it's part of a CRLF.
                    //
                    continue;
                }

                if (keyboardLayout.TryMapVirtualKey(ch, out var vk))
                {
                    //
                    // This is a "mormal" character with a corresponding
                    // virtual key on the current keyboard layout.
                    //
                    SendVirtualKey(vk);
                }
                else
                {
                    //
                    // This is a ligature or any kind of character that
                    // has no corresponding virtual on the current keyboard layout.
                    //
                    // Converting the character to a Alt+0nnn sequence doesn't
                    // work reliably, so we just send a '?'.
                    //
                    if (keyboardLayout.TryMapVirtualKey('?', out var questionMark))
                    {
                        SendVirtualKey(questionMark);
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Full-screen mode.
        //
        // NB. In container-handled mode, setting FullScreen to true..
        //
        // - calls the OnRequestGoFullScreen event,
        // - shows the connection bar (if enabled)
        // - changes hotkeys
        //
        // However, it does not resize the control automatically.
        //---------------------------------------------------------------------

        private FullScreenContext? fullScreenContext = null;
        private static Form? fullScreenForm = null;

        private static void MoveControls(Control source, Control target)
        {
            var controls = new Control[source.Controls.Count];
            source.Controls.CopyTo(controls, 0);
            source.Controls.Clear();
            target.Controls.AddRange(controls);

            Debug.Assert(source.Controls.Count == 0);
        }

        /// <summary>
        /// Gets or sets full-scren mode for the containing window.
        /// 
        /// This property should only be changed from within RDP
        /// callbacks.
        /// </summary>
        internal bool ContainerFullScreen
        {
            get => fullScreenForm != null && fullScreenForm.Visible;
            private set
            {
                if (value == this.ContainerFullScreen)
                {
                    //
                    // Nothing to do.
                    //
                    return;
                }
                else if (value)
                {
                    Debug.Assert(this.fullScreenContext != null);
                    Debug.Assert(this.CurrentParentForm != null);

                    //
                    // Enter full-screen.
                    //
                    // To provide a true full screen experience, we create a
                    // new window and temporarily move all controls to this window.
                    //
                    // NB. The RDP ActiveX has some quirk where the connection bar
                    // disappears when you go full-screen a second time and the
                    // hosting window is different from the first time.
                    // By using a single/static window and keeping it around
                    // after first use, we ensure that the form is always the
                    // same, thus circumventing the quirk.
                    //
                    if (fullScreenForm == null)
                    {
                        //
                        // First time to go full screen, create the
                        // full-screen window.
                        //
                        fullScreenForm = new Form()
                        {
                            Icon = this.CurrentParentForm!.Icon,
                            FormBorderStyle = FormBorderStyle.None,
                            StartPosition = FormStartPosition.Manual,
                            TopMost = true,
                            ShowInTaskbar = false
                        };
                    }

                    //
                    // Use current screen bounds if none specified.
                    //
                    fullScreenForm.Bounds =
                        this.fullScreenContext!.Bounds ?? Screen.FromControl(this).Bounds;

                    MoveControls(this, fullScreenForm);

                    //
                    // Set the parent to the window we want to bring to the front
                    // when the user clicks minimize on the conection bar.
                    //
                    Debug.Assert(this.MainWindow != null);
                    fullScreenForm.Show(this.MainWindow);

                    //
                    // Resize to fit new form.
                    //
                    DangerousResizeClient(fullScreenForm.Size);
                }
                else
                {
                    Debug.Assert(fullScreenForm != null);

                    //
                    // Return from full-screen.
                    //

                    MoveControls(fullScreenForm!, this);

                    //
                    // Only hide the window, we might need it again.
                    //
                    fullScreenForm!.Hide();

                    //
                    // Resize back to original size.
                    //
                    this.deferResize.Schedule();

                    this.fullScreenContext = null;
                    Debug.Assert(!this.ContainerFullScreen);
                }
            }
        }

        /// <summary>
        /// Check if any instance of this control currently uses
        /// full-screen mode.
        /// </summary>
        private static bool IsFullScreenFormVisible
        {
            get => fullScreenForm != null && fullScreenForm.Visible;
        }

        /// <summary>
        /// Check if the client is currently in full-screen mode.
        /// </summary>
        [Browsable(false)]
        public override bool IsFullScreen
        {
            get
            {
                try
                {
                    return this.client.FullScreen;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if the current state is suitable for entering
        /// full-screen mode.
        /// </summary>
        [Browsable(false)]
        public bool CanEnterFullScreen
        {
            get => this.State == ConnectionState.LoggedOn && !IsFullScreenFormVisible;
        }

        /// <summary>
        /// Enter full screen mode.
        /// </summary>
        public bool TryEnterFullScreen(Rectangle? customBounds)
        {
            if (this.MainWindow == null)
            {
                throw new InvalidOperationException("Main window must be set");
            }

            if (!this.CanEnterFullScreen)
            {
                return false;
            }

            this.fullScreenContext = new FullScreenContext(customBounds);

            this.client.FullScreenTitle = this.ConnectionBarText;
            this.client.FullScreen = true;

            return true;
        }

        /// <summary>
        /// Leave full-screen mode.
        /// </summary>
        public bool TryLeaveFullScreen()
        {
            if (!this.IsFullScreen)
            {
                return false;
            }

            this.client.FullScreen = false;
            return true;
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class FullScreenContext
        {
            public Rectangle? Bounds { get; }

            public FullScreenContext(Rectangle? bounds)
            {
                this.Bounds = bounds;
            }
        }
    }
}
