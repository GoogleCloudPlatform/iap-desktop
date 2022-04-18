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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Threading;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public class SshConnection : SshWorkerThread
    {
        private readonly Queue<SendOperation> sendQueue = new Queue<SendOperation>();
        private readonly TaskCompletionSource<int> connectionCompleted
            = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        //
        // List of open channels. Only accessed on worker thread,
        // so no locking required.
        //
        private readonly LinkedList<SshAsyncChannelBase> channels
            = new LinkedList<SshAsyncChannelBase>();

        public SshConnection(
            IPEndPoint endpoint,
            ISshAuthenticator authenticator,
            SynchronizationContext callbackContext)
            : base(
                  endpoint,
                  authenticator,
                  callbackContext)
        {
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnConnected()
        {
            //
            // Complete task on callback context.
            //
            this.CallbackContext.Post(() => this.connectionCompleted.SetResult(0));
        }

        protected override void OnConnectionError(Exception exception)
        {
            if (!this.connectionCompleted.Task.IsCompleted)
            {
                //
                // Complete task (with asynchronous continuation).
                //
                this.connectionCompleted.SetException(exception);
            }
        }

        protected override void OnReadyToSend(SshAuthenticatedSession session)
        {
            lock (this.sendQueue)
            {
                Debug.Assert(this.IsRunningOnWorkerThread);
                Debug.Assert(this.sendQueue.Count > 0);

                var packet = this.sendQueue.Peek();

                //
                // NB. The operation can throw an exception. It is
                // imporant that we let this exception escape because
                // it might be simply an EAGAIN situation. If the
                // exception is bad, we'll receive an OnSendError 
                // callback later.
                //
                packet.Operation(session);

                //
                // Sending succeeded - complete packet.
                //
                // N.B. Force continuations onto callback
                // continuation context so that we don't block the
                // current worker thread.
                //
                this.sendQueue.Dequeue();

                this.CallbackContext.Post(() => packet.CompletionSource.SetResult(0));

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

        protected override void OnReadyToReceive(SshAuthenticatedSession session)
        {
            foreach (var channel in this.channels)
            {
                channel.OnReceive();
            }
        }

        protected override void OnReceiveError(Exception exception)
        {
            foreach (var channel in this.channels)
            {
                channel.OnReceiveError(exception);
            }
        }

        protected override void OnBeforeCloseSession()
        {
            Debug.Assert(this.IsRunningOnWorkerThread);

            //
            // Close all open channels.
            //
            foreach (var channel in this.channels)
            {
                channel.Dispose();
            }
        }

        //---------------------------------------------------------------------
        // Helper methods for channels.
        //---------------------------------------------------------------------

        /// <summary>
        /// Run an sending operation on the worker thread
        /// when the connection is ready to send data.
        /// </summary>
        internal Task RunSendOperationAsync(
            Action<SshAuthenticatedSession> sendOperation)
        {
            if (!this.IsConnected)
            {
                throw new SshException("Connection is closed");
            }

            lock (this.sendQueue)
            {
                var packet = new SendOperation(sendOperation);
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

        internal async Task<TResult> RunSendOperationAsync<TResult>(
            Func<SshAuthenticatedSession, TResult> sendOperation)
            where TResult : class
        {
            TResult result = null;

            await RunSendOperationAsync(session =>
                {
                    result = sendOperation(session);
                })
                .ConfigureAwait(false);

            return result;
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task ConnectAsync()
        {
            StartConnection();
            return this.connectionCompleted.Task;
        }

        public async Task<SshAsyncShellChannel> OpenShellChannelAsync(
            ITextTerminal terminal,
            TerminalSize initialSize)
        {
            IEnumerable<EnvironmentVariable> environmentVariables = null;
            if (terminal.Locale != null)
            {
                //
                // Format language so that Linux understands it.
                //
                var languageFormatted = terminal.Locale.Name.Replace('-', '_');
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

            return await RunSendOperationAsync(
                session => {
                    Debug.Assert(this.IsRunningOnWorkerThread);

                    using (session.Session.AsBlocking())
                    {
                        var nativeChannel = session.OpenShellChannel(
                            LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                            terminal.TerminalType,
                            initialSize.Columns,
                            initialSize.Rows,
                            environmentVariables);

                        var channel = new SshAsyncShellChannel(
                            this,
                            nativeChannel,
                            terminal);

                        this.channels.AddLast(channel);

                        return channel;
                    }
                })
                .ConfigureAwait(false);
        }


        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        protected internal class SendOperation
        {
            internal readonly TaskCompletionSource<uint> CompletionSource;
            internal readonly Action<SshAuthenticatedSession> Operation;

            internal SendOperation(Action<SshAuthenticatedSession> operation)
            {
                this.Operation = operation;
                this.CompletionSource = new TaskCompletionSource<uint>();
            }
        }
    }

    public abstract class SshAsyncChannelBase : IDisposable // TODO: add comments
    {
        public abstract SshConnection Connection { get; }

        internal abstract void OnReceive();

        internal abstract void OnReceiveError(Exception exception);

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class SshAsyncShellChannel : SshAsyncChannelBase // TODO: Rename, put into separate file
    {
        public const string DefaultTerminal = "xterm";
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;
        public static readonly TerminalSize DefaultTerminalSize = new TerminalSize(80, 24);

        /// <summary>
        /// Channel handle, must only be accessed on worker thread.
        /// </summary>
        private readonly SshShellChannel nativeChannel;

        private readonly ITextTerminal terminal;
        private readonly StreamingDecoder decoder;

        private readonly byte[] receiveBuffer = new byte[64 * 1024];
        private bool closed = false;

        public override SshConnection Connection { get; }

        internal SshAsyncShellChannel(
            SshConnection connection,
            SshShellChannel nativeChannel,
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

        protected override void Dispose(bool disposing) // TODO: Hoist to base class
        {
            if (this.Connection.IsRunningOnWorkerThread)
            {
                try
                {
                    if (!this.closed)
                    {
                        this.nativeChannel.Close();
                        this.nativeChannel.Dispose();
                        this.closed = true;
                    }
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
            else
            {
                this.Connection
                    .RunSendOperationAsync(_ => this.Dispose())
                    .ContinueWith(_ => { });
            }
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
                    this.terminal.OnError(e);
                }
            });
        }

        internal override void OnReceiveError(Exception exception)
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            //
            // Run callback on different context (not on the current
            // worker thread).
            //
            this.Connection.CallbackContext.Post(() =>
            {
                this.terminal.OnError(exception);
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
