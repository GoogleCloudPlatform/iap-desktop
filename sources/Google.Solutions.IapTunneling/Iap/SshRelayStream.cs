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

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable CA2201 // Do not raise reserved exception types

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Factory for creating (Web Socket) connections to the tunneling 
    /// endpoint (for ex, Cloud IAP).
    /// </summary>
    public interface ISshRelayEndpoint
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
    public class SshRelayStream : SingleReaderSingleWriterStream
    {
        private readonly ISshRelayEndpoint endpoint;
        private readonly SemaphoreSlim connectSemaphore = new SemaphoreSlim(1);

        // Queue of un-ack'ed messages that might require re-sending.
        private readonly object sentButUnacknoledgedQueueLock = new object();
        private readonly Queue<DataMessage> sentButUnacknoledgedQueue = new Queue<DataMessage>();

        public string Sid { get; private set; }

        // Current connection, not to be accessed directly.
        private INetworkStream __currentConnection = null;

        // Connection statistics, volatile.
        private ulong bytesSent = 0;
        private ulong bytesSentAndAcknoledged = 0;
        private ulong bytesReceived = 0;

        private ulong lastAck = 0;

        internal ulong ExpectedAck
        {
            get
            {
                lock (this.sentButUnacknoledgedQueueLock)
                {
                    return this.sentButUnacknoledgedQueue.Any()
                        ? this.sentButUnacknoledgedQueue.Last().ExpectedAck
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
                    return this.sentButUnacknoledgedQueue.Count;
                }
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public SshRelayStream(ISshRelayEndpoint endpoint)
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
                // Acquire semaphore to ensure that only a single
                // connect operation is in flight at a time.
                await this.connectSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (this.__currentConnection == null)
                {
                    // Connect first.
                    if (Thread.VolatileRead(ref this.bytesReceived) == 0)
                    {
                        // First attempt to open a connection. 
                        TraceLine("Connecting...");
                        this.__currentConnection =
                            await this.endpoint.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // We had a communication breakdown, try establishing a new connection.

                        Debug.Assert(this.Sid != null);

                        // Restart the transmission where the client left off consuming. 
                        // Depending on when the connection broke down, the ACK for the consumed
                        // data may or may not have been successfully transmitted back to the server.

                        TraceLine("Reconnecting...");
                        this.__currentConnection = await this.endpoint.ReconnectAsync(
                            this.Sid,
                            Thread.VolatileRead(ref this.bytesReceived),
                            cancellationToken).ConfigureAwait(false);
                    }

                    // Resend any un-ack'ed data.
                    while (true)
                    {
                        Task resendMessage = null;
                        lock (this.sentButUnacknoledgedQueueLock)
                        {
                            if (!this.sentButUnacknoledgedQueue.Any())
                            {
                                break;
                            }

                            var message = this.sentButUnacknoledgedQueue.Dequeue();

                            TraceLine($"Resending DATA #{message.SequenceNumber}...");
                            resendMessage = this.__currentConnection.WriteAsync(
                                message.Buffer,
                                0,
                                message.BufferLength,
                                cancellationToken);
                        }

                        Debug.Assert(resendMessage != null);
                        await resendMessage.ConfigureAwait(false);
                    }

                    // The first receive should be a CONNECT_SUCCESS_SID or
                    // RECONNECT_SUCCESS_ACK message. There is no strong reason
                    // for eagerly trying to recive these messages here.
                }

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
                // Acquire semaphore to ensure that only a single
                // connect operation is in flight at a time.
                await this.connectSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Drop this connection.
                try
                {
                    if (this.__currentConnection != null)
                    {
                        await this.__currentConnection.CloseAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    TraceLine($"Failed to close connection: {e}");
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
            // Open a WebSocketStream, without wrapping it as a SshRelayStream
            // and do a zero-byte read. This will fail if access is denied.
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    // If access to the instance is allowed, but the instance
                    // simply does not listen on this port, the connect or read 
                    // will hang. Therefore, apply a timeout.
                    cts.CancelAfter(timeout);

                    var connection = await this.endpoint.ConnectAsync(cts.Token).ConfigureAwait(false);
                    await connection.ReadAsync(Array.Empty<byte>(), 0, 0, cts.Token).ConfigureAwait(false);
                    await connection.CloseAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (WebSocketStreamClosedByServerException e)
                when ((CloseCode)e.CloseStatus == CloseCode.NOT_AUTHORIZED)
            {
                throw new UnauthorizedException(e.CloseStatusDescription);
            }
            catch (OperationCanceledException)
            {
                throw new NetworkStreamClosedException("Connection timed out");
            }
        }

        //---------------------------------------------------------------------
        // SingleReaderSingleWriterStream implementation
        //---------------------------------------------------------------------

        public override int MaxWriteSize => 16 * 1024;
        public override int MinReadSize => (int)DataMessage.MaxDataLength;

        protected override async Task<int> ProtectedReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (count < this.MinReadSize)
            {
                throw new IndexOutOfRangeException(
                    $"Read buffer too small ({count}), must be at least {this.MinReadSize}");
            }

            MessageBuffer receiveBuffer = new MessageBuffer(new byte[count + DataMessage.DataOffset]);

            while (true)
            {
                try
                {
                    var connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);

                    int bytesRead = await connection.ReadAsync(
                        receiveBuffer.Buffer,
                        0,
                        receiveBuffer.Buffer.Length,
                        cancellationToken).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        TraceLine("Server closed connection");

                        // We are done here
                        return 0;
                    }
                    else if (bytesRead < 2)
                    {
                        throw new InvalidServerResponseException("Truncated message received");
                    }

                    var tag = receiveBuffer.PeekMessageTag();
                    switch (tag)
                    {
                        case MessageTag.CONNECT_SUCCESS_SID:
                            {
                                this.Sid = receiveBuffer.AsSidMessage().Sid;

                                TraceLine($"Connected session <{this.Sid}>");

                                // No data to return to client yet.
                                break;
                            }

                        case MessageTag.RECONNECT_SUCCESS_ACK:
                            {
                                var reconnectMessage = receiveBuffer.AsAckMessage();
                                var lastAckReceived = Thread.VolatileRead(ref this.bytesSentAndAcknoledged);
                                if (lastAckReceived < reconnectMessage.Ack)
                                {
                                    TraceLine("Last ACK sent by server was not received");

                                    bytesSentAndAcknoledged = reconnectMessage.Ack;
                                }
                                else if (lastAckReceived > reconnectMessage.Ack)
                                {
                                    Debug.Assert(false, "Server acked backwards");
                                    throw new Exception("Server acked backwards");
                                }
                                else
                                {
                                    TraceLine($"Reconnected session <{this.Sid}>");
                                }

                                // No data to return to client yet.
                                break;
                            }

                        case MessageTag.ACK:
                            {
                                var ackMessage = receiveBuffer.AsAckMessage();
                                if (ackMessage.Ack <= 0 || ackMessage.Ack > Thread.VolatileRead(ref this.bytesSent))
                                {
                                    throw new InvalidServerResponseException("Received invalid ACK");
                                }

                                this.bytesSentAndAcknoledged = ackMessage.Ack;

                                lock (this.sentButUnacknoledgedQueueLock)
                                {
                                    // The server might be acknolodging multiple messages at once.
                                    while (this.sentButUnacknoledgedQueue.Count > 0 &&
                                           this.sentButUnacknoledgedQueue.Peek().ExpectedAck
                                                <= Thread.VolatileRead(ref this.bytesSentAndAcknoledged))
                                    {
                                        this.sentButUnacknoledgedQueue.Dequeue();
                                    }
                                }

                                TraceLine($"Received ACK #{ackMessage.Ack}");

                                // No data to return to client yet.
                                break;
                            }

                        case MessageTag.DATA:
                            {
                                var dataMessage = receiveBuffer.AsDataMessage();

                                TraceLine($"Received data ({dataMessage.DataLength} bytes)");

                                bytesReceived += dataMessage.DataLength;

                                // Copy data to caller's buffer.
                                Debug.Assert(dataMessage.DataLength < count);
                                Array.Copy(
                                    receiveBuffer.Buffer,
                                    DataMessage.DataOffset,
                                    buffer,
                                    offset,
                                    dataMessage.DataLength);
                                return (int)dataMessage.DataLength;
                            }

                        default:
                            if (this.Sid == null)
                            {
                                // An unrecognized tag at the start of a connection means that we are
                                // essentially reading junk, so bail out.
                                throw new InvalidServerResponseException($"Unknown tag: {tag}");
                            }
                            else
                            {
                                // The connection was properly opened - an unrecognized tag merely
                                // means that the server uses a feature that we do not support (yet).
                                // In accordance with the protocol specification, ignore this tag.
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
                            (CloseCode)e.CloseStatus == CloseCode.DESTINATION_READ_FAILED ||
                            (CloseCode)e.CloseStatus == CloseCode.DESTINATION_WRITE_FAILED)
                    {
                        TraceLine("Server closed connection");

                        // Server closed the connection normally, we are done here

                        return 0;
                    }
                    else if ((CloseCode)e.CloseStatus == CloseCode.NOT_AUTHORIZED)
                    {
                        TraceLine("Not authorized");

                        throw new UnauthorizedException(e.CloseStatusDescription);
                    }
                    else if ((CloseCode)e.CloseStatus == CloseCode.SID_UNKNOWN ||
                             (CloseCode)e.CloseStatus == CloseCode.SID_IN_USE)
                    {
                        // Failed reconect attempt - do not try again.
                        TraceLine("Sid unknown or in use");

                        throw new WebSocketStreamClosedByServerException(
                            e.CloseStatus,
                            "Connection losed abnormally and an attempt to reconnect was rejected");
                    }
                    else if ((CloseCode)e.CloseStatus == CloseCode.FAILED_TO_CONNECT_TO_BACKEND)
                    {
                        // Server probably not listening.
                        TraceLine("Failed to connect to backend");

                        throw new WebSocketStreamClosedByServerException(
                            e.CloseStatus,
                            "Failed to connect to backend");
                    }
                    else
                    {
                        TraceLine($"Connection closed abnormally: {(CloseCode)e.CloseStatus}");

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
            if (count > this.MaxWriteSize)
            {
                throw new IndexOutOfRangeException(
                    $"Write buffer too large ({count}), must be at most {this.MaxWriteSize}");
            }

            while (true)
            {
                try
                {
                    var connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);

                    // Take care of outstanding ACKs.
                    var bytesToAck = Thread.VolatileRead(ref this.bytesReceived);
                    if (this.lastAck < bytesToAck)
                    {
                        var ackMessage = new MessageBuffer(
                            new byte[AckMessage.ExpectedLength]).AsAckMessage();
                        ackMessage.Tag = MessageTag.ACK;
                        ackMessage.Ack = bytesToAck;

                        TraceLine($"Sending ACK #{ackMessage.Ack}...");

                        await connection.WriteAsync(
                            ackMessage.Buffer,
                            0,
                            (int)AckMessage.ExpectedLength,
                            cancellationToken).ConfigureAwait(false);

                        this.lastAck = ackMessage.Ack;
                    }

                    // Send data.

                    var newMessage = new DataMessage((uint)count)
                    {
                        Tag = MessageTag.DATA,
                        DataLength = (uint)count,
                        SequenceNumber = Thread.VolatileRead(ref this.bytesSent)
                    };
                    Array.Copy(buffer, offset, newMessage.Buffer, DataMessage.DataOffset, count);

                    TraceLine($"Sending DATA #{newMessage.SequenceNumber}...");

                    // Update bytesSent before we write the data to the wire,
                    // otherwise we might see an ACK before bytesSent even reflects
                    // that the data has been sent.
                    this.bytesSent = Thread.VolatileRead(ref this.bytesSent) + (ulong)count;

                    await connection.WriteAsync(
                        newMessage.Buffer,
                        0,
                        newMessage.BufferLength,
                        cancellationToken).ConfigureAwait(false);

                    // We should get an ACK for this message.
                    lock (this.sentButUnacknoledgedQueueLock)
                    {
                        this.sentButUnacknoledgedQueue.Enqueue(newMessage);
                    }

                    return;
                }
                catch (WebSocketStreamClosedByServerException e)
                {
                    if (e.CloseStatus == WebSocketCloseStatus.NormalClosure ||
                            (CloseCode)e.CloseStatus == CloseCode.DESTINATION_READ_FAILED ||
                            (CloseCode)e.CloseStatus == CloseCode.DESTINATION_WRITE_FAILED)
                    {
                        // The server closed the connection and us sending more data
                        // really seems unexpected.
                        throw;
                    }
                    else if ((CloseCode)e.CloseStatus == CloseCode.NOT_AUTHORIZED)
                    {
                        TraceLine("NOT_AUTHORIZED");

                        throw new UnauthorizedException(e.CloseStatusDescription);
                    }
                    else
                    {
                        TraceLine($"Connection closed abnormally: {(CloseCode)e.CloseStatus}");

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
    }

    [Serializable]
    public class SshRelayException : Exception
    {
        protected SshRelayException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public SshRelayException(string message) : base(message)
        {
        }
    }

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

    [Serializable]
    public class UnauthorizedException : SshRelayException
    {
        protected UnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
