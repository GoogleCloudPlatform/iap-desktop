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
using Google.Solutions.IapTunneling.Net;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
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
    /// SSH Relay session. A session uses one WebSocket connection
    /// at a time. If that connection breaks, the session attempts
    /// to reconnect.
    /// </summary>
    internal sealed class SshRelaySession
    {
        private const uint MaxReconnects = 2;

        public ISshRelayEndpoint Endpoint { get; }

        //
        // Current connection, guarded by the a lock.
        //
        private INetworkStream connection = null;
        private readonly AsyncLock connectLock = new AsyncLock();

        /// <summary>
        /// Unique identifier of session. Available after initial
        /// connection has been established.
        /// </summary>
        public string Sid { get; private set; }

        /// <summary>
        /// Last ACK received, only to be accessed from writer thread.
        /// </summary>
        public ulong LastAckReceived { get; set; } = 0;

        /// <summary>
        /// Last ACK sent, only to be accessed from reader thread.
        /// </summary>
        public ulong LastAckSent { get; set; } = 0;

        private void TraceVerbose(string message)
        {
            if (IapTraceSources.Default.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                IapTraceSources.Default.TraceVerbose(
                    "SshRelayStream [{0}] AR:{1} AS:{2}: {3}",
                    this.Sid,
                    this.LastAckReceived,
                    this.LastAckSent,
                    message);
            }
        }

        private async Task<INetworkStream> GetConnectionAsync(
            Func<INetworkStream, CancellationToken, Task> resendUnacknoledgedDataAction,
            CancellationToken cancellationToken)
        {
            //
            // This method might be called concurrently by a
            // writer and a reader, so we have to synchronize.
            //
            using (await this.connectLock
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                if (this.connection != null)
                {
                    //
                    // We're still connected.
                    //
                    return this.connection;
                }
                else if (this.LastAckReceived == 0)
                {
                    //
                    // Initial connect.
                    //
                    TraceVerbose($"Establishing new connection");

                    var connection = await this.Endpoint
                        .ConnectAsync(cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // To complete connection establishment, we have to receive
                    // a CONNECT_SUCCESS_SID message.
                    //
                    var message = new byte[SshRelayFormat.MaxMessageSize];
                    string connectionSid = null;

                    while (true)
                    {
                        var bytesRead = await connection
                            .ReadAsync(
                                message,
                                0,
                                message.Length,
                                cancellationToken)
                            .ConfigureAwait(false);

                        SshRelayFormat.Tag.Decode(message, out var tag);

                        if (bytesRead == 0)
                        {
                            throw new WebSocketStreamClosedByServerException(
                                WebSocketCloseStatus.NormalClosure,
                                "The connection was closed by the server");
                        }
                        else if (bytesRead < SshRelayFormat.Tag.Length)
                        {
                            throw new SshRelayProtocolViolationException(
                                "The server sent an incomplete message");
                        }

                        switch (tag)
                        {
                            case SshRelayMessageTag.CONNECT_SUCCESS_SID:
                                {
                                    var bytesDecoded = SshRelayFormat.ConnectSuccessSid.Decode(
                                        message,
                                        out var sid);
                                    connectionSid = sid;

                                    Debug.Assert(bytesDecoded == bytesRead);

                                    //
                                    // If the previous connection broke before we received
                                    // the first ACK, then there might be data to be resend.
                                    //

                                    await resendUnacknoledgedDataAction(
                                            connection,
                                            cancellationToken)
                                        .ConfigureAwait(false);

                                    this.Sid = connectionSid;
                                    this.connection = connection;

                                    TraceVerbose($"Received CONNECT_SUCCESS_SID, connected");

                                    return connection;
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
                }
                else
                {
                    //
                    // Reconnect + sync ack's + resend data.
                    //
                    Debug.Assert(this.Sid != null);
                    TraceVerbose($"Attempting reconnect with ack={this.LastAckReceived}");

                    var connection = await this.Endpoint
                        .ReconnectAsync(
                            this.Sid,
                            this.LastAckReceived,
                            cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // To complete connection establishment, we have to receive
                    // a RECONNECT_SUCCESS_ACK message.
                    //
                    var message = new byte[SshRelayFormat.MaxMessageSize];
                    while (true)
                    {
                        var bytesRead = await connection
                            .ReadAsync(
                                message,
                                0,
                                message.Length,
                                cancellationToken)
                            .ConfigureAwait(false);

                        SshRelayFormat.Tag.Decode(message, out var tag);

                        if (bytesRead == 0)
                        {
                            throw new WebSocketStreamClosedByServerException(
                                WebSocketCloseStatus.NormalClosure,
                                "The connection was closed by the server");
                        }
                        else if (bytesRead < SshRelayFormat.Tag.Length)
                        {
                            throw new SshRelayProtocolViolationException(
                                "The server sent an incomplete message");
                        }

                        switch (tag)
                        {
                            case SshRelayMessageTag.RECONNECT_SUCCESS_ACK:
                                {
                                    var bytesDecoded = SshRelayFormat.ReconnectAck.Decode(
                                        message,
                                        out var ack);
                                    this.LastAckReceived = ack;

                                    Debug.Assert(bytesDecoded == bytesRead);

                                    //
                                    // Resend all data since the ACK that we just received.
                                    //

                                    await resendUnacknoledgedDataAction(
                                            connection,
                                            cancellationToken)
                                        .ConfigureAwait(false);

                                    this.connection = connection;

                                    TraceVerbose("Received RECONNECT_SUCCESS_ACK, reconnected");

                                    return connection;
                                }
                            case SshRelayMessageTag.CONNECT_SUCCESS_SID:
                                {
                                    //
                                    // We shouldn't be receiving this message after
                                    // a reconnect.
                                    //
                                    throw new SshRelayProtocolViolationException(
                                        "The server sent an unexpected CONNECT_SUCCESS_SID " +
                                        "message in response to a reconect");
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
                }
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public SshRelaySession(ISshRelayEndpoint endpoint)
        {
            this.Endpoint = endpoint;
        }

        internal async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            using (await this.connectLock
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                //
                // Drop this connection.
                //
                if (this.connection != null)
                {
                    try
                    {
                        await this.connection
                            .CloseAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        IapTraceSources.Default.TraceError(e);
                    }

                    try
                    {
                        this.connection.Dispose();
                    }
                    catch (Exception e)
                    {
                        IapTraceSources.Default.TraceError(e);
                    }

                    this.connection = null;
                }

                TraceVerbose("Disonnected");
            }
        }

        public async Task<uint> IoAsync(
            Func<INetworkStream, Task<uint>> ioAction,
            Func<INetworkStream, CancellationToken, Task> resendUnacknoledgedDataAction,
            bool treatNormalCloseAsError,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    var connection = await GetConnectionAsync(
                            resendUnacknoledgedDataAction,
                            cancellationToken)
                        .ConfigureAwait(false);

                    Debug.Assert(connection != null);
                    return await ioAction(connection).ConfigureAwait(false);
                }
                catch (WebSocketStreamClosedByClientException)
                {
                    throw;
                }
                catch (WebSocketStreamClosedByServerException e)
                {
                    IapTraceSources.Default.TraceError(e);

                    switch ((SshRelayCloseCode)e.CloseStatus)
                    {
                        case SshRelayCloseCode.NORMAL:
                        case SshRelayCloseCode.DESTINATION_READ_FAILED:
                        case SshRelayCloseCode.DESTINATION_WRITE_FAILED:
                            //
                            // NB. We get a DESTINATION_*_FAILED if the
                            // backend closed the connection (as opposed
                            // to the relay).
                            //
                            if (treatNormalCloseAsError)
                            {
                                throw;
                            }
                            else
                            {
                                //
                                // Server closed the connection normally.
                                //
                                return 0;
                            }

                        case SshRelayCloseCode.NOT_AUTHORIZED:
                            throw new SshRelayDeniedException(
                                $"The server denied access: " +
                                e.CloseStatusDescription);

                        case SshRelayCloseCode.FAILED_TO_REWIND:
                        case SshRelayCloseCode.SID_UNKNOWN:
                        case SshRelayCloseCode.SID_IN_USE:
                            throw new SshRelayReconnectException(
                                "The server closed the connection unexpectedly and " +
                                "reestablishing the connection failed: " +
                                e.CloseStatusDescription);

                        case SshRelayCloseCode.FAILED_TO_CONNECT_TO_BACKEND:
                            throw new SshRelayConnectException(
                                "The server could not connect to the backend: " +
                                e.CloseStatusDescription);

                        default:
                            {
                                if (attempt++ >= MaxReconnects)
                                {
                                    TraceVerbose($"Failed to reconnect after {attempt} attempts");
                                    throw;
                                }
                                else
                                {
                                    //
                                    // Try again.
                                    //
                                    await DisconnectAsync(cancellationToken)
                                        .ConfigureAwait(true);

                                    TraceVerbose("Attempting to reconnect");

                                    break;
                                }
                            }
                    }
                }
            }
        }

        public void Dispose()
        {
            this.connectLock.Dispose();
            this.connection?.Dispose();
        }

        public override string ToString()
        {
            var sidToken = this.Sid != null 
                ? this.Sid.Substring(0, Math.Min(this.Sid.Length, 10))
                : "(unknown)";
            return $"[SshRelaySession {sidToken}]";
        }
    }
}
