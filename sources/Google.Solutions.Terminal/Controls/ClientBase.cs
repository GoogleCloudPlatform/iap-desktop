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

using Google.Solutions.Mvvm.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Base class for terminal clients.
    /// 
    /// Some operations only work reliably when the control is in a certain
    /// state. In particular, this applies to the MSTSCAX client (which won't
    /// reliably tell us which state it is in), but applies to other clients
    /// as well.
    ///
    /// Thus, we maintain a state machine to track the control's state.
    /// </summary>
    public abstract class ClientBase : UserControl
    {
        private ConnectionState state = ConnectionState.NotConnected;

        /// <summary>
        /// Connection state has changed.
        /// </summary>
        public event EventHandler? StateChanged;

        /// <summary>
        /// Connection closed abnormally.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? ConnectionFailed;

        /// <summary>
        /// Connection closed normally.
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs>? ConnectionClosed;

        /// <summary>
        /// Connect to server.
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Simulate key strokes to send a piece of text.
        /// </summary>
        public abstract void SendText(string text);

        protected ClientBase()
        {
#if DEBUG
            //
            // Show label in top-right corner that indicates the current state.
            //

            var stateLabel = new Label()
            {
                AutoSize = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
            };
            this.Controls.Add(stateLabel);
            this.StateChanged += (_, args) => stateLabel.Text = this.State.ToString();
#endif
        }

        //---------------------------------------------------------------------
        // State tracking.
        //---------------------------------------------------------------------

        /// <summary>
        /// Current state of the connection.
        /// </summary>
        [Browsable(false)]
        public ConnectionState State
        {
            get => this.state;
            private set // Modified via OnXxx methods.
            {
                Debug.Assert(!this.InvokeRequired);
                if (this.state != value)
                {
                    this.state = value;
                    this.StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected void ExpectState(ConnectionState expectedState)
        {
            if (this.State != expectedState)
            {
                throw new InvalidOperationException(
                    $"Operation is not allowed in state {this.State}");
            }
        }

        /// <summary>
        /// Wait until a certain state has been reached. Mainly
        /// intended for testing.
        /// </summary>
        internal virtual async Task AwaitStateAsync(ConnectionState state)
        {
            Debug.Assert(!this.InvokeRequired);

            if (this.State == state)
            {
                return;
            }

            var completionSource = new TaskCompletionSource<ConnectionState>();

            void onStateChanged(object sender, EventArgs args)
            {
                if (this.State == state)
                {
                    this.StateChanged -= onStateChanged;
                    completionSource.SetResult(this.State);
                }
            }

            this.StateChanged += onStateChanged;

            await completionSource
                .Task
                .ConfigureAwait(true);
        }

        protected void OnBeforeConnect()
        {
            this.State = ConnectionState.Connecting;
        }

        protected void OnConnectionFailed(Exception e)
        {
            this.ConnectionFailed?.Invoke(this, new ExceptionEventArgs(e));

            this.State = ConnectionState.NotConnected;
        }

        protected void OnConnectionClosed(DisconnectReason reason)
        {
            this.ConnectionClosed?.Invoke(
                this,
                new ConnectionClosedEventArgs(reason));

            this.State = ConnectionState.NotConnected;
        }

        protected void OnAfterConnect()
        {
            this.State = ConnectionState.Connected;
        }

        protected void OnAfterLogin()
        {
            this.State = ConnectionState.LoggedOn;
        }

        protected void OnBeforeDisconnect()
        {
            this.State = ConnectionState.Disconnecting;
        }

        //---------------------------------------------------------------------
        // Inner types.
        //---------------------------------------------------------------------

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

            /// <summary>
            /// Client is disconnecting.
            /// </summary>
            Disconnecting,

            /// <summary>
            /// User logged on, session is ready to use.
            /// </summary>
            LoggedOn
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
