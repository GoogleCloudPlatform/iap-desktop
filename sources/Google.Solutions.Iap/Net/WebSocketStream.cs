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
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    /// Stream that allows sending and receiving WebSocket frames.
    /// </summary>
    public class WebSocketStream : SingleReaderSingleWriterStream
    {
        private readonly ClientWebSocket socket;

        private volatile bool closeByClientInitiated = false;

        public bool IsCloseInitiated => this.closeByClientInitiated;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public WebSocketStream(ClientWebSocket socket)
        {
            if (socket.State != WebSocketState.Open)
            {
                throw new ArgumentException("Web socket must be open");
            }

            this.socket = socket.ExpectNotNull(nameof(socket));
        }

        //---------------------------------------------------------------------
        // Privates
        //---------------------------------------------------------------------

        private static bool IsSocketError(Exception caughtEx, SocketError error)
        {
            // ClientWebSocket throws almost arbitrary nestings of 
            // SocketExceptions, IOExceptions, WebSocketExceptions, all
            // wrapped in AggregateExceptions.

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
            // wrapped in AggregateExceptions.

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

        private void VerifyConnectionNotClosedAlready()
        {
            if (this.closeByClientInitiated)
            {
                // Do not even try to send, it will not succeed anyway.
                throw new WebSocketStreamClosedByClientException();
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        /// <summary>
        /// Read until (a) the buffer is full or (b) the end of
        /// the frame has been reached.
        /// </summary>
        protected override async Task<int> ReadCoreAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            VerifyConnectionNotClosedAlready();

            try
            {
                var bytesReceived = 0;
                var bytesLeftInBuffer = count;
                WebSocketReceiveResult result;
                do
                {
                    IapTraceSource.Log.TraceVerbose(
                        "WebSocketStream: begin ReadAsync()... [socket: {0}]",
                        this.socket.State);

                    result = await this.socket.ReceiveAsync(
                            new ArraySegment<byte>(
                                buffer,
                                offset + bytesReceived,
                                bytesLeftInBuffer),
                            cancellationToken)
                        .ConfigureAwait(false);

                    bytesReceived += result.Count;
                    bytesLeftInBuffer -= result.Count;

                    Debug.Assert(bytesReceived + bytesLeftInBuffer == count);

                    IapTraceSource.Log.TraceVerbose(
                        "WebSocketStream: end ReadAsync() - {0} bytes read [socket: {1}]",
                        result.Count,
                        this.socket.State);
                }
                while (bytesLeftInBuffer > 0 && !result.EndOfMessage);

                if (result.CloseStatus != null)
                {
                    Debug.Assert(bytesReceived == 0);

                    IapTraceSource.Log.TraceVerbose(
                        "WebSocketStream: Connection closed by server: {0}",
                        result.CloseStatus);

                    //
                    // In case of a normal close, it is preferable to simply
                    // return 0. But if the connection was closed abnormally,
                    // the client needs to know the details.
                    //
                    if (result.CloseStatus.Value 
                        != WebSocketCloseStatus.NormalClosure)
                    {
                        throw new WebSocketStreamClosedByServerException(
                            result.CloseStatus.Value,
                            result.CloseStatusDescription);
                    }
                    else
                    {
                        Debug.Assert(bytesReceived == 0);
                    }
                }

                return bytesReceived;
            }
            catch (Exception e) when (
                IsSocketError(e, SocketError.ConnectionAborted) ||
                IsWebSocketError(e, WebSocketError.ConnectionClosedPrematurely))
            {
                IapTraceSource.Log.TraceVerbose(
                    "WebSocketStream.Read: connection aborted - {0}", e);

                throw new WebSocketStreamClosedByServerException(
                    (WebSocketCloseStatus)1006, // Abnormal closure
                    e.Message);
            }
        }

        protected override async Task WriteCoreAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            VerifyConnectionNotClosedAlready();

            try
            {
                IapTraceSource.Log.TraceVerbose(
                    "WebSocketStream: begin WriteAsync({0} bytes)... [socket: {1}]",
                    count,
                    this.socket.State);

                await this.socket.SendAsync(
                        new ArraySegment<byte>(buffer, offset, count),
                        WebSocketMessageType.Binary,
                        true,
                        cancellationToken)
                    .ConfigureAwait(false);

                IapTraceSource.Log.TraceVerbose(
                    "WebSocketStream: end WriteAsync()... [socket: {0}]",
                    this.socket.State);
            }
            catch (Exception e) when (
                IsSocketError(e, SocketError.ConnectionAborted) ||
                IsWebSocketError(e, WebSocketError.ConnectionClosedPrematurely))
            {
                IapTraceSource.Log.TraceVerbose(
                    "WebSocketStream.Write: connection aborted - {0}", e);

                throw new WebSocketStreamClosedByServerException(
                    (WebSocketCloseStatus)1006, // Abnormal closure
                    e.Message);
            }
        }

        public override async Task CloseCoreAsync(
            CancellationToken cancellationToken)
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
                //
                // Server already closed the connection - nevermind then.
                //
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.socket.Dispose();
        }
    }

    public class WebSocketStreamClosedByClientException : 
        NetworkStreamClosedException
    {
        public WebSocketStreamClosedByClientException()
            : base("The connection has already been closed by the client")
        {
        }
    }

    public class WebSocketConnectionDeniedException : Exception
    {
        public WebSocketConnectionDeniedException()
            : base("The server denied the use of WebSockets")
        {
        }
    }

    public class WebSocketStreamClosedByServerException : 
        NetworkStreamClosedException
    {
        public WebSocketCloseStatus CloseStatus { get; private set; }
        public string CloseStatusDescription { get; private set; }

        public WebSocketStreamClosedByServerException(
            WebSocketCloseStatus closeStatus,
            string closeStatusDescription)
            : base($"{closeStatusDescription} (code {closeStatus})")
        {
            this.CloseStatus = closeStatus;
            this.CloseStatusDescription = closeStatusDescription;
        }
    }
}
