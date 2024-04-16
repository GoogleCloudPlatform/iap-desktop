//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Threading;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Native;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Channel for interacting with a remote shell.
    /// </summary>
    public class RemoteShellChannel : RemoteChannelBase
    {
        public const string DefaultTerminal = "xterm";
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;
        public static readonly TerminalSize DefaultTerminalSize = new TerminalSize(80, 24);

        /// <summary>
        /// Channel handle, must only be accessed on worker thread.
        /// </summary>
        private readonly Libssh2ShellChannel nativeChannel;

        private readonly ITextTerminal terminal;
        private readonly StreamingDecoder decoder;

        private readonly byte[] receiveBuffer = new byte[64 * 1024];

        public override RemoteConnection Connection { get; }

        internal RemoteShellChannel(
            RemoteConnection connection,
            Libssh2ShellChannel nativeChannel,
            ITextTerminal terminal)
        {
            this.Connection = connection;
            this.nativeChannel = nativeChannel;
            this.terminal = terminal;
            this.decoder = new StreamingDecoder(DefaultEncoding);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Close()
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            this.nativeChannel.Close();
            this.nativeChannel.Dispose();
        }

        internal override void OnReceive()
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            //
            // NB. This method is always called on the same thread, so it's ok
            // to reuse the same buffer.
            //

            var bytesReceived = this.nativeChannel.Read(this.receiveBuffer);
            var endOfStream = this.nativeChannel.IsEndOfStream;

            //
            // Run callback asynchronously (post) on different context 
            // (i.e., not on the current worker thread).
            //
            // NB. By decoding the data first, we make sure that the
            // buffer is ready to be reused while the callback is
            // running.
            //

            var receivedData = this.decoder.Decode(
                this.receiveBuffer,
                0,
                (int)bytesReceived);

            this.Connection.CallbackContext.Post(() =>
            {
                try
                {
                    this.terminal.OnDataReceived(receivedData);

                    //
                    // In non-blocking mode, we're not always receive a final
                    // zero-length read.
                    //
                    if (bytesReceived > 0 && endOfStream)
                    {
                        this.terminal.OnDataReceived(string.Empty);
                    }
                }
                catch (Exception e)
                {
                    this.terminal.OnError(TerminalErrorType.TerminalIssue, e);
                }
            });
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

            var lostConnection = exception.Unwrap() is Libssh2Exception sshEx &&
                errorsIndicatingLostConnection.Contains(sshEx.ErrorCode);

            //
            // Run callback on different context (not on the current
            // worker thread).
            //
            this.Connection.CallbackContext.Post(() =>
            {
                this.terminal.OnError(
                    lostConnection
                        ? TerminalErrorType.ConnectionLost
                        : TerminalErrorType.ConnectionFailed,
                    exception);
            });
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public async Task SendAsync(byte[] buffer)
        {
            await this.Connection
                .RunSendOperationAsync(_ =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    this.nativeChannel.Write(buffer);
                })
                .ConfigureAwait(false);
        }

        public Task SendAsync(string data)
        {
            return SendAsync(DefaultEncoding.GetBytes(data));
        }

        public async Task ResizeTerminalAsync(TerminalSize size)
        {
            await this.Connection
                .RunSendOperationAsync(_ =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    this.nativeChannel.ResizePseudoTerminal(
                        size.Columns,
                        size.Rows);
                })
                .ConfigureAwait(false);
        }
    }
}
