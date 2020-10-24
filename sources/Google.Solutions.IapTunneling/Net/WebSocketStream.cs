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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1032 // Implement standard exception constructors

namespace Google.Solutions.IapTunneling.Net
{
    /// <summary>
    /// Stream that allows sending and receiving WebSocket messages at once
    /// so that the client does not have to deal with fragmentation.
    /// </summary>
    public class WebSocketStream : SingleReaderSingleWriterStream
    {
        private readonly ClientWebSocket socket;
        private readonly int maxReadMessageSize;

        private volatile bool closeByClientInitiated = false;
        private volatile WebSocketStreamClosedByServerException closeByServerReceived = null;

        public bool IsCloseInitiated => this.closeByClientInitiated;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public WebSocketStream(ClientWebSocket socket, int maxReadMessageSize)
        {
            if (socket.State != WebSocketState.Open)
            {
                throw new ArgumentException("Web socket must be open");
            }

            this.socket = socket;
            this.maxReadMessageSize = maxReadMessageSize;
        }

        //---------------------------------------------------------------------
        // Privates
        //---------------------------------------------------------------------

        private static bool IsSocketError(Exception caughtEx, SocketError error)
        {
            // ClientWebSocket throws almost arbitrary nestings of 
            // SocketExceptions, IOExceptions, WebSocketExceptions, all
            // wrapped in AggregrateExceptions.

            for (var ex = caughtEx; ex != null; ex = ex.InnerException)
            {
                if (ex is SocketException socketException)
                {
                    if (socketException.SocketErrorCode == error)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsWebSocketError(Exception caughtEx, WebSocketError error)
        {
            // ClientWebSocket throws almost arbitrary nestings of 
            // WebSocketException, IOExceptions, SocketExceptions, all
            // wrapped in AggregrateExceptions.

            for (var ex = caughtEx; ex != null; ex = ex.InnerException)
            {
                if (ex is WebSocketException socketException)
                {
                    if (socketException.WebSocketErrorCode == error)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //---------------------------------------------------------------------
        // SingleReaderSingleWriterStream implementation
        //---------------------------------------------------------------------

        public override int MaxWriteSize => int.MaxValue;
        public override int MinReadSize => this.maxReadMessageSize;

        private void VerifyConnectionNotClosedAlready()
        {
            if (this.closeByClientInitiated)
            {
                // Do not even try to send, it will not succeed anyway.
                throw new WebSocketStreamClosedByClientException();
            }
            else if (this.closeByServerReceived != null)
            {
                // Do not try to read, it cannot succeed anyway.
                throw this.closeByServerReceived;
            }
        }

        protected override async Task<int> ProtectedReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            VerifyConnectionNotClosedAlready();

            // Check buffer size. Zero-sized buffers are allowed for 
            // connection probing.
            if (count > 0 && count < this.MinReadSize)
            {
                throw new IndexOutOfRangeException($"Read buffer too small, must be at least {this.MinReadSize}");
            }

            try
            {
                int bytesReceived = 0;
                WebSocketReceiveResult result;
                do
                {
                    if (bytesReceived > 0 && bytesReceived == count)
                    {
                        throw new OverflowException("Buffer too small to receive an entire message");
                    }

                    TraceSources.Compute.TraceVerbose($"WebSocketStream: begin ReadAsync()... [socket: {this.socket.State}]");
                    result = await this.socket.ReceiveAsync(
                        new ArraySegment<byte>(
                            buffer,
                            offset + bytesReceived,
                            count - bytesReceived),
                        cancellationToken).ConfigureAwait(false);
                    bytesReceived += result.Count;

                    TraceSources.Compute.TraceVerbose($"WebSocketStream: end ReadAsync() - {result.Count} bytes read [socket: {this.socket.State}]");
                }
                while (count > 0 && !result.EndOfMessage);

                if (result.CloseStatus != null)
                {
                    Debug.Assert(bytesReceived == 0);

                    TraceSources.Compute.TraceVerbose($"WebSocketStream: Connection closed by server: {result.CloseStatus}");

                    this.closeByServerReceived = new WebSocketStreamClosedByServerException(
                        result.CloseStatus.Value,
                        result.CloseStatusDescription);

                    // In case of a normal close, it is preferable to simply return 0. But
                    // if the connection was closed abnormally, the client needs to know
                    // the details.
                    if (result.CloseStatus.Value != WebSocketCloseStatus.NormalClosure)
                    {
                        throw this.closeByServerReceived;
                    }
                    else
                    {
                        Debug.Assert(bytesReceived == 0);
                    }
                }

                return bytesReceived;
            }
            catch (Exception e) when (IsSocketError(e, SocketError.ConnectionAborted))
            {
                TraceSources.Compute.TraceVerbose($"WebSocketStream.Receive: connection aborted - {e}");

                // ClientWebSocket/WinHttp can also throw an exception if
                // the connection has been closed.

                this.closeByServerReceived = new WebSocketStreamClosedByServerException(
                    WebSocketCloseStatus.NormalClosure,
                    e.Message);

                throw this.closeByServerReceived;
            }
        }

        protected override async Task ProtectedWriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            VerifyConnectionNotClosedAlready();

            try
            {
                TraceSources.Compute.TraceVerbose($"WebSocketStream: begin WriteAsync({count} bytes)... [socket: {this.socket.State}]");
                await this.socket.SendAsync(
                    new ArraySegment<byte>(buffer, offset, count),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken).ConfigureAwait(false);
                TraceSources.Compute.TraceVerbose($"WebSocketStream: end WriteAsync()... [socket: {this.socket.State}]");
            }
            catch (Exception e) when (IsSocketError(e, SocketError.ConnectionAborted))
            {
                TraceSources.Compute.TraceVerbose($"WebSocketStream.Send: connection aborted - {e}");

                this.closeByServerReceived = new WebSocketStreamClosedByServerException(
                    WebSocketCloseStatus.NormalClosure,
                    e.Message);

                throw this.closeByServerReceived;
            }
        }
        public override async Task ProtectedCloseAsync(CancellationToken cancellationToken)
        {
            VerifyConnectionNotClosedAlready();

            try
            {
                this.closeByClientInitiated = true;
                await this.socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    string.Empty,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (
                IsWebSocketError(e, WebSocketError.InvalidMessageType) ||
                IsWebSocketError(e, WebSocketError.InvalidState) ||
                IsSocketError(e, SocketError.ConnectionAborted))
            {
                // Server already closed the connection - nevermind then.
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.socket.Dispose();
        }
    }

    [Serializable]
    public class WebSocketStreamClosedByClientException : NetworkStreamClosedException
    {
        protected WebSocketStreamClosedByClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public WebSocketStreamClosedByClientException()
        {
        }
    }

    [Serializable]
    public class WebSocketStreamClosedByServerException : NetworkStreamClosedException
    {
        public WebSocketCloseStatus CloseStatus { get; private set; }
        public string CloseStatusDescription { get; private set; }

        public WebSocketStreamClosedByServerException(
            WebSocketCloseStatus closeStatus,
            string closeStatusDescription)
        {
            this.CloseStatus = closeStatus;
            this.CloseStatusDescription = closeStatusDescription;
        }

        protected WebSocketStreamClosedByServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.CloseStatus = (WebSocketCloseStatus)info.GetInt32("CloseStatus");
            this.CloseStatusDescription = info.GetString("CloseStatusDescription");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("CloseStatus", this.CloseStatus);
            info.AddValue("CloseStatusDescription", this.CloseStatusDescription);
            base.GetObjectData(info, context);
        }
    }
}
