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
using Google.Solutions.Ssh.Native;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Channel for interacting with a remote shell.
    /// </summary>
    /// <remarks> 
    /// Events are delivered on the worker thread.
    /// </remarks>
    public class SshShellChannel : SshChannelBase, IPseudoTerminal
    {
        /// <summary>
        /// Encoding used by pseudo-terminal.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Channel handle, must only be accessed on worker thread.
        /// </summary>
        private readonly Libssh2ShellChannel nativeChannel;

        private readonly StreamingDecoder receiveDecoder;
        private readonly byte[] receiveBuffer = new byte[64 * 1024];

        /// <summary>
        /// Task that is completed when an EOF was received.
        /// </summary>
        /// <remarks>
        /// Force continuations to run asycnhronously so that they
        /// don't block the worker thread.
        /// </remarks>
        private readonly TaskCompletionSource<object?> endOfStream
            = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        internal SshShellChannel(
            SshConnection connection,
            Libssh2ShellChannel nativeChannel)
        {
            this.Connection = connection;
            this.nativeChannel = nativeChannel;
            this.receiveDecoder = new StreamingDecoder(DefaultEncoding);
        }

        //---------------------------------------------------------------------
        // IPseudoTerminal.
        //---------------------------------------------------------------------

        public event EventHandler<PseudoTerminalDataEventArgs>? OutputAvailable;
        public event EventHandler<PseudoTerminalErrorEventArgs>? FatalError;
        public event EventHandler<EventArgs>? Disconnected;

        public Task DrainAsync()
        {
            return this.endOfStream.Task;
        }

        public async Task ResizeAsync(
            PseudoTerminalSize dimensions,
            CancellationToken cancellationToken)
        {
            //
            // Switch to worker thread and write to channel.
            //
            try
            {
                await this.Connection
                    .RunAsync(_ =>
                    {
                        Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                        this.nativeChannel.ResizePseudoTerminal(
                            dimensions.Width,
                            dimensions.Height);
                    })
                    .ConfigureAwait(false);
            }
            catch (SshConnectionClosedException)
            {
                //
                // Connection closed already. This can happen
                // if the connection was disconnected by the
                // server, and the client has't caught up to that.
                //
            }
        }

        public async Task WriteAsync(
            string data,
            CancellationToken cancellationToken)
        {
            if (data.Length == 0)
            {
                return;
            }

            //
            // Switch to worker thread and write to channel.
            //
            await this.Connection
                .RunAsync(_ =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    this.nativeChannel.Write(DefaultEncoding.GetBytes(data));
                })
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override SshConnection Connection { get; }

        internal override void OnReceive()
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            //
            // NB. This method is always called on the same thread, so it's ok
            // to reuse the same buffer.
            //

            uint bytesReceived;
            bool endOfStream;

            //
            // Read as much data as available.
            //
            do
            {
                bytesReceived = this.nativeChannel.Read(this.receiveBuffer);
                endOfStream = this.nativeChannel.IsEndOfStream;

                var receivedData = this.receiveDecoder.Decode(
                    this.receiveBuffer,
                    0,
                    (int)bytesReceived);

                try
                {
                    if (bytesReceived > 0)
                    {
                        this.OutputAvailable?.Invoke(
                            this,
                            new PseudoTerminalDataEventArgs(receivedData));
                    }

                    if (endOfStream)
                    {
                        //
                        // End of stream reached, that means we're
                        // disconnecting.
                        //
                        this.Disconnected?.Invoke(this, EventArgs.Empty);

                        this.endOfStream.SetResult(null);
                    }
                }
                catch (Exception e)
                {
                    this.FatalError?.Invoke(this, new PseudoTerminalErrorEventArgs(e));
                }
            }
            while (bytesReceived > 0 && !endOfStream);
        }

        internal override void OnReceiveError(Exception exception)
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            var errorsIndicatingLostConnection = new[]
            {
                LIBSSH2_ERROR.SOCKET_SEND,
                LIBSSH2_ERROR.SOCKET_RECV,
                LIBSSH2_ERROR.SOCKET_TIMEOUT
            };

            var unwrappedException = exception.Unwrap();
            var lostConnection = unwrappedException is Libssh2Exception sshEx &&
                errorsIndicatingLostConnection.Contains(sshEx.ErrorCode);

            if (lostConnection)
            {
                this.Disconnected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.FatalError?.Invoke(
                    this,
                    new PseudoTerminalErrorEventArgs(unwrappedException));
            }
        }

        protected override void Close()
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            this.nativeChannel.Close();
            this.nativeChannel.Dispose();
        }
    }
}
