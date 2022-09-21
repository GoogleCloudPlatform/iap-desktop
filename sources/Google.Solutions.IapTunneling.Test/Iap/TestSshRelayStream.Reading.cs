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

using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.Testing.Common;
using NUnit.Framework;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1825 // Avoid zero-length array allocations.

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    public class TestSshRelayStreamReading : IapFixtureBase
    {
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        [Test]
        public async Task WhenPerformingFirstRead_ThenConnectionIsOpened()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                    new byte[]{ }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            Assert.AreEqual(0, endpoint.ConnectCount);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(1, endpoint.ConnectCount);
        }

        [Test]
        public void WhenBufferIsTiny_ThenReadFailsWithIndexOutOfRangeException()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[64],
                    new byte[]{ }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            Assert.AreEqual(0, endpoint.ConnectCount);

            byte[] buffer = new byte[SshRelayStream.MinReadSize - 1];

            ExceptionAssert.ThrowsAggregateException<IndexOutOfRangeException>(() =>
            {
                relay.ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token).Wait();
            });
        }

        [Test]
        public void WhenReadingTruncatedMessage_ThenReadFailsWithInvalidServerResponseException()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0 },
                    new byte[]{ }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            Assert.AreEqual(0, endpoint.ConnectCount);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];

            ExceptionAssert.ThrowsAggregateException<InvalidServerResponseException>(() =>
            {
                relay.ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token).Wait();
            });
        }

        [Test]
        public async Task WhenReadEncountersUnrecognizedMessageTag_ThenReadSkipsMessage(
            [Values(
                (byte)SshRelayMessageTag.UNUSED_0,
                (byte)SshRelayMessageTag.DEPRECATED,
                (byte)SshRelayMessageTag.UNUSED_5,
                (byte)SshRelayMessageTag.UNUSED_6,
                (byte)SshRelayMessageTag.UNUSED_8,
                (byte)SshRelayMessageTag.LONG_CLOSE)] byte tag)
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, tag, 0, 0, 0, 0, 0, 0, 0, 0 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                    new byte[]{ 0, tag, 0, 0, 0, 0, 0, 0, 0, 0 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 0xA, 0xB },
                    new byte[]{ 0, tag, 0, 0, 0, 0, 0, 0, 0, 0 },
                    new byte[]{ }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(2, bytesRead);
            Assert.AreEqual(0xA, buffer[0]);
            Assert.AreEqual(0xB, buffer[1]);

            bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(0, bytesRead);
        }

        [Test]
        public async Task WhenAckIsRead_ThenUnacknoledgedQueueIsTrimmed()
        {
            byte[] request = new byte[] { 1, 2, 3, 4 };

            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, (byte)request.Length },
                    new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, (byte)(request.Length*3) },
                    new byte[]{ }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            Assert.AreEqual(0, relay.UnacknoledgedMessageCount);
            Assert.AreEqual(0, relay.ExpectedAck);

            // Send 3 messages.
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(3, relay.UnacknoledgedMessageCount);
            Assert.AreEqual((byte)(request.Length * 3), relay.ExpectedAck);

            // Receive 2 ACKs.
            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(0, bytesRead);
            Assert.AreEqual(0, relay.UnacknoledgedMessageCount);
            Assert.AreEqual(0, relay.ExpectedAck);
        }

        [Test]
        public async Task WhenAckIsZero_ThenReadFailsWithInvalidServerResponseException()
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = new MockStream()
                {
                    ExpectedReadData = new byte[][]
                    {
                        new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 0 },
                        new byte[]{ }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Send a message.
            byte[] request = new byte[] { 1, 2, 3, 4 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Receive invalid ACK.
            ExceptionAssert.ThrowsAggregateException<InvalidServerResponseException>(() =>
            {
                byte[] buffer = new byte[SshRelayStream.MinReadSize];
                int bytesRead = relay.ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token).Result;
            });
        }

        [Test]
        public async Task WhenAckIsMismatched_ThenReadFailsWithInvalidServerResponseException()
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = new MockStream()
                {
                    ExpectedReadData = new byte[][]
                    {
                        new byte[]{ 0, (byte)SshRelayMessageTag.ACK, 0, 0, 0, 10 },
                        new byte[]{ }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Send 5 bytes.
            byte[] request = new byte[] { 1, 2, 3, 4 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            // Receive invalid ACK for byte 10.
            ExceptionAssert.ThrowsAggregateException<InvalidServerResponseException>(() =>
            {
                byte[] buffer = new byte[SshRelayStream.MinReadSize];
                int bytesRead = relay.ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token).Result;
            });
        }

        [Test]
        public async Task WhenReadingData_ThenDataHeaderIsTrimmed()
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = new MockStream()
                {
                    ExpectedReadData = new byte[][]
                    {
                        new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                        new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 0xA, 0xB },
                        new byte[]{ }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(2, bytesRead);
            Assert.AreEqual(0xA, buffer[0]);
            Assert.AreEqual(0xB, buffer[1]);
        }

        [Test]
        public async Task WhenServerClosesConnectionGracefully_ThenReadReturnsZero()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 },
                    new byte[]{ }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);

            bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(0, bytesRead);
        }

        [Test]
        public async Task WhenServerClosesConnectionWithDestinationReadFailedCode_ThenReadReturnsZero()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                    new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                },
                ExpectServerCloseCodeOnRead = (WebSocketCloseStatus)SshRelayCloseCode.DESTINATION_READ_FAILED
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);

            bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(0, bytesRead);
        }

        [Test]
        public async Task WhenServerClosesConnectionForcefullyOnFirstRead_ThenConnectIsRetried(
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
                ExpectedStreams = new[] {
                    new MockStream()
                    {
                        ExpectServerCloseCodeOnRead = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 1, 2 }
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // connection breaks, triggering another connect.
            var buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(2, bytesRead);
            Assert.AreEqual(2, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);
        }


        [Test]
        public async Task WhenServerClosesConnectionForcefullyOnSubsequentReadAndReconnectFails_ThenReadFailsWithException(
            [Values(
                (WebSocketCloseStatus)SshRelayCloseCode.SID_UNKNOWN,
                (WebSocketCloseStatus)SshRelayCloseCode.SID_IN_USE
            )] WebSocketCloseStatus closeStatus)
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[] {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                        },
                        ExpectServerCloseCodeOnRead = WebSocketCloseStatus.ProtocolError
                    },
                    new MockStream()
                    {
                        ExpectServerCloseCodeOnRead = closeStatus
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // read data
            var buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);

            // connection breaks, triggering a reconnect that will fail.
            ExceptionAssert.ThrowsAggregateException<WebSocketStreamClosedByServerException>(() =>
            {
                relay.ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token).Wait();
            });
        }

        [Test]
        public async Task WhenServerClosesConnectionForcefullyOnSubsequentRead_ThenReconnectIsPerformed(
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
                ExpectedStreams = new[] {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                        },
                        ExpectServerCloseCodeOnRead = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 1, 2 }
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // read data
            var buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);

            // connection breaks, triggering a reconnect.
            bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(2, bytesRead);
            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(1, endpoint.ReconnectCount);
        }


        [Test]
        public async Task WhenServerClosesConnectionForcefullyOnWriteAndSubsequentRead_ThenReconnectIsPerformed(
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
                ExpectedStreams = new[] {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                        },
                        ExpectServerCloseCodeOnRead = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.RECONNECT_SUCCESS_ACK, 0, 0, 0, 0, 0, 0, 0, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                        },
                        ExpectServerCloseCodeOnRead = closeStatus
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write something so that a connection breakdown causes a reconnect,
            // not just another connect.
            var request = new byte[] { 1, 2, 3 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);
            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);

            // connection breaks, triggering reconnect.

            bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);
            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(1, endpoint.ReconnectCount);
        }

        [Test]
        public async Task WhenClientClosedConnection_SubsequentReadFailsWithException()
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[] {
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 1, 1 }
                        }
                    },
                    new MockStream()
                    {
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)SshRelayMessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)SshRelayMessageTag.DATA, 0, 0, 0, 2, 1, 2 }
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write and read something.
            byte[] request = new byte[] { 1, 2, 3, 4 };
            await relay
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            byte[] buffer = new byte[SshRelayStream.MinReadSize];
            int bytesRead = await relay
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(1, bytesRead);

            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);

            await relay
                .CloseAsync(this.tokenSource.Token)
                .ConfigureAwait(false);

            ExceptionAssert.ThrowsAggregateException<NetworkStreamClosedException>(() =>
            {
                bytesRead = relay.ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token).Result;
            });
        }

        [Test]
        public void WhenServerClosesConnectionWithNotAuthorized_ThenTestConnectionAsyncThrowsUnauthorizedException()
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

            ExceptionAssert.ThrowsAggregateException<SshRelayDeniedException>(
                () => relay
                    .TestConnectionAsync(TimeSpan.FromSeconds(2))
                    .Wait());
        }
    }
}
