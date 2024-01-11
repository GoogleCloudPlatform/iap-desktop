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

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    public class RdpClient : UserControl
    {
        private readonly Google.Solutions.Tsc.MsRdpClient client;
        private readonly IMsRdpClientNonScriptable5 clientNonScriptable;
        private readonly IMsRdpClientAdvancedSettings6 clientAdvancedSettings;

        private ConnectionState state = ConnectionState.NotConnected;

        private readonly DeferredCallback deferResize;

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

            // this.client.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("client.OcxState")));

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


            this.clientNonScriptable = (IMsRdpClientNonScriptable5)this.client.GetOcx();

            this.clientNonScriptable.AllowCredentialSaving = false;
            this.clientNonScriptable.PromptForCredentials = false;
            this.clientNonScriptable.NegotiateSecurityLayer = true;
            
            this.clientAdvancedSettings = this.client.AdvancedSettings7;
            this.clientAdvancedSettings.EnableCredSspSupport = true;
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

        //---------------------------------------------------------------------
        // Resiying.
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
                    // First, resize the control.
                    //
                    this.client.Size = this.Size;

                    //
                    // Resize the session.
                    //
                    var newSize = this.Size;
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

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private void OnFatalError(
            object sender,
            IMsTscAxEvents_OnFatalErrorEvent args)
        {
            this.State = ConnectionState.ConnectionLost;
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
                this.State = ConnectionState.ConnectionLost;
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
            this.State = ConnectionState.ConnectionLost;


            var e = new RdpDisconnectedException(
                args.discReason,
                this.client.GetErrorDescription((uint)args.discReason, 0));

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(e.Message))
            {
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
            Debug.Assert(this.State ==ConnectionState.Connecting);
            //TODO: port rest
        }

        private void OnFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {

            //TODO: port rest
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

            //TODO: port rest
        }

        private void OnRequestLeaveFullScreen(object sender, EventArgs e)
        {

            Debug.Assert(this.State == ConnectionState.LoggedOn);
            //TODO: port rest
        }

        private void OnRequestContainerMinimize(object sender, EventArgs e)
        {
            //TODO: port rest
        }

        //---------------------------------------------------------------------
        // Publics.
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
        /// NetworkLevelAuthentication (CredSSP).
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

        public void Connect()
        {
            ExpectState(ConnectionState.NotConnected);
            Precondition.ExpectNotEmpty(this.Server, nameof(this.Server));
            Precondition.ExpectNotEmpty(this.Server, nameof(this.Username));

            this.client.Connect();
            this.State = ConnectionState.Connecting;
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
            ConnectionLost
        }
    }
}
