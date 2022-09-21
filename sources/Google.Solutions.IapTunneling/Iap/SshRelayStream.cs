using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Threading;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly SshRelayConnectionManager connectionManager;

        // Queue of un-ack'ed messages that might require re-sending.
        private readonly AsyncLock unacknoledgedQueueLock = new AsyncLock();
        private readonly Queue<UnacknoledgedWrite> unacknoledgedQueue = new Queue<UnacknoledgedWrite>();

        private long bytesReceived = 0;
        private long bytesSent = 0;

        //---------------------------------------------------------------------
        // Privates
        //---------------------------------------------------------------------

        private void TraceLine(string message)
        {
            if (IapTraceSources.Default.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                IapTraceSources.Default.TraceVerbose(
                    "{0} - {1}",
                    this.connectionManager,
                    message);
            }
        }

        private async Task ResendUnacknoledgedData(
            INetworkStream connection,
            CancellationToken cancellationToken)
        {
            using (await this.unacknoledgedQueueLock
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                while (this.unacknoledgedQueue.Any())
                {
                    var write = this.unacknoledgedQueue.Dequeue();
                    if (write.ExpectedAck > this.connectionManager.LastAckReceived)
                    {
                        //
                        // We never got an ACK for this one, resend.
                        //

                        TraceLine($"Resending DATA #{write.SequenceNumber}...");
                        await connection
                            .WriteAsync(
                                write.Data,
                                0,
                                write.Data.Length,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        //
                        // This was ACKed, so don't resend.
                        //
                    }
                }
            }
        }


        //---------------------------------------------------------------------
        // Internal - for testing only.
        //---------------------------------------------------------------------

        internal ulong ExpectedAck
        {
            get
            {
                using (this.unacknoledgedQueueLock.Acquire())
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
                using (this.unacknoledgedQueueLock.Acquire())
                {
                    return this.unacknoledgedQueue.Count;
                }
            }
        }

        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

        public SshRelayStream(ISshRelayEndpoint endpoint)
        {
            this.connectionManager = new SshRelayConnectionManager(endpoint);
        }

        /// <summary>
        /// Maximum amount of data (in byte) that can be written at once.
        /// </summary>
        public const int MaxWriteSize = (int)SshRelayFormat.Data.MaxPayloadLength;

        /// <summary>
        /// Minimum amount of data (in byte) that can be read at once.
        /// </summary>
        public const int MinReadSize = (int)SshRelayFormat.Data.MaxPayloadLength;

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

                    using (var connection = await this.connectionManager.Endpoint
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
                throw new UnauthorizedException(e.CloseStatusDescription);
            }
            catch (OperationCanceledException)
            {
                throw new NetworkStreamClosedException("Connection timed out");
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public string Sid { get; private set; }

        protected async override Task<int> ProtectedReadAsync(
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

            return (int)await this.connectionManager.IoAsync(
                async connection => {
                    while (true)
                    {
                        var bytesRead = await connection
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
                        else if (bytesRead < SshRelayFormat.Tag.Length)
                        {
                            throw new InvalidServerResponseException(
                                "The server sent an incomplete message");
                        }

                        SshRelayFormat.Tag.Decode(message, out var tag);

                        switch (tag)
                        {
                            case SshRelayMessageTag.DATA:
                                {
                                    var bytesDecoded = SshRelayFormat.Data.Decode(
                                        message,
                                        buffer,
                                        (uint)offset,
                                        (uint)count,
                                        out var dataLength);

                                    Debug.Assert(dataLength < bytesDecoded);
                                    Debug.Assert(bytesDecoded == bytesRead);

                                    TraceLine($"Received data ({dataLength} bytes)");

                                    Interlocked.Add(ref this.bytesReceived, dataLength);

                                    return dataLength;
                                }
                            case SshRelayMessageTag.ACK:
                                {
                                    var bytesDecoded = SshRelayFormat.Ack.Decode(message, out var ack);

                                    Debug.Assert(ack > 0);
                                    Debug.Assert(bytesDecoded == bytesRead);

                                    this.connectionManager.LastAckReceived = ack;

                                    using (await this.unacknoledgedQueueLock
                                        .AcquireAsync(cancellationToken)
                                        .ConfigureAwait(false))
                                    {
                                        //
                                        // The server might be acknolodging multiple messages at once.
                                        //
                                        while (this.unacknoledgedQueue.Count > 0 &&
                                               this.unacknoledgedQueue.Peek().ExpectedAck <= ack)
                                        {
                                            this.unacknoledgedQueue.Dequeue();
                                        }
                                    }

                                    TraceLine($"Received ACK #{ack}");

                                    break;
                                }
                            case SshRelayMessageTag.LONG_CLOSE:
                            default:
                                //
                                // Unknown tag, ignore.
                                //
                                TraceLine($"Encountered unknown during read: {tag}");

                                break;
                        }
                    }
                },
                ResendUnacknoledgedData,
                cancellationToken);
        }

        protected async override Task ProtectedWriteAsync(
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

            await this.connectionManager.IoAsync(
                async connection => {

                    //
                    // Take care of outstanding ACKs.
                    //
                    var bytesToAck = (ulong)Thread.VolatileRead(ref this.bytesReceived);
                    if (this.connectionManager.LastAckSent < bytesToAck)
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

                        this.connectionManager.LastAckSent = bytesToAck;
                    }


                    //
                    // Send data.
                    //
                    var sequenceNumber = (ulong)Thread.VolatileRead(ref this.bytesSent);

                    var message = new byte[SshRelayFormat.Data.HeaderLength + count];
                    SshRelayFormat.Data.Encode(message, buffer, (uint)offset, (uint)count);

                    TraceLine($"Sending DATA #{sequenceNumber}...");

                    //
                    // Update bytesSent before we write the data to the wire,
                    // otherwise we might see an ACK before bytesSent even reflects
                    // that the data has been sent.
                    //
                    Interlocked.Add(ref this.bytesSent, count);

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
                    using (await this.unacknoledgedQueueLock
                        .AcquireAsync(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        this.unacknoledgedQueue.Enqueue(new UnacknoledgedWrite(
                            message,
                            sequenceNumber,
                            sequenceNumber + (ulong)count));
                    }

                    return 0;
                },
                ResendUnacknoledgedData,
                cancellationToken);
        }

        public override async Task ProtectedCloseAsync(CancellationToken cancellationToken)
        {
            await this.connectionManager
                .DisconnectAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public override string ToString()
        {
            return this.connectionManager.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.connectionManager.Dispose();
        }

        //---------------------------------------------------------------------
        // Helper structs.
        //---------------------------------------------------------------------


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

    public class SshRelayException : Exception
    {
        public SshRelayException(string message) : base(message)
        {
        }
    }

    public class SshRelayProtocolViolationException : SshRelayException
    {
        public SshRelayProtocolViolationException(string message) : base(message)
        {
        }
    }

    public class UnauthorizedException : SshRelayException
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
