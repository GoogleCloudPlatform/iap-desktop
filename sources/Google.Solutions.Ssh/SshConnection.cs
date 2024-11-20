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
using Google.Solutions.Platform.IO;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Google.Solutions.Ssh.SshConnection;

namespace Google.Solutions.Ssh
{
    public class SshConnection : SshWorkerThread
    {
        private readonly Queue<ISendOperation> sendQueue = new Queue<ISendOperation>();

        /// <summary>
        /// Task that is completed after the SSH connection has
        /// been established.
        /// </summary>
        /// <remarks>
        /// Force continuations to run asycnhronously so that they
        /// don't block the worker thread.
        /// </remarks>
        private readonly TaskCompletionSource<int> connectionCompleted
            = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        //
        // List of open channels. Only accessed on worker thread,
        // so no locking required.
        //
        private readonly LinkedList<SshChannelBase> channels
            = new LinkedList<SshChannelBase>();

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <remarks>
        /// Keyboard handler callbacks are delivered on the designated
        /// synchronization context.
        /// </remarks>
        public SshConnection(
            IPEndPoint endpoint,
            ISshCredential credential,
            IKeyboardInteractiveHandler keyboardHandler)
            : base(endpoint, credential, keyboardHandler)
        {
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        private protected override void OnConnected()
        {
            //
            // Complete task on callback context.
            //
            this.connectionCompleted.SetResult(0);
        }

        private protected override void OnConnectionError(Exception exception)
        {
            if (!this.connectionCompleted.Task.IsCompleted)
            {
                //
                // Complete task (with asynchronous continuation).
                //
                this.connectionCompleted.SetException(exception);
            }
        }

        private protected override void OnReadyToSend(Libssh2AuthenticatedSession session)
        {
            lock (this.sendQueue)
            {
                Debug.Assert(this.IsRunningOnWorkerThread);
                Debug.Assert(this.sendQueue.Count > 0);

                var packet = this.sendQueue.Peek();

                //
                // NB. The operation can throw an exception. It is
                // important that we let this exception escape because
                // it might be simply an EAGAIN situation. If the
                // exception is bad, we'll receive an OnSendError 
                // callback later.
                //
                packet.Run(session);

                //
                // Sending succeeded - complete packet.
                //
                // N.B. Force continuations onto callback
                // continuation context so that we don't block the
                // current worker thread.
                //
                this.sendQueue.Dequeue();

                packet.OnCompleted();

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

        private protected override void OnSendError(Exception exception)
        {
            lock (this.sendQueue)
            {
                Debug.Assert(this.sendQueue.Count > 0);
                this.sendQueue.Dequeue().OnFailed(exception);
            }
        }

        private protected override void OnReadyToReceive(Libssh2AuthenticatedSession session)
        {
            foreach (var channel in this.channels)
            {
                channel.OnReceive();
            }
        }

        private protected override void OnReceiveError(Exception exception)
        {
            foreach (var channel in this.channels)
            {
                channel.OnReceiveError(exception);
            }
        }

        private protected override void OnBeforeCloseSession()
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
        /// Execute a callback on the worker thread when the connection
        /// is ready to send data.
        /// </summary>
        internal Task<TResult> RunAsync<TResult>(
            Func<Libssh2AuthenticatedSession, TResult> callback,
            bool terminateConnectionOnError = true)
        {
            if (!this.IsConnected)
            {
                throw new SshConnectionClosedException();
            }

            lock (this.sendQueue)
            {
                var packet = new SendOperation<TResult>(
                    callback, 
                    terminateConnectionOnError);
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
                return packet.Task;
            }
        }

        /// <summary>
        /// Execute a callback on the worker thread when the connection
        /// is ready to send data.
        /// </summary>
        internal Task RunAsync(
            Action<Libssh2AuthenticatedSession> callback,
            bool terminateConnectionOnError = true)
        {
            return RunAsync<object?>(
                s => {
                    callback(s);
                    return null;
                },
                terminateConnectionOnError);
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task ConnectAsync()
        {
            StartConnection();
            return this.connectionCompleted.Task;
        }

        public Task<SshShellChannel> OpenShellAsync(
            PseudoTerminalSize initialSize,
            string terminalType,
            CultureInfo? locale)
        {
            IEnumerable<EnvironmentVariable>? environmentVariables = null;
            if (locale != null)
            {
                //
                // Format language so that Linux understands it.
                //
                var languageFormatted = locale.Name.Replace('-', '_');
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

            return RunAsync(
                session => {
                    Debug.Assert(this.IsRunningOnWorkerThread);

                    using (session.Session.AsBlocking())
                    {
                        var nativeChannel = session.OpenShellChannel(
                            LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                            terminalType,
                            initialSize.Width,
                            initialSize.Height,
                            environmentVariables);

                        var channel = new SshShellChannel(
                            this,
                            nativeChannel);

                        this.channels.AddLast(channel);

                        return channel;
                    }
                },
                false);
        }

        public Task<SftpChannel> OpenFileSystemAsync()
        {
            return RunAsync(
                session => {
                    Debug.Assert(this.IsRunningOnWorkerThread);

                    using (session.Session.AsBlocking())
                    {
                        var channel = new SftpChannel(
                            this,
                            session.OpenSftpChannel());

                        this.channels.AddLast(channel);

                        return channel;
                    }
                });
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private interface ISendOperation
        {
            /// <summary>
            /// Run the operation.
            /// </summary>
            /// <remarks>
            /// Any exception thrown by this method causes
            /// the connection to be terminated.
            /// </remarks>
            void Run(Libssh2AuthenticatedSession session);

            /// <summary>
            /// Mark the operation as successful.
            /// </summary>
            void OnCompleted();

            /// <summary>
            /// Mark the operation as failed.
            /// </summary>
            void OnFailed(Exception e);
        }

        protected internal class SendOperation<TResult> : ISendOperation
        {
            private readonly TaskCompletionSource<TResult> completionSource;
            private readonly Func<Libssh2AuthenticatedSession, TResult> operation;
            private readonly bool terminateConnectionOnError;
            private TResult result = default!;
            private Exception? exception;

            internal SendOperation(
                Func<Libssh2AuthenticatedSession, TResult> operation,
                bool terminateConnectionOnError)
            {
                this.operation = operation;
                this.terminateConnectionOnError = terminateConnectionOnError;

                //
                // Force continuations to run asycnhronously so that they
                // don't block the worker thread.
                //
                this.completionSource = new TaskCompletionSource<TResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            void ISendOperation.Run(Libssh2AuthenticatedSession session)
            {
                try
                {
                    this.result = this.operation(session);
                }
                catch (Exception e) when (!this.terminateConnectionOnError)
                {
                    //
                    // Swallow exception here so that we keep the
                    // connection alive.
                    //
                    this.exception = e;
                }
            }

            void ISendOperation.OnCompleted()
            {
                //
                // Propagate exception or result to awaiters.
                //
                if (this.exception != null)
                {
                    this.completionSource.TrySetException(this.exception);
                }
                else
                {
                    this.completionSource.TrySetResult(this.result);
                }
            }

            void ISendOperation.OnFailed(Exception e)
            {
                this.completionSource.TrySetException(e);
            }

            /// <summary>
            /// Task to await completion.
            /// </summary>
            public Task<TResult> Task
            {
                get => this.completionSource.Task;
            }
        }
    }
}
