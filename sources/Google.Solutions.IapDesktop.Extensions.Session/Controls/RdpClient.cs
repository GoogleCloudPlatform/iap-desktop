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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using MSTSCLib;
using System.ComponentModel;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls // TODO: reorder methods
{
    /// <summary>
    /// Wrapper control for the native RDP client that implements
    /// a smooth full-screen experience and uses a state machine
    /// to ensure more reliable operation.
    /// </summary>
    public partial class RdpClient : UserControl
    {
        private const string WebAuthnPlugin = "webauthn.dll";

        private readonly Google.Solutions.Tsc.MsRdpClient client;
        private readonly IMsRdpClientNonScriptable5 clientNonScriptable;
        private readonly IMsRdpClientAdvancedSettings6 clientAdvancedSettings;
        private readonly IMsRdpClientSecuredSettings clientSecuredSettings;

        private ConnectionState state = ConnectionState.NotConnected;

        private readonly DeferredCallback deferResize;
        private Form parentForm = null;
        private int keysSent = 0;

        public RdpClient()
        {
            this.client = new Google.Solutions.Tsc.MsRdpClient
            {
                Enabled = true,
                Location = new System.Drawing.Point(0, 0),
                Name = "client",
                Size = new System.Drawing.Size(100, 100),
            };
            this.deferResize = new DeferredCallback(PerformDeferredResize, TimeSpan.FromMilliseconds(200));

            ((System.ComponentModel.ISupportInitialize)(this.client)).BeginInit();
            this.SuspendLayout();

            //
            // Hook up events.
            //
            this.client.OnConnecting += new System.EventHandler(this.OnConnecting);
            this.client.OnConnected += new System.EventHandler(this.OnConnected);
            this.client.OnLoginComplete += new System.EventHandler(this.OnLoginComplete);
            this.client.OnDisconnected += new AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEventHandler(this.OnDisconnected);
            this.client.OnRequestGoFullScreen += new System.EventHandler(this.OnRequestGoFullScreen);
            this.client.OnRequestLeaveFullScreen += new System.EventHandler(this.OnRequestLeaveFullScreen);
            this.client.OnFatalError += new AxMSTSCLib.IMsTscAxEvents_OnFatalErrorEventHandler(this.OnFatalError);
            this.client.OnWarning += new AxMSTSCLib.IMsTscAxEvents_OnWarningEventHandler(this.OnWarning);
            this.client.OnRemoteDesktopSizeChange += new AxMSTSCLib.IMsTscAxEvents_OnRemoteDesktopSizeChangeEventHandler(this.OnRemoteDesktopSizeChange);
            this.client.OnRequestContainerMinimize += new System.EventHandler(this.OnRequestContainerMinimize);
            this.client.OnAuthenticationWarningDisplayed += new System.EventHandler(this.OnAuthenticationWarningDisplayed);
            this.client.OnLogonError += new AxMSTSCLib.IMsTscAxEvents_OnLogonErrorEventHandler(this.OnLogonError);
            this.client.OnFocusReleased += new AxMSTSCLib.IMsTscAxEvents_OnFocusReleasedEventHandler(this.OnFocusReleased);
            this.client.OnServiceMessageReceived += new AxMSTSCLib.IMsTscAxEvents_OnServiceMessageReceivedEventHandler(this.OnServiceMessageReceived);
            this.client.OnAutoReconnected += new System.EventHandler(this.OnAutoReconnected);
            this.client.OnAutoReconnecting2 += new AxMSTSCLib.IMsTscAxEvents_OnAutoReconnecting2EventHandler(this.OnAutoReconnecting2);

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

            //
            // Bitmap persistence consumes vast amounts of memory, so keep
            // it disabled.
            //
            this.clientAdvancedSettings.BitmapPersistence = 0;

            //
            // Let us handle full-screen mode ourselves.
            //
            this.clientAdvancedSettings.ContainerHandledFullScreen = 1;


            this.clientAdvancedSettings.allowBackgroundInput = 1;

            //
            // As a user control, we don't get a FormClosing event,
            // so attach to the parent form.
            //
            this.VisibleChanged += (_, __) =>
            {
                if (this.parentForm == null && FindForm() is Form form)
                {
                    this.parentForm = form;
                    this.parentForm.FormClosing += OnFormClosing;

                    this.client.ContainingControl = this.parentForm;
                }
            };
            this.ParentChanged += (_, __) =>
            {
                if (this.parentForm != null)
                {
                    this.parentForm.FormClosing -= OnFormClosing;
                }

                if (this.Parent?.FindForm() is Form newParent)
                {
                    this.parentForm = newParent;
                    this.parentForm.FormClosing += OnFormClosing;

                    this.client.ContainingControl = this.parentForm;
                }
            };
        }

        //---------------------------------------------------------------------
        // Basic properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Outmost window that can be used to pass the focus to. This
        /// can be the parent window, but doesn't have to be.
        /// </summary>
        public Form MainWindow { get; set; }

        //---------------------------------------------------------------------
        // Closing & disposing.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.client.Dispose();
            this.deferResize.Dispose();
        }

        protected void OnFormClosing(object sender, FormClosingEventArgs args)
        {
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
                ApplicationTraceSource.Log.TraceVerbose(
                    "RemoteDesktopPane: Aborting FormClosing because control is in connecting");

                args.Cancel = true;
                return;
            }
            else if (
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn)
            {
                try
                {
                    ApplicationTraceSource.Log.TraceVerbose(
                        "RemoteDesktopPane: Disconnecting because form is closing");

                    //
                    // NB. This does not trigger an OnDisconnected event.
                    //
                    this.client.Disconnect();

                    this.ConnectionClosed?.Invoke(
                        this,
                        new ConnectionClosedEventArgs(DisconnectReason.FormClosed));

                    //
                    // Eagerly dispose the control. If we don't do it here,
                    // we risk a deadlock later.
                    //
                    this.client.Dispose();

                    this.State = ConnectionState.NotConnected;
                }
                catch (Exception e)
                {
                    ApplicationTraceSource.Log.TraceVerbose(
                        "RemoteDesktopPane: Disconnecting failed");

                    this.ConnectionFailed?.Invoke(this, new ExceptionEventArgs(e));
                }
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
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
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
                else if (fullScreenForm != null && fullScreenForm.WindowState == FormWindowState.Minimized)
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
                    //
                    DangerousResizeClient(this.Size);
                }
                else if (
                    this.State == ConnectionState.Connecting ||
                    this.state == ConnectionState.Connected)
                {
                    //
                    // It's not size to resize now, but it will
                    // be in a bit. So try again later.
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
                // Try to adjust settings without reconnecting - this only works when
                // (1) The server is running 2012R2 or newer
                // (2) The logon process has completed.
                //
                this.client.UpdateSessionDisplaySettings(
                    (uint)newSize.Width,
                    (uint)newSize.Height,
                    (uint)newSize.Width,
                    (uint)newSize.Height,
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
                this.State = ConnectionState.Connecting;
                this.client.Reconnect((uint)newSize.Width, (uint)newSize.Height);
            }
        }

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private void OnFatalError(
            object sender,
            IMsTscAxEvents_OnFatalErrorEvent args)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(args.errorCode))
            {
                //
                // Make sure to leave full-screen mode.
                //
                this.ContainerFullScreen = false;

                this.ConnectionFailed?.Invoke(
                    this,
                    new ExceptionEventArgs(new RdpFatalException(args.errorCode)));

                this.State = ConnectionState.NotConnected;
            }
        }

        private void OnLogonError(
            object sender,
            IMsTscAxEvents_OnLogonErrorEvent args)
        {
            var e = new RdpLogonException(args.lError);

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e))
            {
                //
                // Make sure to leave full-screen mode.
                //
                this.ContainerFullScreen = false;

                if (!e.IsIgnorable)
                {
                    this.ConnectionFailed?.Invoke(
                        this,
                        new ExceptionEventArgs(e));

                    this.State = ConnectionState.NotConnected;
                }
            }
        }

        private void OnLoginComplete(object sender, EventArgs e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.State = ConnectionState.LoggedOn;
            }
        }

        private void OnDisconnected(
            object sender,
            IMsTscAxEvents_OnDisconnectedEvent args)
        {
            var e = new RdpDisconnectedException(
                    args.discReason,
                    this.client.GetErrorDescription((uint)args.discReason, 0));

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e.Message))
            {
                //
                // Make sure to leave full-screen mode, otherwise
                // we're showing a dead control full-screen.
                //
                this.ContainerFullScreen = false;

                //
                // Force focus back to main window. 
                // 
                this.MainWindow.Focus();

                this.State = ConnectionState.Disconnecting;

                if (this.State != ConnectionState.Connecting && e.IsTimeout)
                {
                    //
                    // An already-established connection timed out, this is common when
                    // connecting to Windows 10 VMs.
                    //
                    // NB. The same error code can occur during the initial connection,
                    // but then it should be treated as an error.
                    //
                    this.ConnectionClosed?.Invoke(
                        this,
                        new ConnectionClosedEventArgs(DisconnectReason.Timeout));
                }
                else if (e.IsUserDisconnectedRemotely)
                {
                    //
                    // User signed out or clicked Start > Disconnect. 
                    //
                    this.ConnectionClosed?.Invoke(
                        this,
                        new ConnectionClosedEventArgs(DisconnectReason.DisconnectedByUser));
                }
                else if (e.IsUserDisconnectedLocally)
                {
                    //
                    // User clicked X in the connection bar or aborted a reconnect
                    //
                    this.ConnectionClosed?.Invoke(
                        this,
                        new ConnectionClosedEventArgs(DisconnectReason.DisconnectedByUser));
                }
                else if (!e.IsIgnorable)
                {
                    this.ConnectionFailed?.Invoke(this, new ExceptionEventArgs(e));
                }

                this.State = ConnectionState.NotConnected;
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            using (ApplicationTraceSource.Log.TraceMethod()
                .WithParameters(this.client.ConnectedStatusText))
            {
                this.State = ConnectionState.Connected;
            }
        }

        private void OnConnecting(object sender, EventArgs e)
        {
            Debug.Assert(this.State == ConnectionState.Connecting);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            { }
        }

        private void OnAuthenticationWarningDisplayed(object sender, EventArgs _)
        {
            Debug.Assert(this.State == ConnectionState.Connecting);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.ServerAuthenticationWarningDisplayed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnWarning(
            object sender,
            IMsTscAxEvents_OnWarningEvent args)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(args.warningCode))
            { }
        }

        private void OnAutoReconnecting2(
            object sender,
            IMsTscAxEvents_OnAutoReconnecting2Event args)
        {
            Debug.Assert(
                this.State == ConnectionState.Connecting ||
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var e = new RdpDisconnectedException(
                    args.disconnectReason,
                    this.client.GetErrorDescription((uint)args.disconnectReason, 0));

                ApplicationTraceSource.Log.TraceVerbose(
                    "Reconnect attempt {0}/{1} - {2} - {3}",
                    args.attemptCount,
                    args.maxAttemptCount,
                    e.Message,
                    args.networkAvailable);


                this.State = ConnectionState.Connecting;
            }
        }

        private void OnAutoReconnected(object sender, EventArgs e)
        {
            Debug.Assert(this.State == ConnectionState.Connecting);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.State = ConnectionState.LoggedOn;
            }
        }

        private void OnFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {
            Debug.Assert(this.MainWindow != null);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                //
                // Release focus and move it to the main window. This ensures
                // that any other shortcuts start applying again.
                //
                this.MainWindow.Focus();
            }
        }

        private void OnRemoteDesktopSizeChange(
            object sender,
            IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
        {
            Debug.Assert(
                this.State == ConnectionState.Connecting ||
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            { }
        }

        private void OnServiceMessageReceived(
            object sender,
            IMsTscAxEvents_OnServiceMessageReceivedEvent e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e.serviceMessage))
            { }
        }

        private void OnRequestGoFullScreen(object sender, EventArgs e)
        {
            Debug.Assert(
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            this.ContainerFullScreen = true;
        }

        private void OnRequestLeaveFullScreen(object sender, EventArgs e)
        {
            Debug.Assert(
                this.State == ConnectionState.LoggedOn ||
                (this.State == ConnectionState.Connecting && !this.ContainerFullScreen));

            this.ContainerFullScreen = false;
        }

        private void OnRequestContainerMinimize(object sender, EventArgs e)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                //
                // Minimize this window.
                //
                fullScreenForm.WindowState = FormWindowState.Minimized;

                //
                // Minimize the main form (which is still running in the 
                // back)
                //
                this.MainWindow.WindowState = FormWindowState.Minimized;
            }
        }

        //---------------------------------------------------------------------
        // Public methods.
        //---------------------------------------------------------------------

        public void Connect()
        {
            Debug.Assert(!this.client.IsDisposed);

            ExpectState(ConnectionState.NotConnected);
            Precondition.ExpectNotEmpty(this.Server, nameof(this.Server));
            Precondition.ExpectNotEmpty(this.Server, nameof(this.Username));

            if (this.EnableWebAuthnRedirection)
            {
                //
                // Load WebAuthn plugin. This requires at least 22H2, both client- and server-side.
                //
                // Once the plugin DLL is loaded, WebAuthn redirection is enabled automatically
                // unless there's a client- or server-side policy that disabled WebAuthn redirection.
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

            //
            // Reset state in case we're connecting for the second time.
            //
            this.State = ConnectionState.Connecting;
            this.client.FullScreen = false;
            this.client.Size = this.Size;
            this.client.DesktopHeight = this.Size.Height;
            this.client.DesktopWidth = this.Size.Width;

            this.client.Connect();
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

        private FullScreenContext fullScreenContext = null;
        private static Form fullScreenForm = null;

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
        /// This property should only be changes from within RDP
        /// callbacks.
        /// </summary>
        public bool ContainerFullScreen
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
                            Icon = this.parentForm.Icon,
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
                        this.fullScreenContext.Bounds ?? Screen.FromControl(this).Bounds;

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
                    //
                    // Return from full-screen.
                    //

                    MoveControls(fullScreenForm, this);

                    //
                    // Only hide the window, we might need it again.
                    //
                    fullScreenForm.Hide();

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
        /// Check if the client is currently in full-screen mode.
        /// </summary>
        public bool IsFullScreen
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
        public bool CanEnterFullScreen
        {
            get => this.State == ConnectionState.LoggedOn;
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


        public void ShowSecurityScreen()
        {
            Debug.Assert(this.State == ConnectionState.LoggedOn);

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
            Debug.Assert(this.State == ConnectionState.LoggedOn);

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
            Debug.Assert(this.State == ConnectionState.LoggedOn);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                this.client.Focus();

                var nonScriptable = (IMsRdpClientNonScriptable5)this.client.GetOcx();

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
        // State tracking.
        //---------------------------------------------------------------------

        /// <summary>
        /// Connection state has changed.
        /// </summary>
        public event EventHandler StateChanged;

        /// <summary>
        /// Connection closed abnormally.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ConnectionFailed;


        /// <summary>
        /// Connection closed normally.
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// The server authentication warning has been displayed.
        /// </summary>
        internal event EventHandler ServerAuthenticationWarningDisplayed;

        private void ExpectState(ConnectionState expectedState)
        {
            if (this.State != expectedState)
            {
                throw new InvalidOperationException($"Operation is not allowed in state {this.State}");
            }
        }

        /// <summary>
        /// Current state of the connection.
        /// </summary>
        [Browsable(true)]
        public ConnectionState State
        {
            get => this.state;
            private set
            {
                Debug.Assert(!this.InvokeRequired);
                if (this.state != value)
                {
                    this.state = value;
                    this.StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public enum ConnectionState
        {
            /// <summary>
            /// Client not connected yet or an existing connection has 
            /// been lost.
            /// </summary>
            NotConnected,

            /// <summary>
            /// Client is in the process of connecting.
            /// </summary>
            Connecting,

            /// <summary>
            /// Client connected, but user log on hasn't completed yet.
            /// </summary>
            Connected,

            Disconnecting,

            /// <summary>
            /// User logged on, session is ready to use.
            /// </summary>
            LoggedOn,
        }

        internal async Task AwaitStateAsync(ConnectionState state)
        {
            Debug.Assert(!this.InvokeRequired);

            if (this.State == state)
            {
                return;
            }

            var completionSource = new TaskCompletionSource<ConnectionState>();

            EventHandler callback = null;
            callback = (object sender, EventArgs args) =>
            {
                if (this.State == state)
                {
                    this.StateChanged -= callback;
                    completionSource.SetResult(this.State);
                }
            };

            this.StateChanged += callback;
            
            await completionSource
                .Task
                .ConfigureAwait(true);

            //
            // There might be a resize pending.
            //
            await this.deferResize
                .WaitForCompletionAsync()
                .ConfigureAwait(true);
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

        public class ConnectionClosedEventArgs : EventArgs
        {
            internal ConnectionClosedEventArgs(DisconnectReason reason)
            {
                this.Reason = reason;
            }

            public DisconnectReason Reason { get; }
        }

        public enum DisconnectReason
        {
            Timeout,
            DisconnectedByUser,
            FormClosed
        }
    }
}
