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

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    public class RdpClient : UserControl
    {
        private readonly Google.Solutions.Tsc.MsRdpClient client;
        private readonly IMsRdpClientNonScriptable5 nonScriptable;

        private ConnectionState state = ConnectionState.NotConnected;

        public RdpClient()
        {
            this.client = new Google.Solutions.Tsc.MsRdpClient
            {
                Enabled = true,
                Location = new System.Drawing.Point(0, 0),
                Name = "client",
                Size = new System.Drawing.Size(100, 100),
            };


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


            this.nonScriptable = (IMsRdpClientNonScriptable5)this.client.GetOcx();

            this.nonScriptable.AllowCredentialSaving = false;
            this.nonScriptable.PromptForCredentials = false;
            this.nonScriptable.NegotiateSecurityLayer = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            this.client.Size = this.Size;
        }

        //---------------------------------------------------------------------
        // RDP callbacks.
        //---------------------------------------------------------------------

        private void OnFatalError(
            object sender,
            IMsTscAxEvents_OnFatalErrorEvent args)
        {
        }

        private void OnLogonError(
            object sender,
            IMsTscAxEvents_OnLogonErrorEvent args)
        {
        }

        private void OnLoginComplete(object sender, EventArgs e)
        {
        }

        private void OnDisconnected(
            object sender,
            IMsTscAxEvents_OnDisconnectedEvent args)
        {
        }

        private void OnConnected(object sender, EventArgs e)
        {
        }


        private void OnConnecting(object sender, EventArgs e)
        {
        }

        private void OnAuthenticationWarningDisplayed(object sender, EventArgs _)
        {
        }

        private void OnWarning(
            object sender,
            IMsTscAxEvents_OnWarningEvent args)
        {
        }

        private void OnAutoReconnecting2(
            object sender,
            IMsTscAxEvents_OnAutoReconnecting2Event args)
        {
        }

        private void OnAutoReconnected(object sender, EventArgs e)
        {
        }

        private void OnFocusReleased(
            object sender,
            IMsTscAxEvents_OnFocusReleasedEvent e)
        {
        }

        private void OnRemoteDesktopSizeChange(
            object sender,
            IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
        {
        }

        private void OnServiceMessageReceived(
            object sender,
            IMsTscAxEvents_OnServiceMessageReceivedEvent e)
        {
        }

        private void reconnectButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
        }

        private void OnRequestGoFullScreen(object sender, EventArgs e)
        {
        }

        private void OnRequestLeaveFullScreen(object sender, EventArgs e)
        {
        }

        private void OnRequestContainerMinimize(object sender, EventArgs e)
        {
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
                this.client.AdvancedSettings7.RDPPort = value;
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
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.AdvancedSettings7.ClearTextPassword = value;
            }
        }

        public void Connect()
        {
            ExpectState(ConnectionState.NotConnected);

            this.client.Connect();
        }

        //---------------------------------------------------------------------
        // State tracking.
        //---------------------------------------------------------------------

        public event EventHandler StateChanged;

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
