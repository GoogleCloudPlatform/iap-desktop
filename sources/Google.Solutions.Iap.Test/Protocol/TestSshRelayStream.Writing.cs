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

using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;


namespace Google.Solutions.Iap.Test.Protocol
{
    [TestFixture]
    public class TestSshRelayStreamWriting : IapFixtureBase
    {
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        [SetUp]
        public void SetUp()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [Test]
        public async Task Write_WhenPerformingFirstWrite_ThenConnectionIsOpened()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            Assert.AreEqual(0, endpoint.ConnectCount);

            var request = new byte[] { 1, 2, 3 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(1, endpoint.ConnectCount);
        }

        [Test]
        public async Task Write_WhenPerformingWrite_ThenAckIsSentFirst()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                },
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 1 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write and read something.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            var buffer = new byte[SshRelayStream.MinReadSize];
            var bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);

            Assert.AreEqual(1, stream.WriteCount);
            Assert.AreEqual(2, stream.ReadCount);

            // Write a second request - this should cause an ACK to be sent first.
            request = new byte[] { 2 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(3, stream.WriteCount);
            Assert.AreEqual(2, stream.ReadCount);
        }

        [Test]
        public async Task Write_WhenPerformingWriteWithoutPreviousRead_ThenNoAckIsSent()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                },
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write two requests with no read in between.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            request = new byte[] { 2 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(2, stream.WriteCount);
            Assert.AreEqual(1, stream.ReadCount);
        }

        [Test]
        public async Task Write_WhenPerformingWriteAfterBackendFailure_ThenWriteFailsWithException()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                },
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                },
                ExpectServerCloseCodeOnWrite = (WebSocketCloseStatus)SshRelayCloseCode.FAILED_TO_CONNECT_TO_BACKEND
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Write another request - this should fail.
            request = new byte[] { 2 };
            await ExceptionAssert
                .ThrowsAsync<SshRelayConnectException>(
                    () => relay.WriteAsync(request, 0, request.Length, this.tokenSource.Token))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Write_WhenClientClosedConnection_ThenSubsequentWriteFailsWithException()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                },
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request, then close.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            await relay
                .CloseAsync(this.tokenSource.Token)
                .ConfigureAwait(false);

            // Write another request - this should fail.
            request = new byte[] { 2 };
            await ExceptionAssert
                .ThrowsAsync<NetworkStreamClosedException>(
                    () => relay.WriteAsync(request, 0, request.Length, this.tokenSource.Token))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Write_WhenServerClosedConnection_ThenSubsequentWriteFailsWithException()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                },
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                },
                ExpectServerCloseCodeOnWrite = WebSocketCloseStatus.NormalClosure
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Write another request - this should fail.
            request = new byte[] { 2 };
            await ExceptionAssert
                .ThrowsAsync<WebSocketStreamClosedByServerException>(
                    () => relay.WriteAsync(request, 0, request.Length, this.tokenSource.Token))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Write_WhenServerClosesConnectionForcefullyBeforeReceivingFirstAck_ThenChannelConnectsAgainAndDataIsResent(
            [Values(
                WebSocketCloseStatus.EndpointUnavailable,
                WebSocketCloseStatus.InvalidMessageType,
                WebSocketCloseStatus.ProtocolError,
                (WebSocketCloseStatus)SshRelayCloseCode.BAD_ACK,
                (WebSocketCloseStatus)SshRelayCloseCode.ERROR_UNKNOWN,
                (WebSocketCloseStatus)SshRelayCloseCode.INVALID_TAG,
                (WebSocketCloseStatus)SshRelayCloseCode.INVALID_WEBSOCKET_OPCODE,
                (WebSocketCloseStatus)SshRelayCloseCode.REAUTHENTICATION_REQUIRED
            )] WebSocketCloseStatus closeStatus)
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[]
                {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                        },
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 },
                        },
                        ExpectServerCloseCodeOnWrite = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 }
                        },
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 3 },
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write two requests, but do not await ACK.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            request = new byte[] { 2 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);

            // Write another request - this should cause a reconnect and resend.
            request = new byte[] { 3 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(2, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);
        }

        [Test]
        public async Task Write_WhenDataReadAndServerClosesConnectionForcefullyAfterReceivingFirstAck_ThenChannelReconnectsAndDataIsResent(
            [Values(
                WebSocketCloseStatus.EndpointUnavailable,
                WebSocketCloseStatus.InvalidMessageType,
                WebSocketCloseStatus.ProtocolError,
                (WebSocketCloseStatus)SshRelayCloseCode.BAD_ACK,
                (WebSocketCloseStatus)SshRelayCloseCode.ERROR_UNKNOWN,
                (WebSocketCloseStatus)SshRelayCloseCode.INVALID_TAG,
                (WebSocketCloseStatus)SshRelayCloseCode.INVALID_WEBSOCKET_OPCODE,
                (WebSocketCloseStatus)SshRelayCloseCode.REAUTHENTICATION_REQUIRED
            )] WebSocketCloseStatus closeStatus)
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[]
                 {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 1 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 1, 2 }
                        },
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 2 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 },
                        },
                        ExpectServerCloseCodeOnWrite = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.RECONNECT_SUCCESS_ACK, 0, 0, 0, 0, 0, 0, 0, 1 },
                        },
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 3 },
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write a request.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Read something..
            var buffer = new byte[SshRelayStream.MinReadSize];
            var bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(2, bytesRead);

            // Write another request, causing an ACK to be sent.
            request = new byte[] { 2 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Write another request - this should cause a reconnect and resend.
            request = new byte[] { 3 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(1, endpoint.ReconnectCount);
        }


        [Test]
        public async Task Write_WhenDataReadAndServerClosesConnectionForcefullyAfterReceivingFirstAck_ThenChannelReconnectsAndDataSinceReconnectAckIsResent(
            [Values(
                WebSocketCloseStatus.EndpointUnavailable,
                WebSocketCloseStatus.InvalidMessageType,
                WebSocketCloseStatus.ProtocolError,
                (WebSocketCloseStatus)SshRelayCloseCode.BAD_ACK,
                (WebSocketCloseStatus)SshRelayCloseCode.ERROR_UNKNOWN,
                (WebSocketCloseStatus)SshRelayCloseCode.INVALID_TAG,
                (WebSocketCloseStatus)SshRelayCloseCode.INVALID_WEBSOCKET_OPCODE,
                (WebSocketCloseStatus)SshRelayCloseCode.REAUTHENTICATION_REQUIRED
            )] WebSocketCloseStatus closeStatus)
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[]
                 {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 1 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 1, 2 }
                        },
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 2 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 2 },
                        },
                        ExpectServerCloseCodeOnWrite = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.RECONNECT_SUCCESS_ACK, 0, 0, 0, 0, 0, 0, 0, 2 },
                        },
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 3 },
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write a request.
            var request = new byte[] { 1 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Read something..
            var buffer = new byte[SshRelayStream.MinReadSize];
            var bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(2, bytesRead);

            // Write another request, causing an ACK to be sent.
            request = new byte[] { 2 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Write another request - this should cause a reconnect and resend.
            request = new byte[] { 3 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(1, endpoint.ReconnectCount);
        }

        [Test]
        public async Task Write_WhenServerClosesConnectionWithNotAuthorizedCode_ThenWriteFailsWithUnauthorizedException()
        {
            var stream = new MockStream()
            {
                ExpectServerCloseCodeOnRead = (WebSocketCloseStatus)SshRelayCloseCode.NOT_AUTHORIZED
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request - this should fail.
            var request = new byte[] { 2 };
            await ExceptionAssert
                .ThrowsAsync<SshRelayDeniedException>(
                    () => relay.WriteAsync(request, 0, request.Length, this.tokenSource.Token))
                .ConfigureAwait(false);
        }
    }
}
