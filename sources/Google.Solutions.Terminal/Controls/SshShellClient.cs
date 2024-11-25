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
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Client that connects a virtual terminal to an SSH shell channel.
    /// </summary>
    public partial class SshShellClient : PseudoTerminalClientBase
    {
        private SshConnection? connection;

        private void CloseConnection()
        {
            //
            // Close underlying SSH connection. This will cause
            // the worker thread to stop, but that happens
            // asynchronously.
            //
            if (this.connection != null)
            {
                this.connection?.Dispose();
                this.connection = null;
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override async Task<IPseudoTerminal> ConnectCoreAsync(
            PseudoTerminalSize initialSize)
        {
            Debug.Assert(this.connection == null);

            var endpoint = this.ServerEndpoint.ExpectNotNull(nameof(this.ServerEndpoint));
            var credential = this.Credential.ExpectNotNull(nameof(this.Credential));

            //
            // Create a new SSH connection.
            //
            // NB. This might throw various types of Libssh2Exception,
            //     which are propagated as ConnectionFailed events.
            //
            var syncContext = SynchronizationContext.Current;
            this.connection = new SshConnection(
                endpoint,
                credential,
                new SynchronizedKeyboardInteractiveHandler(
                    this.KeyboardInteractiveHandler,
                    syncContext))
            {
                ConnectionTimeout = this.ConnectionTimeout,
                Banner = this.Banner,

                //
                // Do not join worker thread as this could block the
                // UI thread.
                //
                JoinWorkerThreadOnDispose = false
            };

            await this.connection
                .ConnectAsync()
                .ConfigureAwait(false);

            //
            // Open a shell channel, which acts as a pty.
            //
            // NB. The channel delivers event on an arbitrary thread,
            //     but the VirtualTerminal can deal with that. So we 
            //     don't need to worry about forcing callbacks back
            //     onto a different synchronization context here.
            //
            return await this.connection
                .OpenShellAsync(
                    initialSize,
                    this.TerminalType,
                    this.Locale)
                .ConfigureAwait(false);
        }

        protected override void OnConnectionClosed(DisconnectReason reason)
        {
            CloseConnection();

            base.OnConnectionClosed(reason);
        }

        protected override void OnConnectionFailed(Exception e)
        {
            CloseConnection();

            base.OnConnectionFailed(e);
        }

        protected override bool IsCausedByConnectionTimeout(Exception e)
        {
            return e.Unwrap() is Libssh2Exception sshEx &&
                sshEx.ErrorCode == LIBSSH2_ERROR.SOCKET_TIMEOUT;
        }

        /// <summary>
        /// Get underlying SSH connection.
        /// </summary>
        /// <remarks>
        /// The connection must be in LoggedOn state.
        /// </remarks>
        protected SshConnection Connection
        {
            get
            {
                ExpectState(ConnectionState.LoggedOn);
                return this.connection!;
            }
        }
    }
}
