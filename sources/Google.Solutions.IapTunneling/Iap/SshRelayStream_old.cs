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
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Factory for creating (Web Socket) connections to the tunneling 
    /// endpoint (for ex, Cloud IAP).
    /// </summary>
    public interface ISshRelayEndpoint__OLD
    {
        Task<INetworkStream> ConnectAsync(CancellationToken token);

        Task<INetworkStream> ReconnectAsync(
            string sid,
            ulong lastByteConsumedByClient,
            CancellationToken token);
    }

    /// <summary>
    /// NetworkStream that implements the SSH Relay v4 protocol. Because
    /// the protocol supports connection reestablishment, a SshRelayStream
    /// needs to be created by using a ISshRelayEndpoint, which serves
    /// as the factory for underlying WebSocket connections.
    /// </summary>
    public class SshRelayStream__OLD : SingleReaderSingleWriterStream
    {
        private readonly ISshRelayEndpoint endpoint;
        private readonly SemaphoreSlim connectSemaphore = new SemaphoreSlim(1);

        // Queue of un-ack'ed messages that might require re-sending.
        private readonly object sentButUnacknoledgedQueueLock = new object();
        private readonly Queue<UnacknoledgedWrite> unacknoledgedQueue = new Queue<UnacknoledgedWrite>();

        public string Sid { get; private set; }

        // Current connection, not to be accessed directly.
        private INetworkStream __currentConnection = null;

        // Connection statistics, volatile.
        private ulong bytesSent = 0;
        private ulong bytesSentAndAcknoledged = 0;
        private ulong bytesReceived = 0;

        private ulong lastAckSent = 0;

        internal ulong ExpectedAck
        {
            get
            {
                lock (this.sentButUnacknoledgedQueueLock)
                {
                    return this.unacknoledgedQueue.Any()
                        ? this.unacknoledgedQueue.Last().ExpectedAck
                        : 0;
                }
            }
        }

        internal int UnacknoledgedMessageCount
        {
            get
            {
                lock (this.sentButUnacknoledgedQueueLock)
                {
                    return this.unacknoledgedQueue.Count;
                }
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public SshRelayStream__OLD(ISshRelayEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        //---------------------------------------------------------------------
        // Privates
        //---------------------------------------------------------------------

        private void TraceLine(string message)
        {
            if (IapTraceSources.Default.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                IapTraceSources.Default.TraceVerbose(
                    "SshRelayStream [TX: {0} TXA: {1} RX: {2} AQ: {3}]: {4}",
                    Thread.VolatileRead(ref this.bytesSent),
                    Thread.VolatileRead(ref this.bytesSentAndAcknoledged),
                    Thread.VolatileRead(ref this.bytesReceived),
                    this.UnacknoledgedMessageCount,
                    message);
            }
        }

        private async Task<INetworkStream> ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                //
                // Acquire semaphore to ensure that only a single
                // connect operation is in flight at a time.
                //
                await this.connectSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (this.__currentConnection == null)
                {
                    //
                    // Connect first.
                    //
                    if (Thread.VolatileRead(ref this.bytesReceived) == 0)
                    {
                        //
                        // First attempt to open a connection. 
                        //
                        TraceLine("Connecting...");
                        this.__currentConnection =
                            await this.endpoint.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        //
                        // We had a communication breakdown, try establishing a new connection.
                        //
                        Debug.Assert(this.Sid != null);

                        //
                        // Restart the transmission where the client left off consuming. 
                        // Depending on when the connection broke down, the ACK for the consumed
                        // data may or may not have been successfully transmitted back to the server.
                        //

                        TraceLine("Reconnecting...");
                        this.__currentConnection = await this.endpoint.ReconnectAsync(
                            this.Sid,
                            Thread.VolatileRead(ref this.bytesReceived),
                            cancellationToken).ConfigureAwait(false);
                    }

                    //
                    // Resend any un-ack'ed data.
                    //
                    while (true)
                    {
                        Task resendMessage = null;
                        lock (this.sentButUnacknoledgedQueueLock)
                        {
                            if (!this.unacknoledgedQueue.Any())
                            {
                                break;
                            }

                            var write = this.unacknoledgedQueue.Dequeue();

                            TraceLine($"Resending DATA #{write.SequenceNumber}...");
                            resendMessage = this.__currentConnection.WriteAsync(
                                write.Data,
                                0,
                                write.Data.Length,
                                cancellationToken);
                        }

                        Debug.Assert(resendMessage != null);
                        await resendMessage.ConfigureAwait(false);
                    }
                }

                //
                // NB. The first receive should be a CONNECT_SUCCESS_SID or
                // RECONNECT_SUCCESS_ACK message. There is no strong reason
                // for eagerly trying to receive these messages here.
                //

                return this.__currentConnection;
            }
            finally
            {
                this.connectSemaphore.Release();
            }
        }

        private async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                //
                // Acquire semaphore to ensure that only a single
                // connect operation is in flight at a time.
                //
                await this.connectSemaphore
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                //
                // Drop this connection.
                //
                try
                {
                    if (this.__currentConnection != null)
                    {
                        await this.__currentConnection
                            .CloseAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    TraceLine($"Failed to close connection: {e}");
                }

                try
                {
                    this.__currentConnection.Dispose();
                }
                catch (Exception e)
                {
                    TraceLine($"Failed to dispose connection: {e}");
                }

                this.__currentConnection = null;

                TraceLine("Disonnected.");
            }
            finally
            {
                this.connectSemaphore.Release();
            }
        }

        public async Task TestConnectionAsync(TimeSpan timeout)
        {
            //
            // Open a WebSocketStream, without wrapping it as a SshRelayStream
            // and do a zero-byte read. This will fail if access is denied.
            //
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    //
                    // If access to the instance is allowed, but the instance
                    // simply does not listen on this port, the connect or read 
                    // will hang. Therefore, apply a timeout.
                    //
                    cts.CancelAfter(timeout);

                    using (var connection = await this.endpoint
                        .ConnectAsync(cts.Token)
                        .ConfigureAwait(false))
                    {
                        await connection
                            .ReadAsync(Array.Empty<byte>(), 0, 0, cts.Token)
                            .ConfigureAwait(false);
                        await connection
                            .CloseAsync(cts.Token)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (WebSocketStreamClosedByServerException e)
                when ((SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.NOT_AUTHORIZED ||
                      (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.LOOKUP_FAILED ||
                      (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.LOOKUP_FAILED_RECONNECT)
            {
                //
                // Request was rejected by access level or IAM policy.
                //
                throw new SshRelayDeniedException(e.CloseStatusDescription);
            }
            catch (OperationCanceledException)
            {
                throw new NetworkStreamClosedException("Connection timed out");
            }
        }

        /// <summary>
        /// Maximum amount of data (in byte) that can be written at once.
        /// </summary>
        public const int MaxWriteSize = (int)SshRelayFormat.Data.MaxPayloadLength;

        /// <summary>
        /// Minimum amount of data (in byte) that can be read at once.
        /// </summary>
        public const int MinReadSize = (int)SshRelayFormat.Data.MaxPayloadLength;

        //---------------------------------------------------------------------
        // SingleReaderSingleWriterStream implementation
        //---------------------------------------------------------------------

        protected override async Task<int> ProtectedReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (count < MinReadSize)
            {
                throw new IndexOutOfRangeException(
                    $"Read buffer too small ({count}), must be at least {MinReadSize}");
            }

            var message = new byte[Math.Max(
                SshRelayFormat.MinMessageSize, 
                SshRelayFormat.Data.HeaderLength + count)];

            while (true)
            {
                try
                {
                    var connection = await ConnectAsync(cancellationToken)
                        .ConfigureAwait(false);

                    int bytesRead = await connection
                        .ReadAsync(
                            message,
                            0,
                            message.Length,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        TraceLine("Server closed connection");

                        return 0;
                    }
                    else if (bytesRead < 2)
                    {
                        throw new InvalidServerResponseException("Truncated message received");
                    }

                    var bytesDecoded = SshRelayFormat.Tag.Decode(message, out var tag);
                    
                    switch (tag)
                    {
                        case SshRelayMessageTag.CONNECT_SUCCESS_SID:
                            {
                                bytesDecoded = SshRelayFormat.ConnectSuccessSid.Decode(message, out var sid);
                                this.Sid = sid;

                                TraceLine($"Connected session <{this.Sid}>");
                                Debug.Assert(bytesDecoded == bytesRead);

                                break;
                            }

                        case SshRelayMessageTag.RECONNECT_SUCCESS_ACK:
                            {
                                bytesDecoded = SshRelayFormat.ReconnectAck.Decode(message, out var ack);
                                
                                var lastAckReceived = Thread.VolatileRead(ref this.bytesSentAndAcknoledged);
                                if (lastAckReceived < ack)
                                {
                                    TraceLine("Last ACK sent by server was not received");

                                    this.bytesSentAndAcknoledged = ack;
                                }
                                else if (lastAckReceived > ack)
                                {
                                    Debug.Assert(false, "Server acked backwards");
                                    throw new InvalidServerResponseException("Server acked backwards");
                                }
                                else
                                {
                                    TraceLine($"Reconnected session <{this.Sid}>");
                                }

                                Debug.Assert(bytesDecoded == bytesRead);

                                break;
                            }

                        case SshRelayMessageTag.ACK:
                            {
                                bytesDecoded = SshRelayFormat.Ack.Decode(message, out var ack);

                                if (ack <= 0 || ack > Thread.VolatileRead(ref this.bytesSent))
                                {
                                    throw new InvalidServerResponseException("Received invalid ACK");
                                }

                                this.bytesSentAndAcknoledged = ack;

                                lock (this.sentButUnacknoledgedQueueLock)
                                {
                                    //
                                    // The server might be acknolodging multiple messages at once.
                                    //
                                    while (this.unacknoledgedQueue.Count > 0 &&
                                           this.unacknoledgedQueue.Peek().ExpectedAck
                                                <= Thread.VolatileRead(ref this.bytesSentAndAcknoledged))
                                    {
                                        this.unacknoledgedQueue.Dequeue();
                                    }
                                }

                                TraceLine($"Received ACK #{ack}");
                                Debug.Assert(bytesDecoded == bytesRead);

                                break;
                            }

                        case SshRelayMessageTag.DATA:
                            {
                                bytesDecoded = SshRelayFormat.Data.Decode(
                                    message, 
                                    buffer,
                                    (uint)offset,
                                    (uint)count,
                                    out var dataLength);

                                TraceLine($"Received data ({dataLength} bytes)");

                                Debug.Assert(dataLength < bytesDecoded);
                                this.bytesReceived += dataLength;

                                Debug.Assert(bytesDecoded == bytesRead);

                                return (int)dataLength;
                            }

                        case SshRelayMessageTag.LONG_CLOSE:
                            {
                                bytesDecoded = SshRelayFormat.LongClose.Decode(
                                   message,
                                   out var closeCode,
                                   out var reason);

                                //
                                // Ignore the message for now.
                                // 

                                TraceLine($"Received close: {reason} ({closeCode})");

                                Debug.Assert(bytesDecoded == bytesRead);

                                break;
                            }

                        default:
                            {
                                //
                                // An unrecognized tag merely
                                // means that the server uses a feature that we do not support (yet).
                                // In accordance with the protocol specification, ignore this tag.
                                //
                                break;
                            }
                    }
                }
                catch (WebSocketStreamClosedByClientException)
                {
                    TraceLine("Detected attempt to read after close");
                    throw;
                }
                catch (WebSocketStreamClosedByServerException e)
                {
                    if (e.CloseStatus == WebSocketCloseStatus.NormalClosure ||
                        (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.DESTINATION_READ_FAILED ||
                        (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.DESTINATION_WRITE_FAILED)
                    {
                        TraceLine("Server closed connection");

                        //
                        // Server closed the connection normally, we are done here
                        //

                        return 0;
                    }
                    else if ((SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.NOT_AUTHORIZED)
                    {
                        TraceLine("Not authorized");

                        throw new SshRelayDeniedException(e.CloseStatusDescription);
                    }
                    else if ((SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.SID_UNKNOWN ||
                             (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.SID_IN_USE)
                    {
                        //
                        // Failed reconect attempt - do not try again.
                        //
                        TraceLine("Sid unknown or in use");

                        throw new WebSocketStreamClosedByServerException(
                            e.CloseStatus,
                            "Connection closed abnormally and an attempt to reconnect was rejected");
                    }
                    else if ((SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.FAILED_TO_CONNECT_TO_BACKEND)
                    {
                        //
                        // Server probably not listening.
                        //
                        TraceLine("Failed to connect to backend");

                        throw new WebSocketStreamClosedByServerException(
                            e.CloseStatus,
                            "Failed to connect to backend");
                    }
                    else
                    {
                        TraceLine($"Connection closed abnormally: {(SshRelayCloseCode)e.CloseStatus}");

                        TraceLine("Disconnecting, preparing to reconnect");
                        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        protected override async Task ProtectedWriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (count > MaxWriteSize)
            {
                throw new IndexOutOfRangeException(
                    $"Write buffer too large ({count}), must be at most {MaxWriteSize}");
            }

            while (true)
            {
                try
                {
                    var connection = await ConnectAsync(cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // Take care of outstanding ACKs.
                    //
                    var bytesToAck = Thread.VolatileRead(ref this.bytesReceived);
                    if (this.lastAckSent < bytesToAck)
                    {
                        var ackBuffer = new byte[SshRelayFormat.Ack.MessageLength];
                        SshRelayFormat.Ack.Encode(ackBuffer, bytesToAck);

                        TraceLine($"Sending ACK #{bytesToAck}...");

                        await connection
                            .WriteAsync(
                                ackBuffer,
                                0,
                                ackBuffer.Length,
                                cancellationToken)
                            .ConfigureAwait(false);

                        this.lastAckSent = bytesToAck;
                    }

                    //
                    // Send data.
                    //
                    var sequenceNumber = Thread.VolatileRead(ref this.bytesSent);

                    var message = new byte[SshRelayFormat.Data.HeaderLength + count];
                    SshRelayFormat.Data.Encode(message, buffer, (uint)offset, (uint)count);

                    TraceLine($"Sending DATA #{sequenceNumber}...");

                    //
                    // Update bytesSent before we write the data to the wire,
                    // otherwise we might see an ACK before bytesSent even reflects
                    // that the data has been sent.
                    //
                    this.bytesSent = Thread.VolatileRead(ref this.bytesSent) + (ulong)count;

                    await connection
                        .WriteAsync(
                            message,
                            0,
                            message.Length,
                            cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // We should get an ACK for this message.
                    //
                    lock (this.sentButUnacknoledgedQueueLock)
                    {
                        this.unacknoledgedQueue.Enqueue(new UnacknoledgedWrite(
                            message,
                            sequenceNumber,
                            sequenceNumber + (ulong)count));
                    }

                    return;
                }
                catch (WebSocketStreamClosedByServerException e)
                {
                    if (e.CloseStatus == WebSocketCloseStatus.NormalClosure ||
                            (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.DESTINATION_READ_FAILED ||
                            (SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.DESTINATION_WRITE_FAILED)
                    {
                        //
                        // The server closed the connection and us sending more data
                        // really seems unexpected.
                        //
                        throw;
                    }
                    else if ((SshRelayCloseCode)e.CloseStatus == SshRelayCloseCode.NOT_AUTHORIZED)
                    {
                        TraceLine("NOT_AUTHORIZED");

                        throw new SshRelayDeniedException(e.CloseStatusDescription);
                    }
                    else
                    {
                        TraceLine($"Connection closed abnormally: {(SshRelayCloseCode)e.CloseStatus}");

                        TraceLine("Disconnecting, preparing to reconnect");
                        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        public override Task ProtectedCloseAsync(CancellationToken cancellationToken)
        {
            return DisconnectAsync(cancellationToken);
        }

        public override string ToString()
        {
            var sidToken = this.Sid != null ? this.Sid.Substring(0, 10) : "(unknown)";
            return $"[SshRelay {sidToken}]";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.connectSemaphore.Dispose();
        }

        private struct UnacknoledgedWrite
        {
            public readonly byte[] Data;
            public readonly ulong SequenceNumber;
            public readonly ulong ExpectedAck;

            public UnacknoledgedWrite(
                byte[] data, 
                ulong sequenceNumber,
                ulong expectedAck)
            {
                this.Data = data;
                this.SequenceNumber = sequenceNumber;
                this.ExpectedAck = expectedAck;
            }
        }
    }

    //[Serializable]
    //public class SshRelayException : Exception
    //{
    //    protected SshRelayException(SerializationInfo info, StreamingContext context)
    //        : base(info, context)
    //    {
    //    }

    //    public SshRelayException(string message) : base(message)
    //    {
    //    }
    //}

    [Serializable]
    public class InvalidServerResponseException : Exception
    {
        protected InvalidServerResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InvalidServerResponseException(string message) : base(message)
        {
        }
    }

    //[Serializable]
    //public class UnauthorizedException : SshRelayException
    //{
    //    protected UnauthorizedException(SerializationInfo info, StreamingContext context)
    //        : base(info, context)
    //    {
    //    }

    //    public UnauthorizedException(string message) : base(message)
    //    {
    //    }
    //}
}
