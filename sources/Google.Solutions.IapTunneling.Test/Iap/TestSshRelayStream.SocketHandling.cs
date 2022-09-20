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

using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Test.Net;
using Google.Solutions.IapTunneling.Test.Util;
using Google.Solutions.Testing.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    public class TestSshRelayStreamSocketHandling : IapFixtureBase
    {
        private WebSocketServer server;

        [OneTimeSetUp]
        public void StartServer()
        {
            this.server = new WebSocketServer();
        }

        [OneTimeTearDown]
        public void StopServer()
        {
            this.server.Dispose();
        }

        private class Endpoint : ISshRelayEndpoint, IDisposable
        {
            private readonly WebSocketServer server;
            private readonly WebSocketConnection connection;

            private readonly Stack<WebSocketConnection> reconnectConnections
                = new Stack<WebSocketConnection>();

            public int ConnectCalls { get; private set; } = 0;
            public int ReconnectCalls { get; private set; } = 0;

            public ServerWebSocketConnection Server => this.connection.Server;

            public Endpoint(
                WebSocketServer server,
                WebSocketConnection connection)
            {
                this.server = server;
                this.connection = connection;
            }

            public void Dispose()
            {
                this.connection?.Dispose();
            }

            public Task<INetworkStream> ConnectAsync(CancellationToken token)
            {
                if (this.ConnectCalls++ == 0)
                {
                    return Task.FromResult<INetworkStream>(
                        new WebSocketStream(this.connection.Client));
                }
                else
                {
                    return Task.FromResult<INetworkStream>(
                        new WebSocketStream(
                            this.reconnectConnections.Pop().Client));
                }
            }

            public Task<INetworkStream> ReconnectAsync(
                string sid,
                ulong lastByteConsumedByClient,
                CancellationToken token)
            {
                this.ReconnectCalls++;
                return Task.FromResult<INetworkStream>(
                    new WebSocketStream(
                        this.reconnectConnections.Pop().Client));
            }

            public async Task<ServerWebSocketConnection> AfterReconnect()
            {
                var connection = await this.server
                    .ConnectAsync()
                    .ConfigureAwait(false);
                this.reconnectConnections.Push(connection);
                return connection.Server;
            }
        }

        private async Task<Endpoint> CreateEndpointAsync()
        {
            return new Endpoint(
                this.server,
                await this.server
                    .ConnectAsync()
                    .ConfigureAwait(false));
        }

        //---------------------------------------------------------------------
        // TestConnectionAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenReadFailsWithCloseCode_ThenTestConnectionThrowsException(
            [Values(
                SshRelayCloseCode.NOT_AUTHORIZED,
                SshRelayCloseCode.LOOKUP_FAILED,
                SshRelayCloseCode.LOOKUP_FAILED_RECONNECT)] SshRelayCloseCode code)
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .CloseOutputAsync((WebSocketCloseStatus)code)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    try
                    {
                        await clientStream
                            .TestConnectionAsync(TimeSpan.FromSeconds(1))
                            .ConfigureAwait(false);

                        Assert.Fail();
                    }
                    catch (UnauthorizedException e)
                    {
                        Assert.AreEqual(((int)code).ToString(), e.Message);
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: closing.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenBufferSizeTooSmall_ThenReadThrowsException()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    ExceptionAssert.ThrowsAggregateException<IndexOutOfRangeException>(
                        () => clientStream
                            .ReadAsync(buffer, 1, buffer.Length - 1, CancellationToken.None)
                            .Wait());
                }

                Assert.AreEqual(0, endpoint.ReconnectCalls);
            }
        }

        [Test]
        public async Task WhenServerClosesConnectionNormally_ThenReadReturnsZeroAndDoesNotReconnect(
             [Values(
                SshRelayCloseCode.NORMAL,
                SshRelayCloseCode.DESTINATION_READ_FAILED,
                SshRelayCloseCode.DESTINATION_WRITE_FAILED)] SshRelayCloseCode code)
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .CloseOutputAsync((WebSocketCloseStatus)code)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    await clientStream
                        .CloseAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(0, bytesRead);
                    Assert.AreEqual(0, endpoint.ReconnectCalls);
                }
            }
        }

        [Test]
        public async Task WhenServerClosesConnectionWithNotAuthorizedError_ThenReadReturnsZeroAndDoesNotReconnect(
             [Values(
                SshRelayCloseCode.NOT_AUTHORIZED)] SshRelayCloseCode code)
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .CloseOutputAsync((WebSocketCloseStatus)code)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    ExceptionAssert.ThrowsAggregateException<UnauthorizedException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                    Assert.AreEqual(0, endpoint.ReconnectCalls);
                }
            }
        }

        [Test]
        public async Task WhenServerClosesConnectionWithProtocolError_ThenReadReturnsZeroAndDoesNotReconnect(
             [Values(
                SshRelayCloseCode.SID_UNKNOWN,
                SshRelayCloseCode.SID_IN_USE,
                SshRelayCloseCode.FAILED_TO_CONNECT_TO_BACKEND)] SshRelayCloseCode code)
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .CloseOutputAsync((WebSocketCloseStatus)code)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    ExceptionAssert.ThrowsAggregateException<WebSocketStreamClosedByServerException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                    Assert.AreEqual(0, endpoint.ReconnectCalls);
                }
            }
        }

        [Test]
        public async Task WhenConnectionClosedByClient_ThenReadThrowsException()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                using (var clientStream = new SshRelayStream(endpoint))
                {
                    await clientStream
                        .CloseAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    var buffer = new byte[SshRelayStream.MinReadSize];

                    ExceptionAssert.ThrowsAggregateException<NetworkStreamClosedException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: unknown tags/messages.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenServerTruncatedMessage_ThenReadThrowsException()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendBinaryFrameAsync(new byte[] { 1 })
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    ExceptionAssert.ThrowsAggregateException<InvalidServerResponseException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task WhenServerSendsUnknownTagBeforeSid_ThenReadThrowsException()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendBinaryFrameAsync(new byte[] { 0xAA, 0xBB })
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    ExceptionAssert.ThrowsAggregateException<InvalidServerResponseException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task WhenServerSendsUnknownTagAfterSid_ThenReadSucceeds()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendConnectSuccessSidAsync("Sid")
                    .ConfigureAwait(false);

                await endpoint.Server
                    .SendBinaryFrameAsync(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }) // Junk.
                    .ConfigureAwait(false);

                var data = new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
                await endpoint.Server
                    .SendDataAsync(data)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(4, bytesRead);
                    Assert.AreEqual((byte)'d', buffer[0]);
                    Assert.AreEqual((byte)'a', buffer[1]);
                    Assert.AreEqual((byte)'t', buffer[2]);
                    Assert.AreEqual((byte)'a', buffer[3]);
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: buffer.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenBufferSizeSufficient_ThenReadSucceeds()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendConnectSuccessSidAsync("Sid")
                    .ConfigureAwait(false);

                var data = new byte[SshRelayStream.MinReadSize];
                data[0] = 0xAA;
                data[data.Length - 1] = 0xBB;

                await endpoint.Server
                    .SendDataAsync(data)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize + 1];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 1, buffer.Length - 1, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(SshRelayStream.MinReadSize, bytesRead);
                    Assert.AreEqual(0xAA, buffer[1]);
                    Assert.AreEqual(0xBB, buffer[buffer.Length - 1]);
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: reconnect.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenServerClosesConnectionWithUnknownErrorBeforeData_ThenReadConnectsAgain(
             [Values(
                SshRelayCloseCode.INVALID_WEBSOCKET_OPCODE)] SshRelayCloseCode code)
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false); 
                await endpoint.Server
                     .CloseOutputAsync((WebSocketCloseStatus)code)
                     .ConfigureAwait(false);

                var afterReconnect = await endpoint
                    .AfterReconnect()
                    .ConfigureAwait(false);
                await afterReconnect
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false);
                await afterReconnect
                    .SendDataAsync(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' })
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(4, bytesRead);
                    Assert.AreEqual(2, endpoint.ConnectCalls);
                    Assert.AreEqual(0, endpoint.ReconnectCalls);
                }
            }
        }

        [Test]
        public async Task WhenServerClosesConnectionWithUnknownErrorAfterData_ThenReadTriggersReconnect(
             [Values(
                SshRelayCloseCode.INVALID_WEBSOCKET_OPCODE)] SshRelayCloseCode code)
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false);
                await endpoint.Server
                    .SendDataAsync(new byte[] { 0xAA })
                    .ConfigureAwait(false);
                await endpoint.Server
                     .CloseOutputAsync((WebSocketCloseStatus)code)
                     .ConfigureAwait(false);

                var afterReconnect = await endpoint
                    .AfterReconnect()
                    .ConfigureAwait(false);
                await afterReconnect
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false);
                await afterReconnect
                    .SendDataAsync(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' })
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    // Read data before reconnect.
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(1, bytesRead);
                    Assert.AreEqual(1, endpoint.ConnectCalls);
                    Assert.AreEqual(0, endpoint.ReconnectCalls);

                    // Read data after reconnect.
                    bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(4, bytesRead);
                    Assert.AreEqual(1, endpoint.ConnectCalls);
                    Assert.AreEqual(1, endpoint.ReconnectCalls);
                }
            }
        }

        //---------------------------------------------------------------------
        // Write: closing.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenBufferSizeTooBig_ThenWriteThrowsException()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayStream.MaxWriteSize + 1];
                    ExceptionAssert.ThrowsAggregateException<IndexOutOfRangeException>(
                        () => clientStream
                            .WriteAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                }

                Assert.AreEqual(0, endpoint.ReconnectCalls);
            }
        }

        //---------------------------------------------------------------------
        // Write: ack.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDataRead_ThenWriteSendsAck()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false);
                await endpoint.Server
                    .SendDataAsync(new byte[] { 0xAA })
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    // Read data (so that we owe an ACK).
                    var buffer = new byte[SshRelayStream.MaxWriteSize + 1];
                    await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Write 2 chunks data (to send the ACK).
                    var data = new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
                    await clientStream
                        .WriteAsync(data, 0, data.Length, CancellationToken.None)
                        .ConfigureAwait(false);
                    await clientStream
                        .WriteAsync(data, 0, data.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Expect ACK.
                    var serverBuffer = new byte[64];
                    var bytesReceived = await endpoint.Server
                        .ReceiveBinaryFrameAsync(serverBuffer)
                        .ConfigureAwait(false);

                    Assert.AreEqual(10, bytesReceived);
                    SshRelayFormat.Ack.Decode(serverBuffer, out var ack);
                    Assert.AreEqual(1, ack);

                    // Expect DATA.
                    bytesReceived = await endpoint.Server
                        .ReceiveBinaryFrameAsync(serverBuffer)
                        .ConfigureAwait(false);

                    Assert.AreEqual(10, bytesReceived);
                    SshRelayFormat.Tag.Decode(serverBuffer, out var tag);
                    Assert.AreEqual(SshRelayMessageTag.DATA, tag);

                    // Expect DATA.
                    bytesReceived = await endpoint.Server
                        .ReceiveBinaryFrameAsync(serverBuffer)
                        .ConfigureAwait(false);

                    Assert.AreEqual(10, bytesReceived);
                    SshRelayFormat.Tag.Decode(serverBuffer, out tag);
                    Assert.AreEqual(SshRelayMessageTag.DATA, tag);
                }
            }
        }

        //---------------------------------------------------------------------
        // Write: reconnect.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenReconnecting_ThenWriteResendsUnackedData()
        {
            using (var endpoint = await CreateEndpointAsync().ConfigureAwait(false))
            {
                await endpoint.Server
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false);
                await endpoint.Server
                    .SendDataAsync(new byte[] { 0xAA })
                    .ConfigureAwait(false);

                var afterReconnect = await endpoint
                    .AfterReconnect()
                    .ConfigureAwait(false);
                await afterReconnect
                    .SendConnectSuccessSidAsync("sid")
                    .ConfigureAwait(false);
                await afterReconnect
                    .SendDataAsync(new byte[] { 0xBB })
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    // Read data.
                    var buffer = new byte[SshRelayStream.MinReadSize];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);
                    Assert.AreEqual(1, bytesRead);

                    // Send data.
                    var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
                    await clientStream
                        .WriteAsync(data, 0, data.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Reconnect.
                    await endpoint.Server
                        .CloseOutputAsync((WebSocketCloseStatus)SshRelayCloseCode.INVALID_WEBSOCKET_OPCODE)
                        .ConfigureAwait(false);
                    await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);
                    Assert.AreEqual(1, bytesRead);

                    // Send more data.
                    data = new byte[] { 0xEE, 0xFF };
                    await clientStream
                        .WriteAsync(data, 0, data.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Expect DATA.
                    var serverBuffer = new byte[64];
                    var bytesReceived = await afterReconnect
                        .ReceiveBinaryFrameAsync(serverBuffer)
                        .ConfigureAwait(false);

                    Assert.AreEqual(10, bytesReceived);
                    SshRelayFormat.Tag.Decode(serverBuffer, out var tag);
                    Assert.AreEqual(SshRelayMessageTag.DATA, tag);

                    // Expect ACK.
                    bytesReceived = await afterReconnect
                        .ReceiveBinaryFrameAsync(serverBuffer)
                        .ConfigureAwait(false);

                    Assert.AreEqual(10, bytesReceived);
                    SshRelayFormat.Ack.Decode(serverBuffer, out var ack);
                    Assert.AreEqual(2, ack);

                    // Expect DATA.
                    bytesReceived = await afterReconnect
                        .ReceiveBinaryFrameAsync(serverBuffer)
                        .ConfigureAwait(false);

                    Assert.AreEqual(8, bytesReceived);
                    SshRelayFormat.Tag.Decode(serverBuffer, out tag);
                    Assert.AreEqual(SshRelayMessageTag.DATA, tag);
                }
            }
        }
    }
}
