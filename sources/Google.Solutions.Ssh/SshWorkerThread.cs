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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Threading;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public abstract class SshWorkerThread : IDisposable
    {
        private readonly IPEndPoint endpoint;
        private readonly ISshCredential credential;
        private readonly IKeyboardInteractiveHandler keyboardHandler;

        private readonly Thread workerThread;
        private readonly CancellationTokenSource workerCancellationSource;

        private readonly WsaEventHandle readyToSend;

        private bool disposed;

        private static readonly RundownProtection workerThreadRundownProtection
            = new RundownProtection();

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Interval to send keep alive messages in. Longer intervals cause
        /// connection failures to be detected later.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan SocketWaitInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Timeout for blocking operations (used during connection phase).
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public bool JoinWorkerThreadOnDispose { get; set; } = true;

        public string? Banner { get; set; }

        internal bool IsRunningOnWorkerThread
            => Environment.CurrentManagedThreadId == this.workerThread.ManagedThreadId;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        protected SshWorkerThread(
            IPEndPoint endpoint,
            ISshCredential credential,
            IKeyboardInteractiveHandler keyboardHandler)
        {
            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.credential = credential.ExpectNotNull(nameof(credential));
            this.keyboardHandler = keyboardHandler.ExpectNotNull(nameof(keyboardHandler));

            this.readyToSend = NativeMethods.WSACreateEvent();

            this.workerCancellationSource = new CancellationTokenSource();
            this.workerThread = new Thread(WorkerThreadProc)
            {
                Name = $"SSH worker for {credential.Username}@{this.endpoint}",
                IsBackground = true
            };
        }

        //---------------------------------------------------------------------
        // Methods for subclasses.
        //---------------------------------------------------------------------

        protected void StartConnection()
        {
            if (this.workerThread.IsAlive)
            {
                throw new InvalidOperationException(
                    "Connect must only be called once");
            }

            this.workerThread.Start();
        }

        /// <summary>
        /// Handle an error from an SSH channel.
        /// 
        /// Called on worker thread, method should not block for any
        /// significant amount of time.
        /// </summary>
        private protected abstract void OnSendError(Exception exception);

        /// <summary>
        /// Handle an error from an SSH channel.
        /// 
        /// Called on worker thread, method should not block for any
        /// significant amount of time.
        /// </summary>
        private protected abstract void OnReceiveError(Exception exception);

        /// <summary>
        /// Called once after SSH connection has been established successfully.
        /// 
        /// Called on worker thread, method should not block for any
        /// significant amount of time.
        /// </summary>
        private protected abstract void OnConnected();

        /// <summary>
        /// Called once after SSH connection has failed.
        /// 
        /// Called on worker thread, method should not block for any
        /// significant amount of time.
        /// </summary>
        private protected abstract void OnConnectionError(Exception exception);

        /// <summary>
        /// Perform any operation that sends data.
        /// 
        /// Called on worker thread, method should not block for any
        /// significant amount of time.
        /// </summary>
        private protected abstract void OnReadyToSend(Libssh2AuthenticatedSession session);

        /// <summary>
        /// Perform any operation that sends data.
        /// 
        /// Called on worker thread, method should not block for any
        /// significant amount of time.
        /// </summary>
        private protected abstract void OnReadyToReceive(Libssh2AuthenticatedSession session);

        /// <summary>
        /// Close channels and other resources before session is closed.
        /// </summary>
        private protected abstract void OnBeforeCloseSession();

        protected bool IsConnected
            => this.workerThread.IsAlive &&
               !this.workerCancellationSource.IsCancellationRequested;

        /// <summary>
        /// Notify that data is available for sending.
        /// </summary>
        protected void NotifyReadyToSend(bool ready)
        {
            if (ready)
            {
                NativeMethods.WSASetEvent(this.readyToSend);
            }
            else
            {
                NativeMethods.WSAResetEvent(this.readyToSend);
            }
        }

        //---------------------------------------------------------------------
        // Worker thread
        //---------------------------------------------------------------------

        [Flags]
        private enum Operation
        {
            Sending,
            Receiving
        }

        private void WorkerThreadProc()
        {
            //
            // NB. libssh2 has limited support for multi-threading and in general,
            // it's best to use a libssh2 session from a single thread only. 
            // Therefore, all libssh2 operations are performed by this one thead.
            //

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                try
                {
                    using (workerThreadRundownProtection.Acquire())
                    using (var session = new Libssh2Session())
                    {
                        session.SetTraceHandler(
                            LIBSSH2_TRACE.SOCKET |
                                LIBSSH2_TRACE.ERROR |
                                LIBSSH2_TRACE.CONN |
                                LIBSSH2_TRACE.AUTH |
                                LIBSSH2_TRACE.KEX |
                                LIBSSH2_TRACE.SFTP,
                            SshTraceSource.Log.TraceVerbose);

                        if (!string.IsNullOrEmpty(this.Banner))
                        {
                            session.Banner = this.Banner!;
                        }

                        session.Timeout = this.ConnectionTimeout;

                        //
                        // Open connection and perform handshake using blocking I/O.
                        //
                        using (var connectedSession = session.Connect(this.endpoint))
                        using (var authenticatedSession = connectedSession.Authenticate(
                            this.credential,
                            this.keyboardHandler))
                        using (Disposable.Create(() => OnBeforeCloseSession()))
                        {
                            //
                            // Make sure the readyToSend handle remains valid throughout
                            // this thread's lifetime.
                            //
                            var readyToSendHandleSafeToUse = false;
                            this.readyToSend.DangerousAddRef(ref readyToSendHandleSafeToUse);
                            Debug.Assert(readyToSendHandleSafeToUse);

                            //
                            // With the channel established, switch to non-blocking I/O.
                            // Use a disposable scope to make sure that tearing down the 
                            // connection is done using blocking I/O again.
                            //
                            using (session.AsNonBlocking())
                            using (Disposable.Create(() => this.readyToSend.DangerousRelease()))
                            using (var readyToReceive = NativeMethods.WSACreateEvent())
                            {
                                //
                                // Create an event that is signalled whenever there is data
                                // available to read on the socket.
                                //
                                // NB. This is a manual-reset event that must be reset by 
                                // calling WSAEnumNetworkEvents.
                                //

                                if (NativeMethods.WSAEventSelect(
                                    connectedSession.Socket.Handle,
                                    readyToReceive,
                                    NativeMethods.FD_READ) != 0)
                                {
                                    throw new Win32Exception(
                                        NativeMethods.WSAGetLastError(),
                                        "WSAEventSelect failed");
                                }

                                //
                                // Looks good so far, consider the connection successful.
                                //
                                OnConnected();

                                //
                                // Set up keepalives. Because we use non-blocking I/O, we have to 
                                // send keepalives by ourselves.
                                //
                                // NB. This method must not be called before the handshake has completed.
                                //
                                connectedSession.ConfigureKeepAlive(false, this.KeepAliveInterval);

                                var waitHandles = new[]
                                {
                                    readyToReceive.DangerousGetHandle(),
                                    this.readyToSend.DangerousGetHandle()
                                };

                                while (!this.workerCancellationSource.IsCancellationRequested)
                                {
                                    var currentOperation = Operation.Receiving | Operation.Sending;

                                    try
                                    {
                                        // 
                                        // In each iteration, wait for 
                                        // (data received on socket) OR (user data to send)
                                        //
                                        // NB. The timeout should not be lower than approx.
                                        // one second, otherwise we spend too much time calling
                                        // libssh2's keepalive function, which causes the terminal
                                        // to become sluggish.
                                        //

                                        var waitResult = NativeMethods.WSAWaitForMultipleEvents(
                                            (uint)waitHandles.Length,
                                            waitHandles,
                                            false,
                                            (uint)this.SocketWaitInterval.TotalMilliseconds,
                                            false);

                                        if (waitResult == NativeMethods.WSA_WAIT_EVENT_0)
                                        {
                                            //
                                            // Socket has data available. 
                                            //
                                            currentOperation = Operation.Receiving;

                                            //
                                            // Reset the WSA event.
                                            //
                                            var wsaEvents = new NativeMethods.WSANETWORKEVENTS()
                                            {
                                                iErrorCode = new int[10]
                                            };

                                            if (NativeMethods.WSAEnumNetworkEvents(
                                                connectedSession.Socket.Handle,
                                                readyToReceive,
                                                ref wsaEvents) != 0)
                                            {
                                                throw new Win32Exception(
                                                    NativeMethods.WSAGetLastError(),
                                                    "WSAEnumNetworkEvents failed");
                                            }

                                            // 
                                            // Perform whatever receiving operation we need to do.
                                            //
                                            OnReadyToReceive(authenticatedSession);
                                        }
                                        else if (waitResult == NativeMethods.WSA_WAIT_EVENT_0 + 1)
                                        {
                                            //
                                            // User has data to send. Perform whatever send operation 
                                            // we need to do.
                                            // 
                                            currentOperation = Operation.Sending;
                                            OnReadyToSend(authenticatedSession);
                                        }
                                        else if (waitResult == NativeMethods.WSA_WAIT_TIMEOUT)
                                        {
                                            // 
                                            // Channel is idle - use the opportunity to send a 
                                            // keepalive. Libssh2 will ignore the call if no
                                            // keepalive is due yet.
                                            // 
                                            connectedSession.SendKeepAlive();
                                        }
                                        else if (waitResult == NativeMethods.WSA_WAIT_FAILED)
                                        {
                                            throw new Win32Exception(
                                                NativeMethods.WSAGetLastError(),
                                                "WSAWaitForMultipleEvents failed");
                                        }
                                    }
                                    catch (Libssh2Exception e) when (e.ErrorCode == LIBSSH2_ERROR.EAGAIN)
                                    {
                                        //
                                        // Retry operation.
                                        //
                                    }
                                    catch (Exception e)
                                    {
                                        SshTraceSource.Log.TraceError(
                                            "Socket I/O failed for {0}: {1}",
                                            Thread.CurrentThread.Name,
                                            e);

                                        if ((currentOperation & Operation.Sending) != 0)
                                        {
                                            OnSendError(e);
                                        }
                                        else
                                        {
                                            //
                                            // Consider it a receive error.
                                            //
                                            OnReceiveError(e);
                                        }

                                        //
                                        // Bail out.
                                        //
                                        return;
                                    }
                                } // while
                            } // nonblocking
                        }
                    }
                }
                catch (Exception e)
                {
                    SshTraceSource.Log.TraceError(
                        "Connection failed for {0}: {1}",
                        Thread.CurrentThread.Name,
                        e);

                    OnConnectionError(e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            // Stop worker thread.
            this.workerCancellationSource.Cancel();

            if (this.JoinWorkerThreadOnDispose)
            {
                Debug.Assert(
                    !this.IsRunningOnWorkerThread,
                    "Join on worker thread would cause deadlock");
                this.workerThread.Join();
            }

            if (!this.disposed && disposing)
            {
                this.readyToSend.Dispose();
                this.disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wait for all worker threads to complete. Typically only needed
        /// for test cases to prevent worker threads from being aborted.
        /// </summary>
        public static Task JoinAllWorkerThreadsAsync()
        {
            return workerThreadRundownProtection.WaitAsync();
        }
    }
}
