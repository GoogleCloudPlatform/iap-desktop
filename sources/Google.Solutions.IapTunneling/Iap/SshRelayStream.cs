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
    /// NetworkStream for reading/writing from a SshRelayChannel.
    /// </summary>
    public class SshRelayStream : SingleReaderSingleWriterStream
    {
        private readonly SshRelayChannel channel;

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
                    this.channel,
                    message);
            }
        }

        private async Task ResendUnacknoledgedData(
            INetworkStream stream,
            CancellationToken cancellationToken)
        {
            using (await this.unacknoledgedQueueLock
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                while (this.unacknoledgedQueue.Any())
                {
                    var write = this.unacknoledgedQueue.Dequeue();
                    if (write.ExpectedAck > this.channel.LastAckReceived)
                    {
                        //
                        // We never got an ACK for this one, resend.
                        //

                        TraceLine($"Resending DATA #{write.SequenceNumber}...");
                        await stream
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
            this.channel = new SshRelayChannel(endpoint);
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

                    using (var stream = await this.channel.Endpoint
                        .ConnectAsync(cts.Token)
                        .ConfigureAwait(false))
                    {
                        await stream
                            .ReadAsync(Array.Empty<byte>(), 0, 0, cts.Token)
                            .ConfigureAwait(false);
                        await stream
                            .CloseAsync(cts.Token)
                            .ConfigureAwait(false);
                    }

                    TraceLine("Connection test succeeded");
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
                TraceLine($"Connection test failed: {e.CloseStatusDescription} ({e.CloseStatus})");
                throw new SshRelayDeniedException(e.CloseStatusDescription);
            }
            catch (OperationCanceledException)
            {
                throw new NetworkStreamClosedException("Connection timed out");
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public string Sid => this.channel.Sid;

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

            return (int)await this.channel.IoAsync(
                async stream => {
                    while (true)
                    {
                        var bytesRead = await stream
                            .ReadAsync(
                                message,
                                0,
                                message.Length,
                                cancellationToken)
                            .ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
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

                                    TraceLine($"Received DATA message ({dataLength} bytes)");

                                    Interlocked.Add(ref this.bytesReceived, dataLength);

                                    return dataLength;
                                }
                            case SshRelayMessageTag.ACK:
                                {
                                    var bytesDecoded = SshRelayFormat.Ack.Decode(message, out var ack);

                                    Debug.Assert(ack > 0);
                                    Debug.Assert(bytesDecoded == bytesRead);

                                    this.channel.LastAckReceived = ack;

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
                                TraceLine($"Received unknown message: {tag}");

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

            await this.channel.IoAsync(
                async stream => {

                    //
                    // Take care of outstanding ACKs.
                    //
                    var bytesToAck = (ulong)Thread.VolatileRead(ref this.bytesReceived);
                    if (this.channel.LastAckSent < bytesToAck)
                    {
                        var ackBuffer = new byte[SshRelayFormat.Ack.MessageLength];
                        SshRelayFormat.Ack.Encode(ackBuffer, bytesToAck);

                        TraceLine($"Sending ACK #{bytesToAck}...");

                        await stream
                            .WriteAsync(
                                ackBuffer,
                                0,
                                ackBuffer.Length,
                                cancellationToken)
                            .ConfigureAwait(false);

                        this.channel.LastAckSent = bytesToAck;
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

                    await stream
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
            await this.channel
                .DisconnectAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public override string ToString()
        {
            return this.channel.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.channel.Dispose();
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

    public class SshRelayDeniedException : SshRelayException
    {
        public SshRelayDeniedException(string message) : base(message)
        {
        }
    }
}
