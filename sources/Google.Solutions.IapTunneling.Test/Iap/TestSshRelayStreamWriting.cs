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

using Google.Solutions.Common.Test;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;


namespace Google.Solutions.IapTunneling.Test.Iap
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
        public async Task WhenPerformingFirstWrite_ThenConnectionIsOpened()
        {
            var stream = new MockStream();
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            Assert.AreEqual(0, endpoint.ConnectCount);

            byte[] request = new byte[] { 1, 2, 3 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            Assert.AreEqual(1, endpoint.ConnectCount);
        }

        [Test]
        public async Task WhenPerformingWrite_ThenAckIsSentFirst()
        {
            var stream = new MockStream()
            {
                ExpectedReadData = new byte[][]
                {
                    new byte[]{ 0, (byte)MessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 }
                },
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                    new byte[]{ 0, (byte)MessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 1 },
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 2 }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write and read something.
            byte[] request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            byte[] buffer = new byte[relay.MinReadSize];
            int bytesRead = await relay.ReadAsync(buffer, 0, buffer.Length, tokenSource.Token);
            Assert.AreEqual(1, bytesRead);

            Assert.AreEqual(1, stream.WriteCount);
            Assert.AreEqual(2, stream.ReadCount);

            // Write a second request - this should cause an ACK to be sent first.
            request = new byte[] { 2 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            Assert.AreEqual(3, stream.WriteCount);
            Assert.AreEqual(2, stream.ReadCount);
        }

        [Test]
        public async Task WhenPerformingWriteWithoutPreviousRead_ThenNoAckIsSent()
        {
            var stream = new MockStream()
            {
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 2 }
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write two requests with no read in between.
            byte[] request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            request = new byte[] { 2 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            Assert.AreEqual(2, stream.WriteCount);
            Assert.AreEqual(0, stream.ReadCount);
        }


        [Test]
        public async Task WhenPerformingWriteAfterDestinationWriteFailed_ThenWriteFailsWithException()
        {
            var stream = new MockStream()
            {
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                },
                ExpectServerCloseCodeOnWrite = (WebSocketCloseStatus)CloseCode.DESTINATION_WRITE_FAILED
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request.
            byte[] request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            // Write another request - this should fail.
            AssertEx.ThrowsAggregateException<WebSocketStreamClosedByServerException>(() =>
            {
                request = new byte[] { 2 };
                relay.WriteAsync(request, 0, request.Length, tokenSource.Token).Wait();
            });
        }

        [Test]
        public async Task WhenClientClosedConnection_ThenSubsequentWriteFailsWithException()
        {
            var stream = new MockStream()
            {
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                }
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request, then close.
            byte[] request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);
            await relay.CloseAsync(tokenSource.Token);

            // Write another request - this should fail.
            AssertEx.ThrowsAggregateException<NetworkStreamClosedException>(() =>
            {
                request = new byte[] { 2 };
                relay.WriteAsync(request, 0, request.Length, tokenSource.Token).Wait();
            });
        }

        [Test]
        public async Task WhenServerClosedConnection_ThenSubsequentWriteFailsWithException()
        {
            var stream = new MockStream()
            {
                ExpectedWriteData = new byte[][]
                {
                    new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                },
                ExpectServerCloseCodeOnWrite = WebSocketCloseStatus.NormalClosure
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request.
            byte[] request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            // Write another request - this should fail.
            AssertEx.ThrowsAggregateException<WebSocketStreamClosedByServerException>(() =>
            {
                request = new byte[] { 2 };
                relay.WriteAsync(request, 0, request.Length, tokenSource.Token).Wait();
            });
        }

        [Test]
        public async Task WhenServerClosesConnectionForcefullyOnFirstWrite_ThenConnectIsRetriedAndDataIsResent(
            [Values(
                WebSocketCloseStatus.EndpointUnavailable,
                WebSocketCloseStatus.InvalidMessageType,
                WebSocketCloseStatus.ProtocolError,
                (WebSocketCloseStatus)CloseCode.BAD_ACK,
                (WebSocketCloseStatus)CloseCode.ERROR_UNKNOWN,
                (WebSocketCloseStatus)CloseCode.INVALID_TAG,
                (WebSocketCloseStatus)CloseCode.SID_UNKNOWN,
                (WebSocketCloseStatus)CloseCode.FAILED_TO_CONNECT_TO_BACKEND,
                (WebSocketCloseStatus)CloseCode.INVALID_WEBSOCKET_OPCODE,
                (WebSocketCloseStatus)CloseCode.REAUTHENTICATION_REQUIRED
            )] WebSocketCloseStatus closeStatus)
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[]
                {
                    new MockStream()
                    {
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 2 },
                        },
                        ExpectServerCloseCodeOnWrite = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 2 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 3 },
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write two requests, but do not await ACK.
            byte[] request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            request = new byte[] { 2 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);

            // Write another request - this should cause a reconnect and resend.
            request = new byte[] { 3 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            Assert.AreEqual(2, endpoint.ConnectCount);
            Assert.AreEqual(0, endpoint.ReconnectCount);
        }

        [Test]
        public async Task WhenDataReadAndServerClosesConnectionForcefullyOnSubsequentWrite_ThenConnectIsRetriedAndDataIsResent(
            [Values(
                WebSocketCloseStatus.EndpointUnavailable,
                WebSocketCloseStatus.InvalidMessageType,
                WebSocketCloseStatus.ProtocolError,
                (WebSocketCloseStatus)CloseCode.BAD_ACK,
                (WebSocketCloseStatus)CloseCode.ERROR_UNKNOWN,
                (WebSocketCloseStatus)CloseCode.INVALID_TAG,
                (WebSocketCloseStatus)CloseCode.SID_UNKNOWN,
                (WebSocketCloseStatus)CloseCode.FAILED_TO_CONNECT_TO_BACKEND,
                (WebSocketCloseStatus)CloseCode.INVALID_WEBSOCKET_OPCODE,
                (WebSocketCloseStatus)CloseCode.REAUTHENTICATION_REQUIRED
            )] WebSocketCloseStatus closeStatus)
        {
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStreams = new[]
                 {
                    new MockStream()
                    {
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)MessageTag.ACK, 0, 0, 0, 0, 0, 0, 0, 2 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 2 },
                        },
                        ExpectedReadData = new byte[][]
                        {
                            new byte[]{ 0, (byte)MessageTag.CONNECT_SUCCESS_SID, 0, 0, 0, 1, 0 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 2, 1, 2 }
                        },
                        ExpectServerCloseCodeOnWrite = closeStatus
                    },
                    new MockStream()
                    {
                        ExpectedWriteData = new byte[][]
                        {
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 1 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 2 },
                            new byte[]{ 0, (byte)MessageTag.DATA, 0, 0, 0, 1, 3 },
                        }
                    }
                }
            };
            var relay = new SshRelayStream(endpoint);

            // Write a request.
            var request = new byte[] { 1 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            // Read something..
            var buffer = new byte[relay.MinReadSize];
            int bytesRead = await relay.ReadAsync(buffer, 0, buffer.Length, tokenSource.Token);
            Assert.AreEqual(2, bytesRead);

            // Write another request, causing an ACK to be sent.
            request = new byte[] { 2 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            // Write another request - this should cause a reconnect and resend.
            request = new byte[] { 3 };
            await relay.WriteAsync(request, 0, request.Length, tokenSource.Token);

            Assert.AreEqual(1, endpoint.ConnectCount);
            Assert.AreEqual(1, endpoint.ReconnectCount);
        }

        [Test]
        public void WhenServerClosesConnectionWithNotAuthorizedCode_ThenWriteFailsWithUnauthorizedException()
        {
            var stream = new MockStream()
            {
                ExpectServerCloseCodeOnWrite = (WebSocketCloseStatus)CloseCode.NOT_AUTHORIZED
            };
            var endpoint = new MockSshRelayEndpoint()
            {
                ExpectedStream = stream
            };
            var relay = new SshRelayStream(endpoint);

            // Write first request - this should fail.
            AssertEx.ThrowsAggregateException<UnauthorizedException>(() =>
            {
                var request = new byte[] { 2 };
                relay.WriteAsync(request, 0, request.Length, tokenSource.Token).Wait();
            });
        }
    }
}
