using AxMSTSCLib;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.Tsc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using MSTSCLib;
using Google.Solutions.Mvvm.Shell;
using System.ComponentModel;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    public class RdpClient : UserControl
    {
        private const string WebAuthnPlugin = "webauthn.dll";

        private readonly Google.Solutions.Tsc.MsRdpClient client;
        private readonly IMsRdpClientNonScriptable5 clientNonScriptable;
        private readonly IMsRdpClientAdvancedSettings6 clientAdvancedSettings;
        private readonly IMsRdpClientSecuredSettings clientSecuredSettings;

        private ConnectionState state = ConnectionState.NotConnected;

        private readonly DeferredCallback deferResize;
        private Form parentForm = null;

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

            //
            // As a user control, we don't get a FormClosing event,
            // so attach to the parent form.
            //
            this.VisibleChanged += (_, __) =>
            {
                if (this.parentForm == null && FindForm() is Form form)
                {
                    this.parentForm = form;
                    form.FormClosing += OnFormClosing;
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.client.Dispose();
            this.deferResize.Dispose();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            //
            // Do not resize immediately since there might be another resize
            // event coming in a few milliseconds. 
            //
            this.deferResize.Invoke();
        }

        protected void OnFormClosing(object sender, FormClosingEventArgs args)
        {
            if (this.State == ConnectionState.Connecting)
            {
                //
                // Veto this event as it might cause the ActiveX to crash.
                //
                ApplicationTraceSource.Log.TraceVerbose(
                    "RemoteDesktopPane: Aborting FormClosing because control is in connecting");

                args.Cancel = true;
                return;
            }
            else if (this.State == ConnectionState.Connected)
            {
                try
                {
                    ApplicationTraceSource.Log.TraceVerbose(
                        "RemoteDesktopPane: Disconnecting because form is closing");

                    //
                    // NB. This does not trigger an OnDisconnected event.
                    //
                    this.client.Disconnect();
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

        private void PerformDeferredResize(DeferredCallback cb)
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
                    cb.Invoke();
                }
            }
        }

        private void DangerousResizeClient(Size newSize)
        {
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
            this.State = ConnectionState.NotConnected;
            this.ConnectionFailed?.Invoke(
                this,
                new ExceptionEventArgs(new RdpFatalException(args.errorCode)));
        }

        private void OnLogonError(
            object sender,
            IMsTscAxEvents_OnLogonErrorEvent args)
        {
            var e = new RdpLogonException(args.lError);
            if (!e.IsIgnorable)
            {
                this.State = ConnectionState.NotConnected;
                this.ConnectionFailed?.Invoke(
                    this,
                    new ExceptionEventArgs(e));
            }
        }

        private void OnLoginComplete(object sender, EventArgs e)
        {
            this.State = ConnectionState.LoggedOn; ;
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
                this.State = ConnectionState.NotConnected;

                //
                // Make sure to leave full-screen mode, otherwise
                // we're showing a dead control full-screen.
                //
                this.ContainerFullScreen = false;

                //
                // Force focus back to main window. 
                // 
                this.MainWindow.Focus();


                

                // TODO: Port rest

                this.ConnectionFailed?.Invoke(this, new ExceptionEventArgs(e));
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            this.State = ConnectionState.Connected;

            using (ApplicationTraceSource.Log.TraceMethod()
                .WithParameters(this.client.ConnectedStatusText))
            {

            }
            // TODO: Port rest
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
                this.AuthenticationWarningDisplayed?.Invoke(this, EventArgs.Empty);
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
            //TODO: port rest


            this.State = ConnectionState.LoggedOn;
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
        // Basic properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Outmost window that can be used to pass the focus to. This
        /// can be the parent window, but doesn't have to be.
        /// </summary>
        public Form MainWindow { get; set; }

        //---------------------------------------------------------------------
        // Connection properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Server to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Server
        {
            get => this.client.Server;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.Server = value;
            }
        }

        /// <summary>
        /// Server port to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ushort ServerPort
        {
            get => (ushort)this.client.AdvancedSettings7.RDPPort;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RDPPort = value;
            }
        }

        /// <summary>
        /// NetBIOS domain to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Domain
        {
            get => this.client.Domain;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.Domain = value;
            }
        }

        /// <summary>
        /// UPN or username to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Username
        {
            get => this.client.UserName;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.UserName = value;
            }
        }

        /// <summary>
        /// Password to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Password
        {
            get => "*";
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.ClearTextPassword = value;
            }
        }

        /// <summary>
        /// Enable Network Level Authentication (CredSSP).
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableNetworkLevelAuthentication
        {
            get => this.clientAdvancedSettings.EnableCredSspSupport;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.EnableCredSspSupport = value;

                if (!value)
                {
                    //
                    // To disable NLA, we must enable server authentication.
                    //
                    this.clientAdvancedSettings.AuthenticationLevel = 2;
                }
            }
        }

        /// <summary>
        /// Server authentication level.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public uint ServerAuthenticationLevel
        {
            get => this.clientAdvancedSettings.AuthenticationLevel;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.AuthenticationLevel = value;
            }
        }

        /// <summary>
        /// Show a credential prompt if invalid credentials were passed.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableCredentialPrompt
        {
            get => this.clientNonScriptable.AllowPromptingForCredentials;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientNonScriptable.AllowPromptingForCredentials = value;
            }
        }

        /// <summary>
        /// Connection Timeout, including time allowed to enter user credentials.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan ConnectionTimeout
        {
            get => TimeSpan.FromSeconds(this.clientAdvancedSettings.singleConnectionTimeout);
            set
            {
                ExpectState(ConnectionState.NotConnected);

                //
                // Apply timeouts. Note that the control might take
                // about twice the configured timeout before sending a 
                // OnDisconnected event.
                //
                this.clientAdvancedSettings.singleConnectionTimeout = (int)value.TotalSeconds;
                this.clientAdvancedSettings.overallConnectionTimeout = (int)value.TotalSeconds;
            }
        }

        //---------------------------------------------------------------------
        // Connection Bar properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Display the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableConnectionBar
        {
            get => this.clientAdvancedSettings.DisplayConnectionBar;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.DisplayConnectionBar = value;
            }
        }

        /// <summary>
        /// Display a minimize button in the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableConnectionBarMinimizeButton
        {
            get => this.clientAdvancedSettings.ConnectionBarShowMinimizeButton;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.ConnectionBarShowMinimizeButton = value;
            }
        }

        /// <summary>
        /// Pin the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableConnectionBarPin
        {
            get => this.clientAdvancedSettings.PinConnectionBar;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.PinConnectionBar = value;
            }
        }

        /// <summary>
        /// Text to show in connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ConnectionBarText
        {
            get => this.clientNonScriptable.ConnectionBarText;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientNonScriptable.ConnectionBarText = value;
            }
        }

        //---------------------------------------------------------------------
        // Device properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Redirect the clipboard.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableClipboardRedirection
        {
            get => this.clientAdvancedSettings.RedirectClipboard;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectClipboard = value;
            }
        }

        /// <summary>
        /// Redirect local printers.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnablePrinterRedirection
        {
            get => this.clientAdvancedSettings.RedirectPrinters;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectPrinters = value;
            }
        }

        /// <summary>
        /// Redirect local smart cards.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableSmartCardRedirection
        {
            get => this.clientAdvancedSettings.RedirectSmartCards;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectSmartCards = value;
            }
        }

        /// <summary>
        /// Redirect local drives.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableDriveRedirection
        {
            get => this.clientAdvancedSettings.RedirectDrives;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectDrives = value;
            }
        }

        /// <summary>
        /// Redirect local devices.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableDeviceRedirection
        {
            get => this.clientAdvancedSettings.RedirectDevices;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectDevices = value;
            }
        }

        /// <summary>
        /// Redirect local ports.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnablePortRedirection
        {
            get => this.clientAdvancedSettings.RedirectPorts;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectPorts = value;
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableWebAuthnRedirection { get; set; }

        //---------------------------------------------------------------------
        // Hotkey properties.
        //---------------------------------------------------------------------

        public Keys FocusHotKey
        {
            //
            // NB. The Ctrl+Alt modifiers are implied by the HotKeyFocusRelease properties.
            //
            get => (Keys)this.clientAdvancedSettings.HotKeyFocusReleaseLeft | Keys.Control | Keys.Alt;
            set
            {
                Debug.Assert(value.HasFlag(Keys.Control));
                Debug.Assert(value.HasFlag(Keys.Alt));

                var keyWithoutModifiers = (int)(value & ~(Keys.Control | Keys.Alt));

                this.clientAdvancedSettings.HotKeyFocusReleaseLeft = keyWithoutModifiers;
                this.clientAdvancedSettings.HotKeyFocusReleaseRight = keyWithoutModifiers;
            }
        }

        public Keys LeaveFullScreenHotKey
        {
            //
            // NB. The Ctrl+Alt modifiers are implied by the HotKeyFullScreen properties.
            //
            get => (Keys)this.clientAdvancedSettings.HotKeyFullScreen | Keys.Control | Keys.Alt;
            set
            {
                Debug.Assert(value.HasFlag(Keys.Control));
                Debug.Assert(value.HasFlag(Keys.Alt));

                var keyWithoutModifiers = (int)(value & ~(Keys.Control | Keys.Alt));

                this.clientAdvancedSettings.HotKeyFullScreen = keyWithoutModifiers;
                this.clientAdvancedSettings.HotKeyFullScreen = keyWithoutModifiers;
            }
        }

        //---------------------------------------------------------------------
        // Sound and color properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Control where to play audio:
        /// 
        /// 0 - play locally
        /// 1 - play on server
        /// 3 - do not play
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int AudioRedirectionMode
        {
            get => this.clientSecuredSettings.AudioRedirectionMode;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientSecuredSettings.AudioRedirectionMode = value;
            }
        }

        /// <summary>
        /// Color depth, in bits
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ColorDepth
        {
            get => this.client.ColorDepth;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.ColorDepth = value;
            }
        }

        //---------------------------------------------------------------------
        // Public methods.
        //---------------------------------------------------------------------

        public void Connect()
        {
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
            this.client.DesktopHeight = this.Size.Height;
            this.client.DesktopWidth = this.Size.Width;

            this.client.Connect();
        }

        //---------------------------------------------------------------------
        // Full-screen mode.
        //
        // In container-handled mode, setting FullScreen to true..
        //
        // - calls the OnRequestGoFullScreen event,
        // - shows the connection bar (if enabled)
        // - changes hotkeys
        //
        // However, it does not resize the control automatically.
        //
        //---------------------------------------------------------------------

        private class FullScreenContext
        {
            public Rectangle? Bounds { get; }

            public FullScreenContext(Rectangle? bounds)
            {
                this.Bounds = bounds;
            }
        }

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
                    this.deferResize.Invoke();

                    this.fullScreenContext = null;
                    Debug.Assert(!this.ContainerFullScreen);
                }
            }
        }


        public bool CanEnterFullScreen
        {
            get => this.State == ConnectionState.LoggedOn;
        }

        /// <summary>
        /// Enter full screen.
        /// </summary>
        /// <param name="parentWindow">Outmost window</param>
        /// <param name="customBounds">Custom bounds for multi-screen full-screen</param>
        /// <returns></returns>
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

        //---------------------------------------------------------------------
        // State tracking.
        //---------------------------------------------------------------------

        public event EventHandler StateChanged;
        public event EventHandler<ExceptionEventArgs> ConnectionFailed;
        internal event EventHandler AuthenticationWarningDisplayed;

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

        private void ExpectState(ConnectionState expectedState)
        {
            if (this.State != expectedState)
            {
                throw new InvalidOperationException($"Operation is not allowed in state {this.State}");
            }
        }

        public enum ConnectionState
        {
            NotConnected,
            Connecting,
            Connected,
            LoggedOn,
        }

        internal Task AwaitStateAsync(ConnectionState state)
        {
            Debug.Assert(!this.InvokeRequired);

            if (this.State == state)
            {
                return Task.CompletedTask;
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
            return completionSource.Task;
        }
    }
}
