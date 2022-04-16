//
// Copyright 2020 Google LLC
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

using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public class SshShellConnection : SshWorkerThread<SshShellChannel>
    {
        public const string DefaultTerminal = "xterm";
        public static readonly TerminalSize DefaultTerminalSize = new TerminalSize(80, 24);
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        private readonly IRawTerminal terminal;
        private readonly TerminalSize initialSize;

        private readonly Queue<SendOperation> sendQueue = new Queue<SendOperation>();
        private readonly TaskCompletionSource<int> connectionCompleted
            = new TaskCompletionSource<int>();

        private readonly byte[] receiveBuffer = new byte[64 * 1024];

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshShellConnection(
            IPEndPoint endpoint,
            ISshAuthenticator authenticator,
            ITextTerminal terminal,
            TerminalSize initialSize,
            SynchronizationContext callbackContext = null)
            : base(
                  endpoint,
                  authenticator
                    .ThrowIfNull(nameof(authenticator))
                    .BindToSynchronizationContext(callbackContext))
        {
            //
            // NB. We don't want callbacks to happen on the
            // SSH worker threads, for two reasons:
            //
            // (1) It blocks the thread, degrading performance
            // (2) The callbacks most likely need to run on a 
            //     different thread (GUI thread) anyway.
            //
            // Therefore, force all callbacks onto a
            // synchronization context.
            //
            this.terminal = terminal
                .ThrowIfNull(nameof(terminal))
                .BindToSynchronizationContext(callbackContext)
                .ToRawTerminal(DefaultEncoding);
            this.initialSize = initialSize;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override SshShellChannel CreateChannel(SshAuthenticatedSession session)
        {
            IEnumerable<EnvironmentVariable> environmentVariables = null;
            if (this.terminal.Locale != null)
            {
                // Format language so that Linux understands it.
                var languageFormatted = this.terminal.Locale.Name.Replace('-', '_');
                environmentVariables = new[]
                {
                    //
                    // Try to pass locale - but do not fail the connection if
                    // the server rejects it.
                    //
                    new EnvironmentVariable(
                        "LC_ALL",
                        $"{languageFormatted}.UTF-8",
                        false)
                };
            }

            return session.OpenShellChannel(
                LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                this.terminal.TerminalType,
                this.initialSize.Columns,
                this.initialSize.Rows,
                environmentVariables);
        }

        protected override void CloseChannel(SshShellChannel channel)
        {
            try
            {
                channel.Close();
            }
            catch (Exception e)
            {
                //
                // NB. This is non-fatal - we're tearing down the 
                // connection anyway.
                //
                SshTraceSources.Default.TraceError(
                    "Closing connection failed for {0}: {1}",
                    Thread.CurrentThread.Name,
                    e);
            }
        }

        protected override void OnReadyToReceive(SshShellChannel channel)
        {
            //
            // NB. receiveFunc() can throw an exception, in which case
            // we do not complete the current packet. If the exception
            // is bad, we'll receive a OnReceiveError callback later.
            //
            // NB. This method is always called on the same thread, so it's ok
            // to reuse the same buffer.
            //

            var bytesReceived = channel.Read(this.receiveBuffer);
            this.terminal.OnDataReceived(this.receiveBuffer, 0, bytesReceived);

            //
            // In non-blocking mode, we're not always receive a final
            // zero-length read.
            //
            if (bytesReceived > 0 && channel.IsEndOfStream)
            {
                this.terminal.OnDataReceived(Array.Empty<byte>(), 0, 0);
            }
        }

        protected override void OnReceiveError(Exception exception)
        {
            this.terminal.OnError(exception);
        }

        protected override void OnReadyToSend(SshShellChannel channel)
        {
            lock (this.sendQueue)
            {
                Debug.Assert(this.sendQueue.Count > 0);

                var packet = this.sendQueue.Peek();

                //
                // NB. The operation can throw an exception. It is
                // imporant that we let this exception escape because
                // it might be simply an EAGAIN situation. If the
                // exception is bad, we'll receive an OnSendError 
                // callback later.
                //
                packet.Operation(channel);

                //
                // Sending succeeded - complete packet.
                //
                this.sendQueue.Dequeue();
                packet.CompletionSource.SetResult(0);

                if (this.sendQueue.Count == 0)
                {
                    //
                    // Do not ask us for more data, we do not have any
                    // right now.
                    //
                    NotifyReadyToSend(false);
                }
            }
        }

        protected override void OnSendError(Exception exception)
        {
            lock (this.sendQueue)
            {
                Debug.Assert(this.sendQueue.Count > 0);
                this.sendQueue.Dequeue().CompletionSource.SetException(exception);
            }
        }

        protected override void OnConnected()
        {
            // Complete task, but force continuation to run on different thread.
            Task.Run(() => this.connectionCompleted.SetResult(0));
        }

        protected override void OnConnectionError(Exception exception)
        {
            if (!this.connectionCompleted.Task.IsCompleted)
            {
                // TODO: improve this
                // Complete task, but force continuation to run on different thread.
                Task.Run(() => this.connectionCompleted.SetException(exception));
            }
        }

        //---------------------------------------------------------------------
        // I/O.
        //---------------------------------------------------------------------

        protected Task SendAsync(Action<SshChannelBase> operation)
        {
            if (!this.IsConnected)
            {
                throw new SshException("Connection is closed");
            }

            lock (this.sendQueue)
            {
                var packet = new SendOperation(operation);
                this.sendQueue.Enqueue(packet);

                // 
                // Nofify that we have data and expect a Send()
                // callback.
                //
                NotifyReadyToSend(true);

                //
                // Return a task - it'll be completed once we've
                // actually sent the data.
                //
                return packet.CompletionSource.Task;
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task ConnectAsync()
        {
            Connect();
            return connectionCompleted.Task;
        }

        public Task SendAsync(byte[] buffer)
        {
            return SendAsync(channel => channel.Write(buffer));
        }

        public Task SendAsync(string data)
        {
            return SendAsync(DefaultEncoding.GetBytes(data));
        }

        public Task ResizeTerminalAsync(TerminalSize size)
        {
            return SendAsync(
                channel => ((SshShellChannel)channel).ResizePseudoTerminal(
                    size.Columns,
                    size.Rows));
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        protected internal class SendOperation
        {
            internal readonly TaskCompletionSource<uint> CompletionSource;
            internal readonly Action<SshChannelBase> Operation;

            internal SendOperation(Action<SshChannelBase> operation)
            {
                this.Operation = operation;
                this.CompletionSource = new TaskCompletionSource<uint>();
            }
        }
    }
}
