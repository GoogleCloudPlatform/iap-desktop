using Google.Solutions.Common.Util;
using Google.Solutions.Platform.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Base class for a client that connects a virtual terminal 
    /// to a pseudo-terminal.
    /// </summary>
    public abstract class PseudoTerminalClientBase : ClientBase
    {
        public PseudoTerminalClientBase()
        {
            this.Terminal = new VirtualTerminal()
            {
                Dock = DockStyle.Fill
            };

            this.Terminal.DeviceClosed += OnDeviceClosed;
            this.Terminal.DeviceError += OnDeviceError;

            this.Controls.Add(this.Terminal);
        }

        [Browsable(true)]
        public VirtualTerminal Terminal { get; }

        //----------------------------------------------------------------------
        // Pty events.
        //----------------------------------------------------------------------

        private void OnDeviceError(object sender, VirtualTerminalErrorEventArgs e)
        {
            Debug.Assert(
                this.State == ConnectionState.Connecting ||
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn);

            //
            // Propagate event so that the hosting form can show
            // an error message.
            //
            if (IsCausedByConnectionTimeout(e.Exception))
            {
                OnConnectionClosed(DisconnectReason.Timeout);
            }
            else
            {
                OnConnectionFailed(e.Exception);
            }
        }

        private void OnDeviceClosed(object sender, EventArgs e)
        {
            //
            // This is an orderly close.
            //
            OnConnectionClosed(DisconnectReason.DisconnectedByUser);
        }

        //----------------------------------------------------------------------
        // Parentable control events.
        //----------------------------------------------------------------------

        protected override void OnFormClosing(object sender, FormClosingEventArgs args)
        {
            if (this.State == ConnectionState.Disconnecting)
            {
                //
                // Form is being closed as a result of a disconnect
                // (not the other way round).
                //
            }
            else if (
                this.State == ConnectionState.Connecting ||
                this.State == ConnectionState.Connected ||
                this.State == ConnectionState.LoggedOn)
            {
                //
                // Initiate a disconnect.
                //
                // NB. Disposing the pty doesn't invoke OnDeviceClosed.
                //
                OnBeforeDisconnect();

                Debug.Assert(this.Terminal.Device != null);
                this.Terminal.Device?.Dispose();

                OnConnectionClosed(DisconnectReason.FormClosed);
            }

            base.OnFormClosing(sender, args);
        }

        //----------------------------------------------------------------------
        // ClientBase overrides.
        //----------------------------------------------------------------------

        public override void Connect()
        {
            Debug.Assert(!this.Terminal.IsDisposed);

            ExpectState(ConnectionState.NotConnected);
            Precondition.Expect(this.IsHandleCreated, "Control must be created first");

            //
            // NB. We must initialize the pseudo-terminal with
            // the right dimensions. Now that the window has been
            // shown, we know these.
            //
            Debug.Assert(this.Terminal.Dimensions.Width > 0);
            Debug.Assert(this.Terminal.Dimensions.Height > 0);

            //
            // Reset state in case we're connecting for the second time.
            //
            OnBeforeConnect();

            //
            // Connect the terminal to the (new) pty.
            //
            try
            {
                this.Terminal.Device = ConnectCore(this.Terminal.Dimensions);

                OnAfterConnect();
                OnAfterLogin();
            }
            catch (Exception e)
            {
                OnConnectionFailed(e);
            }
        }

        public override void SendText(string text)
        {
            ExpectState(ConnectionState.Connected);
            this.Terminal.SimulateSend(text);
        }

        //----------------------------------------------------------------------
        // Abstract methods.
        //----------------------------------------------------------------------

        /// <summary>
        /// Create a pty.
        /// </summary>
        protected abstract IPseudoTerminal ConnectCore(
            PseudoTerminalSize initialSize);

        /// <summary>
        /// Determine if the exception is caused by a timeout.
        /// </summary>
        protected abstract bool IsCausedByConnectionTimeout(Exception e);
    }
}
