using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using MSTSCLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Google.Solutions.CloudIap.IapDesktop.Windows;
using System.Diagnostics;
using AxMSTSCLib;

namespace Google.Solutions.CloudIap.IapDesktop.RemoteDesktop
{
    public partial class RemoteDesktopPane : ToolWindow
    {
        public RemoteDesktopPane()
        {
            
			this.TabText = this.Text;
			this.DockAreas = DockAreas.Document;

			var fullScreenMenuItem = new ToolStripMenuItem("&Full screen");
			fullScreenMenuItem.Click += fullScreenMenuItem_Click;
			this.TabContextStrip.Items.Add(fullScreenMenuItem);
			this.TabContextStrip.Opening += tabContextStrip_Opening;
		}

		//private void InitializeRdpControl()
		//{
		//	// NB. The initialization needs to happen after the pane is shown, otherwise
		//	// an error happens indicating that the control does not have a Window handle.
		//	System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RemoteDesktopPane));
		//	this.rdpClient = new AxMsRdpClient9NotSafeForScripting();
		//	((System.ComponentModel.ISupportInitialize)(this.rdpClient)).BeginInit();
		//	this.rdpClient.Dock = System.Windows.Forms.DockStyle.Fill;
		//	this.rdpClient.Enabled = true;
		//	this.rdpClient.Location = new System.Drawing.Point(0, 0);
		//	this.rdpClient.Name = "rdpClient";
		//	this.rdpClient.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("rdpClient.OcxState")));
		//	this.rdpClient.Size = this.Size;
		//	this.rdpClient.TabIndex = 1;
		//	((System.ComponentModel.ISupportInitialize)(this.rdpClient)).EndInit();
		//}

		public void Connect(
			string server,
			ushort port,
			VirtualMachineSettings settings)
		{
			// NB. The initialization needs to happen after the pane is shown, otherwise
			// an error happens indicating that the control does not have a Window handle.
			InitializeComponent();
			UpdateLayout();
			
			var advancedSettings = this.rdpClient.AdvancedSettings7;
			var nonScriptable = (IMsRdpClientNonScriptable4)this.rdpClient.GetOcx();
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

			if (settings.AuthenticationLevel == RdpAuthenticationLevel.RequireServerAuthentication)
			{
				advancedSettings.AuthenticationLevel = 1;
			}
			else
			{
				advancedSettings.AuthenticationLevel = 2;
			}

			//
			// Advanced connection settings.
			//
			advancedSettings.keepAliveInterval = 60000;
			advancedSettings.PerformanceFlags = 0; // Enable all features, it's 2020.
			advancedSettings.EnableAutoReconnect = true;
			advancedSettings.MaxReconnectAttempts = 10;

			//
			// Behavior settings.
			//
			advancedSettings.DisplayConnectionBar = 
				(settings.ConnectionBar != RdpConnectionBarState.Off);
			advancedSettings.PinConnectionBar = 
				(settings.ConnectionBar == RdpConnectionBarState.Pinned);
			advancedSettings.EnableWindowsKey = 1;
			advancedSettings.GrabFocusOnConnect = false;

			//
			// Local resources settings.
			//
			advancedSettings.RedirectClipboard = settings.RedirectClipboard;
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
			
			if (settings.DesktopSize == RdpDesktopSize.ScreenSize)
			{
				var screenSize = Screen.GetBounds(this);
				this.rdpClient.DesktopHeight = screenSize.Height;
				this.rdpClient.DesktopWidth = screenSize.Width;
			}
			else
			{
				this.rdpClient.DesktopHeight = this.Size.Height;
				this.rdpClient.DesktopWidth = this.Size.Width;
			}

			//
			// Keyboard settings.
			//
			// TODO: Map advancedSettings2.HotKey*
			//
			// NB. Apply key combinations to the remote server only when the client is running 
			// in full-screen mode.
			this.rdpClient.SecuredSettings2.KeyboardHookMode = 2;

			this.rdpClient.Connect();
		}

		public bool IsConnected => this.rdpClient.Connected == 1;
		public bool IsConnecting => this.rdpClient.Connected == 2;

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

		private void ShowErrorAndClose(string caption, Exception e)
		{
			ExceptionDialog.Show(this, caption, e);
			Close();
		}

		//---------------------------------------------------------------------
		// Window events.
		//---------------------------------------------------------------------

		private void RemoteDesktopPane_SizeChanged(object sender, EventArgs e)
		{
			UpdateLayout();
		}

		private void RemoteDesktopPane_FormClosing(object sender, FormClosingEventArgs args)
		{
			if (this.IsConnecting)
			{
				Debug.WriteLine("Aborting FormClosing because control is in connecting");
				args.Cancel = true;
				return;
			}

			Debug.WriteLine("FormClosing");
			if (this.IsConnected)
			{
				try
				{
					// NB. This does not trigger an OnDisconnected event.
					this.rdpClient.Disconnect();
				}
				catch (Exception e)
				{
					// TODO: Ignore?
					ExceptionDialog.Show(this, "Disconnecting failed", e);
				}
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
			this.rdpClient.FullScreen = true;
		}

		//---------------------------------------------------------------------
		// RDP callbacks.
		//---------------------------------------------------------------------

		private void rdpClient_OnFatalError(
			object sender, 
			IMsTscAxEvents_OnFatalErrorEvent args)
		{
			ShowErrorAndClose(
				"Fatal error",
				new RdpFatalException(args.errorCode));
		}

		private void rdpClient_OnLogonError(
			object sender, 
			IMsTscAxEvents_OnLogonErrorEvent args)
		{
			var e = new RdpLogonException(args.lError);
			if (!e.IsIgnorable)
			{
				ShowErrorAndClose("Logon failed", e);
			}
		}

		private void rdpClient_OnDisconnected(
			object sender, 
			IMsTscAxEvents_OnDisconnectedEvent args)
		{
			var e = new RdpDisconnectedException(
				args.discReason,
				this.rdpClient.GetErrorDescription((uint)args.discReason, 0));

			Debug.WriteLine($"OnDisconnected: {e}");

			if (e.IsIgnorable)
			{
				Close();
			}
			else
			{
				ShowErrorAndClose("Disconnected", e);
			}
		}

		private void rdpClient_OnConnected(object sender, EventArgs e)
		{
			Debug.WriteLine($"OnConnected - {this.rdpClient.ConnectedStatusText}");
			this.spinner.Visible = false;
		}


		private void rdpClient_OnConnecting(object sender, EventArgs e)
		{
			Debug.WriteLine("OnConnecting");
		}

		private void rdpClient_OnAuthenticationWarningDisplayed(object sender, EventArgs args)
		{
			Debug.WriteLine("OnAuthenticationWarningDisplayed");
		}

		private void rdpClient_OnWarning(
			object sender, 
			IMsTscAxEvents_OnWarningEvent args)
		{
			Debug.WriteLine($"OnWarning: {args.warningCode}");
		}

		private void rdpClient_OnAutoReconnecting2(
			object sender, 
			IMsTscAxEvents_OnAutoReconnecting2Event args)
		{
			var e = new RdpDisconnectedException(
				args.disconnectReason,
				this.rdpClient.GetErrorDescription((uint)args.disconnectReason, 0));
			Debug.WriteLine($"OnAutoReconnecting2: {args.attemptCount}/{args.maxAttemptCount} - {e}, Net: {args.networkAvailable}");
		}

		private void rdpClient_OnAutoReconnected(object sender, EventArgs e)
		{
			Debug.WriteLine("OnAutoReconnected");
		}

		private void rdpClient_OnFocusReleased(
			object sender, 
			IMsTscAxEvents_OnFocusReleasedEvent e)
		{
			Debug.WriteLine("OnFocusReleased");
		}

		private void rdpClient_OnRemoteDesktopSizeChange(
			object sender, 
			IMsTscAxEvents_OnRemoteDesktopSizeChangeEvent e)
		{
			Debug.WriteLine("OnRemoteDesktopSizeChange");
		}
	}
}
