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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Base class for terminal clients. A terminal client implements a 
    /// certain protocol (such as RDP or SSH) and a UI control to
    /// interact with it.
    /// 
    /// Some operations only work reliably when the control is in a certain
    /// state. In particular, this applies to the MSTSCAX client (which won't
    /// reliably tell us which state it is in), but applies to other clients
    /// as well.
    ///
    /// Thus, we maintain a state machine to track the control's state.
    /// </summary>
    public abstract class ClientBase : ParentedUserControl
    {
        private readonly ClientStatePanel statePanel;
        private ClientState state = ClientState.NotConnected;

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
            //
            // Show an overlay panel whenever the client is not connected.
            //
            this.statePanel = new ClientStatePanel();
            this.Controls.Add(this.statePanel);

            this.Resize += (_, args) =>
            {
                this.statePanel.Size = this.Size;
            };
            this.StateChanged += (_, args) =>
            {
                this.statePanel.State = this.State;

                //
                // NB. We check IsContainerFullScreen as opposed to
                //     IsFullScreen here because IsFullScreen (at least
                //     in the case of the RdpClient) is only updated
                //     asynchronously after leaving full-screen. 
                //
                //     One particular example where this difference shows
                //     is when the RDP session is disconnected because of
                //     a session timeout while in full-screen mode.
                //     

                this.statePanel.Visible = !this.IsContainerFullScreen && (
                    this.State == ClientState.NotConnected ||
                    this.State == ClientState.Disconnecting ||
                    this.State == ClientState.Connecting);
            };
            this.statePanel.ConnectButtonClicked += (_, args) => Connect();
        }

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
        /// Indicates whether the connection is in a state that permits sending 
        /// key strokes.
        /// </summary>
        [Browsable(false)]
        public bool CanSendText
        {
            get => this.State == ClientState.LoggedOn;
        }

        /// <summary>
        /// Simulate key strokes to send a piece of text.
        /// </summary>
        public abstract void SendText(string text);

        /// <summary>
        /// Check if the client is currently hosted in a full-screen container.
        /// </summary>
        [Browsable(false)]
        public virtual bool IsContainerFullScreen
        {
            get => false;
        }

        public virtual void Bind(IBindingContext bindingContext)
        { }

        //---------------------------------------------------------------------
        // Connection state tracking.
        //---------------------------------------------------------------------

        /// <summary>
        /// Current state of the connection.
        /// </summary>
        [Browsable(false)]
        public ClientState State
        {
            get => this.state;
            private set // Only to be mutated by OnXxx methods.
            {
                Debug.Assert(!this.InvokeRequired);
                if (this.state != value)
                {
                    this.state = value;
                    OnStateChanged();
                }
            }
        }

        protected virtual void OnStateChanged()
        {
            this.StateChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void ExpectState(ClientState expectedState)
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
        internal virtual async Task AwaitStateAsync(
            ClientState state,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!this.InvokeRequired);

            if (this.State == state)
            {
                return;
            }

            var completionSource = new TaskCompletionSource<ClientState>();

            //
            // Register for relevant events.
            //
            void onStateChanged(object? sender, EventArgs args)
            {
                if (this.State == state)
                {
                    completionSource.SetResult(this.State);
                }
            }

            void onConnectionFailed(object? sender, ExceptionEventArgs args)
            {
                completionSource.SetException(args.Exception);
            }

            this.StateChanged += onStateChanged;
            this.ConnectionFailed += onConnectionFailed;

            //
            // Wait till one of the events trigger or the
            // operation is cancelled.
            //

            var registration = cancellationToken
                .Register(() => completionSource.TrySetCanceled());

            try
            {
                await completionSource
                    .Task
                    .ConfigureAwait(true);
            }
            finally
            {
                registration.Dispose();

                this.StateChanged -= onStateChanged;
                this.ConnectionFailed -= onConnectionFailed;
            }
        }

        protected virtual void OnBeforeConnect()
        {
            this.State = ClientState.Connecting;
        }

        protected virtual void OnConnectionFailed(Exception e)
        {
            this.ConnectionFailed?.Invoke(this, new ExceptionEventArgs(e));

            this.State = ClientState.NotConnected;
        }

        protected virtual void OnConnectionClosed(DisconnectReason reason)
        {
            this.ConnectionClosed?.Invoke(
                this,
                new ConnectionClosedEventArgs(reason));

            this.State = ClientState.NotConnected;
        }

        protected virtual void OnAfterConnect()
        {
            this.State = ClientState.Connected;
        }

        protected virtual void OnAfterLogin()
        {
            this.State = ClientState.LoggedOn;
        }

        protected virtual void OnBeforeDisconnect()
        {
            this.State = ClientState.Disconnecting;
        }

        //---------------------------------------------------------------------
        // Inner types.
        //---------------------------------------------------------------------

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
            /// <summary>
            /// The session timed out and was disconnected by the server.
            /// </summary>
            SessionTimeout,

            /// <summary>
            /// The session was disconnected by the user.
            /// </summary>
            DisconnectedByUser,

            /// <summary>
            /// The user closed the window/form that controlled the session.
            /// </summary>
            FormClosed,

            /// <summary>
            /// The user requested the session to be reconnected.
            /// </summary>
            ReconnectInitiatedByUser,
        }
    }
}
