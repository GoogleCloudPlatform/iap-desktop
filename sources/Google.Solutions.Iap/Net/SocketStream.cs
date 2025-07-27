//
//
// Copyright 2019 Google LLC
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

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    /// A socket stream.
    /// </summary>
    public sea class SocketStream : INetworkStream
    {
        private readonly Socket socket;
        private readonly string remoteEndpoint;
        private readonly NetworkStatistics statistics;

        public SocketStream(Socket socket, NetworkStatistics statistics)
        {
            this.socket = socket;
            this.statistics = statistics;
            this.remoteEndpoint = socket.RemoteEndPoint.ToString();
        }

        private static void OnIoCompleted(
            TaskCompletionSource<int> tcs,
            Action<int> trackBytesTransferred,
            SocketAsyncEventArgs args)
        {
            switch (args.SocketError)
            {
                case SocketError.Success:
                    //
                    // Update statistics before releasing waiters.
                    //
                    trackBytesTransferred(args.BytesTransferred);

                    //
                    // Release waiters.
                    //
                    tcs.SetResult(args.BytesTransferred);
                    break;

                case SocketError.ConnectionAborted:
                    tcs.SetException(new NetworkStreamClosedException("Connection aborted"));
                    break;

                case SocketError.ConnectionReset:
                    tcs.SetException(new NetworkStreamClosedException("Connection reset"));
                    break;

                default:
                    tcs.SetException(new SocketException((int)args.SocketError));
                    break;
            }
        }

        protected static Task<int> IoAsync(
            Func<SocketAsyncEventArgs, bool> ioFunc,
            Action<int> trackBytesTransferred,
            SocketAsyncEventArgs eventArgs)
        {
            var tcs = new TaskCompletionSource<int>();

            eventArgs.Completed += (sender, args) =>
            {
                OnIoCompleted(tcs, trackBytesTransferred, args);
            };

            if (!ioFunc(eventArgs))
            {
                //
                // I/O completed synchronously.
                //
                OnIoCompleted(tcs, trackBytesTransferred, eventArgs);
            }

            return tcs.Task;
        }

        protected async Task<int> IoAsync(
            Func<SocketAsyncEventArgs, bool> ioFunc,
            Action<int> trackBytesTransferred,
            SocketAsyncEventArgs eventArgs,
            CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() =>
            {
                this.socket.Shutdown(SocketShutdown.Both);
            }))
            {
                try
                {
                    return await IoAsync(
                            ioFunc,
                            trackBytesTransferred,
                            eventArgs)
                        .ConfigureAwait(false);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
                {
                    throw new NetworkStreamClosedException("Operation aborted");
                }
            }
        }

        public override string ToString()
        {
            return $"[Socket {this.remoteEndpoint}]";
        }

        //---------------------------------------------------------------------
        // INetworkStream.
        //---------------------------------------------------------------------

        public async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            using (var args = new SocketAsyncEventArgs())
            {
                args.SetBuffer(buffer, offset, count);
                var bytesRead = await IoAsync(
                    this.socket.ReceiveAsync,
                    this.statistics.OnReceiveCompleted,
                    args,
                    cancellationToken).ConfigureAwait(false);

                return bytesRead;
            }
        }

        public async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            using (var args = new SocketAsyncEventArgs())
            {
                args.SetBuffer(buffer, offset, count);
                var bytesWritten = await IoAsync(
                    this.socket.SendAsync,
                    this.statistics.OnTransmitCompleted,
                    args,
                    cancellationToken).ConfigureAwait(false);

                Debug.Assert(bytesWritten == count);
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            using (var args = new SocketAsyncEventArgs())
            {
                await IoAsync(
                    this.socket.DisconnectAsync,
                    _ => { },
                    args,
                    cancellationToken).ConfigureAwait(false);
            }

            this.socket.Close();
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.socket.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
