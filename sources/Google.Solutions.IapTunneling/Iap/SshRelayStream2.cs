using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Threading;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Iap
{
    public class SshRelayStream2 : SingleReaderSingleWriterStream
    {
        private const uint MaxReconnects = 3;

        private readonly ISshRelayEndpoint endpoint;

        private readonly AsyncLock connectLock = new AsyncLock();
        private INetworkStream __currentConnection = null;
        private readonly Queue<UnacknoledgedWrite> unacknoledgedQueue = new Queue<UnacknoledgedWrite>();

        // Connection statistics, volatile.
        private ulong bytesSent = 0;
        private ulong bytesSentAndAcknoledged = 0;
        private ulong bytesReceived = 0;

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
            //
            // Check if we're connected. 
            //
            // This method might be called concurrently by a
            // writer and a reader, so we have to synchronize.
            //
            using (await this.connectLock
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                Debug.Assert((this.__currentConnection != null) == (this.Sid != null));

                if (this.__currentConnection != null)
                {
                    //
                    // We're still connected.
                    //
                }
                else if (this.bytesReceived == 0 && this.bytesSent == 0)
                {
                    //
                    // Connect.
                    //

                    var connection = await this.endpoint
                        .ConnectAsync(cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // Expect a CONNECT_SUCCESS_SID message.
                    //
                    var message = new byte[SshRelayFormat.MaxMessageSize];
                    string connectionSid = null;

                    while (connectionSid != null)
                    {
                        var bytesRead = await connection
                            .ReadAsync(
                                message,
                                0,
                                message.Length,
                                cancellationToken)
                            .ConfigureAwait(false);

                        SshRelayFormat.Tag.Decode(message, out var tag);

                        switch (tag)
                        {
                            case SshRelayMessageTag.CONNECT_SUCCESS_SID:
                                {
                                    var bytesDecoded = SshRelayFormat.ConnectSuccessSid.Decode(message, out var sid);
                                    connectionSid = sid;

                                    TraceLine($"Connected session <{this.Sid}>");
                                    Debug.Assert(bytesDecoded == bytesRead);

                                    break;
                                }

                            case SshRelayMessageTag.LONG_CLOSE:
                                // TODO: Throw
                            default:
                                // Ignore.

                                break;
                        }
                    }

                    //
                    // We're connected.
                    //
                    this.Sid = connectionSid;
                    this.__currentConnection = connection;

                }
                else
                {
                    //
                    // Reconnect + sync ack's + resend data.
                    //
                }

                return this.__currentConnection;
            }
        }

        private async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            using (await this.connectLock
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                //
                // Drop this connection.
                //
                if (this.__currentConnection != null)
                {
                    try
                    {
                        await this.__currentConnection
                            .CloseAsync(cancellationToken)
                            .ConfigureAwait(false);
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
                    this.Sid = null;
                }

                TraceLine("Disonnected.");
            }
        }

        private async Task<uint> TransactAsync(
            Func<INetworkStream, Task<uint>> ioAction,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    var connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);

                    Debug.Assert(connection != null);
                    return await ioAction(connection).ConfigureAwait(false);
                }
                catch (WebSocketStreamClosedByClientException)
                {
                    throw;
                }
                catch (WebSocketStreamClosedByServerException e)
                {
                    switch ((SshRelayCloseCode)e.CloseStatus)
                    {
                        case SshRelayCloseCode.NORMAL:
                            //
                            // Server closed the connection normally.
                            //
                            return 0;

                        // TODO: Translate special codes

                        default:
                            {
                                if (attempt++ >= MaxReconnects)
                                {
                                    throw;
                                }
                                else
                                {
                                    //
                                    // Try again.
                                    //
                                    await DisconnectAsync(cancellationToken).ConfigureAwait(true);
                                    break;
                                }
                            }
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public SshRelayStream2(ISshRelayEndpoint endpoint)
        {
            this.endpoint = endpoint;
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
            await TransactAsync(async connection => {
                // Read + dispatch:
                //  DATA
                //  ACK
                //  LONG_CLOSE

                await Task.Yield();
                return 1;
            }, cancellationToken);
        }

        protected async override Task ProtectedWriteAsync(
            byte[] buffer, 
            int offset, 
            int count, 
            CancellationToken cancellationToken)
        {
            await TransactAsync(async connection => {
                // write

                await Task.Yield();
                return 1;
            }, cancellationToken);
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

            this.connectLock.Dispose();
            this.__currentConnection?.Dispose();
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

    public class SshRelayProtocolViolationException : SshRelayException
    {
        public SshRelayProtocolViolationException(string message) : base(message)
        {
        }
    }
}
