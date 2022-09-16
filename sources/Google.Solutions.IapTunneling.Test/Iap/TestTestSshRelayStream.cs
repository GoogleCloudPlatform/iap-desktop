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
using Google.Solutions.Testing.Common;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    public class TestTestSshRelayStream : IapFixtureBase
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
            private readonly WebSocketConnection connection;
            private bool connectCalled = false;

            public int ReconnectCalls { get; private set; } = 0;

            public ServerWebSocketConnection Server => this.connection.Server;

            public Endpoint(WebSocketConnection connection)
            {
                this.connection = connection;
            }

            public void Dispose()
            {
                this.connection?.Dispose();
            }

            public Task<INetworkStream> ConnectAsync(CancellationToken token)
            {
                Assert.IsFalse(this.connectCalled);
                this.connectCalled = true;

                return Task.FromResult<INetworkStream>(
                    new WebSocketStream(this.connection.Client));
            }

            public Task<INetworkStream> ReconnectAsync(
                string sid,
                ulong lastByteConsumedByClient,
                CancellationToken token)
            {
                this.ReconnectCalls++;
                throw new NotImplementedException();
            }
        }

        private async Task<Endpoint> CreateEndpointAsync()
        {
            return new Endpoint(await this.server
                .ConnectAsync()
                .ConfigureAwait(false));
        }

        //---------------------------------------------------------------------
        // TestConnectionAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenReadFailsWithCloseCode_ThenTestConnectionThrowsException(
            [Values(
                CloseCode.NOT_AUTHORIZED,
                CloseCode.LOOKUP_FAILED,
                CloseCode.LOOKUP_FAILED_RECONNECT)] CloseCode code)
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
                CloseCode.NORMAL,
                CloseCode.DESTINATION_READ_FAILED,
                CloseCode.DESTINATION_WRITE_FAILED)] CloseCode code)
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
                CloseCode.NOT_AUTHORIZED)] CloseCode code)
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
                CloseCode.SID_UNKNOWN,
                CloseCode.SID_IN_USE,
                CloseCode.FAILED_TO_CONNECT_TO_BACKEND)] CloseCode code)
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

                var data = new byte[SshRelayFormat.MaxDataPayloadLength];
                data[0] = 0xAA;
                data[data.Length - 1] = 0xBB;

                await endpoint.Server
                    .SendDataAsync(data)
                    .ConfigureAwait(false);

                using (var clientStream = new SshRelayStream(endpoint))
                {
                    var buffer = new byte[SshRelayFormat.MaxDataPayloadLength + 1];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 1, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(SshRelayFormat.MaxDataPayloadLength, bytesRead);
                    Assert.AreEqual(0xAA, buffer[1]);
                    Assert.AreEqual(0xBB, buffer[buffer.Length - 1]);
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: reconnect.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenServerClosesConnectionWithUnknownError_ThenReadReconnectAndRetries(
             [Values(
                CloseCode.INVALID_WEBSOCKET_OPCODE)] CloseCode code)
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
                    Assert.AreEqual(1, endpoint.ReconnectCalls);
                }
            }
        }
    }
}
