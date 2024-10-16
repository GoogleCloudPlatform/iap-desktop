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

using Google.Solutions.Common.Util;
using Google.Solutions.Platform.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
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
                OnConnectionClosed(DisconnectReason.SessionTimeout);
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
            _ = ContinueConnectAysnc();

            async Task ContinueConnectAysnc()
            {
                try
                {
                    this.Terminal.Device = await 
                        ConnectCoreAsync(this.Terminal.Dimensions)
                        .ConfigureAwait(true); // Back to caller thread.

                    //
                    // The distinction between the Connected and LoggedOn 
                    // states isn't relevant for Pty clients, so we skip
                    // straight to LoggedOn.
                    //

                    OnAfterConnect();
                    OnAfterLogin();
                }
                catch (Exception e)
                {
                    OnConnectionFailed(e);
                }
            }
        }

        public override void SendText(string text)
        {
            ExpectState(ConnectionState.LoggedOn);
            this.Terminal.SimulateSend(text);
        }

        //----------------------------------------------------------------------
        // Abstract methods.
        //----------------------------------------------------------------------

        /// <summary>
        /// Create a pty.
        /// </summary>
        protected abstract Task<IPseudoTerminal> ConnectCoreAsync(
            PseudoTerminalSize initialSize);

        /// <summary>
        /// Determine if the exception is caused by a timeout.
        /// </summary>
        protected abstract bool IsCausedByConnectionTimeout(Exception e);
    }
}
