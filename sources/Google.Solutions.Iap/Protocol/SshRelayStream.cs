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
using Google.Solutions.Iap.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Protocol
{
    /// <summary>
    /// NetworkStream for reading/writing from a SshRelaySession.
    /// </summary>
    internal class SshRelayStream : SingleReaderSingleWriterStream
    {
        private readonly SshRelaySession session;

        //
        // Queue of un-ack'ed messages that might require re-sending.
        //
        private readonly AsyncLock unacknoledgedQueueLock = new AsyncLock();
        private readonly Queue<UnacknoledgedWrite> unacknoledgedQueue = new Queue<UnacknoledgedWrite>();

        //---------------------------------------------------------------------
        // Privates
        //---------------------------------------------------------------------

        private void TraceVerbose(string message)
        {
            if (IapTraceSource.Log.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                IapTraceSource.Log.TraceVerbose($"{this.session}: {message}");
            }
        }

        private async Task ResendUnacknoledgedDataAsync(
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
                    if (write.ExpectedAck > this.session.State.LastAckReceived)
                    {
                        //
                        // We never got an ACK for this one, resend.
                        //

                        TraceVerbose($"Resending DATA #{write.SequenceNumber}...");
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

        public SshRelayStream(ISshRelayTarget endpoint)
        {
            this.session = new SshRelaySession(endpoint);
        }

        /// <summary>
        /// Maximum amount of data (in byte) that can be written at once.
        /// </summary>
        public const int MaxWriteSize = (int)SshRelayFormat.Data.MaxPayloadLength;

        /// <summary>
        /// Minimum amount of data (in byte) that can be read at once.
        /// </summary>
        public const int MinReadSize = (int)SshRelayFormat.Data.MaxPayloadLength;

        public async Task ProbeConnectionAsync(TimeSpan timeout)
        {
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

                    await this.session.IoAsync(
                        stream =>
                        {
                            //
                            // If we get here, then we've successfully established
                            // a connection.
                            //
                            return Task.FromResult(0u);
                        },
                        (s, t) => Task.CompletedTask,
                        true,
                        cts.Token);

                    await CloseAsync(cts.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw new NetworkStreamClosedException(
                    "The server did not respond within the allotted time");
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public string? Sid => this.session.Sid;

        protected override async Task<int> ReadCoreAsync(
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

            return (int)await this.session.IoAsync(
                async stream =>
                {
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
                            throw new SshRelayProtocolViolationException(
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

                                    TraceVerbose($"Received DATA message ({dataLength} bytes)");

                                    this.session.State.AddBytesReceived(dataLength);

                                    if (this.session.State.BytesReceived - this.session.State.LastAckSent > 
                                        SshRelaySession.MaxReadDataPerAck)
                                    {
                                        //
                                        // We've read enough data that we really
                                        // need to send an ACK, otherwise the server
                                        // might stall the connection.
                                        //
                                        // If we're tunneling a chatty protocol,
                                        // the ACKs are automatically taken care
                                        // of by the send-side, but we can't rely
                                        // on that.
                                        //
                                        // Instead of sending the ACK here, initiate 
                                        // a zero-byte write. That way, the write is 
                                        // done under the right lock and synchronized
                                        // with other write operations that could 
                                        // start any time.
                                        //
                                        await
                                            WriteAsync(Array.Empty<byte>(), 0, 0, cancellationToken)
                                            .ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        //
                                        // Don't send an ACK yet.
                                        //
                                    }

                                    return dataLength;
                                }
                            case SshRelayMessageTag.ACK:
                                {
                                    var bytesDecoded = SshRelayFormat.Ack.Decode(message, out var ack);

                                    Debug.Assert(bytesDecoded == bytesRead);

                                    if (ack == 0)
                                    {
                                        throw new SshRelayProtocolViolationException(
                                            "The server sent an invalid zero-ack");
                                    }
                                    else if (ack > (ulong)this.session.State.BytesSent)
                                    {
                                        throw new SshRelayProtocolViolationException(
                                            "The server sent a mismatched ack");
                                    }

                                    this.session.State.LastAckReceived = ack;

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

                                    TraceVerbose($"Received ACK #{ack}");

                                    break;
                                }
                            case SshRelayMessageTag.LONG_CLOSE:
                            default:
                                //
                                // Unknown tag, ignore.
                                //
                                TraceVerbose($"Received unknown message: {tag}");

                                break;
                        }
                    }
                },
                ResendUnacknoledgedDataAsync,
                false, // Normal closes are ok.
                cancellationToken);
        }

        protected override async Task WriteCoreAsync(
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

            await this.session.IoAsync(
                async stream =>
                {

                    //
                    // Take care of outstanding ACKs.
                    //
                    var bytesToAck = this.session.State.BytesReceived;
                    if (this.session.State.LastAckSent < bytesToAck)
                    {
                        var ackBuffer = new byte[SshRelayFormat.Ack.MessageLength];
                        SshRelayFormat.Ack.Encode(ackBuffer, bytesToAck);

                        TraceVerbose($"Sending ACK #{bytesToAck}...");

                        await stream
                            .WriteAsync(
                                ackBuffer,
                                0,
                                ackBuffer.Length,
                                cancellationToken)
                            .ConfigureAwait(false);

                        this.session.State.LastAckSent = bytesToAck;
                    }

                    if (count == 0)
                    {
                        //
                        // No data to send.
                        //
                        return 0;
                    }

                    //
                    // Send data.
                    //
                    var sequenceNumber = this.session.State.BytesSent;

                    var message = new byte[SshRelayFormat.Data.HeaderLength + count];
                    SshRelayFormat.Data.Encode(message, buffer, (uint)offset, (uint)count);

                    TraceVerbose($"Sending DATA #{sequenceNumber}...");

                    //
                    // Update bytesSent before we write the data to the wire,
                    // otherwise we might see an ACK before bytesSent even reflects
                    // that the data has been sent.
                    //
                    this.session.State.AddBytesSent((uint)count);

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
                ResendUnacknoledgedDataAsync,
                true, // Normal closes are unexpected.
                cancellationToken);
        }

        public override async Task CloseCoreAsync(CancellationToken cancellationToken)
        {
            await this.session
                .DisconnectAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public override string ToString()
        {
            return this.session.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.session.Dispose();
            this.unacknoledgedQueueLock.Dispose();
        }

        //---------------------------------------------------------------------
        // Helper structs.
        //---------------------------------------------------------------------

        private readonly struct UnacknoledgedWrite
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

    public abstract class SshRelayException : NetworkStreamClosedException
    {
        public SshRelayException(string message) : base(message)
        {
        }
    }

    public class SshRelayConnectException : SshRelayException
    {
        public SshRelayConnectException(string message) : base(message)
        {
        }
    }

    public class SshRelayReconnectException : SshRelayException
    {
        public SshRelayReconnectException(string message) : base(message)
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

    public class SshRelayBackendNotFoundException : SshRelayException
    {
        public SshRelayBackendNotFoundException(string message) : base(message)
        {
        }
    }
}
