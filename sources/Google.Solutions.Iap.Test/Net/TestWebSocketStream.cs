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

using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Test.Util;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Net
{
    [TestFixture]
    public class TestWebSocketStream : IapFixtureBase
    {
        private WebSocketServer? server;

        private WebSocketServer Server
            => this.server ?? throw new InvalidOperationException();

        [OneTimeSetUp]
        public void StartServer()
        {
            this.server = new WebSocketServer();
        }

        [OneTimeTearDown]
        public void StopServer()
        {
            this.server?.Dispose();
        }

        private static byte[] FillBuffer(uint size)
        {
            var buffer = new byte[size];

            for (uint i = 0; i < size; i++)
            {
                buffer[i] = (byte)i;
            }

            return buffer;
        }

        //---------------------------------------------------------------------
        // Read: closing.
        //---------------------------------------------------------------------

        [Test]
        public async Task Read_WhenServerClosesConnectionWithError()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.InternalServerError)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var buffer = new byte[32];

                    try
                    {
                        await clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .ConfigureAwait(false);

                        Assert.Fail();
                    }
                    catch (WebSocketStreamClosedByServerException e)
                    {
                        Assert.AreEqual(
                            WebSocketCloseStatus.InternalServerError,
                            e.CloseStatus);
                        Assert.AreEqual(
                            WebSocketCloseStatus.InternalServerError.ToString(),
                            e.CloseStatusDescription);
                    }

                    await ExceptionAssert
                        .ThrowsAsync<NetworkStreamClosedException>(() => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None))
                        .ConfigureAwait(false);
                }
            }
        }

        [Test]
        public async Task Read_WhenConnectionClosedByClient()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    await clientStream
                        .CloseAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    var buffer = new byte[32];
                    await ExceptionAssert
                        .ThrowsAsync<NetworkStreamClosedException>(() => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None))
                        .ConfigureAwait(false);
                }
            }
        }

        [Test]
        public async Task Read_WhenServerClosesConnectionNormally()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var buffer = new byte[32];

                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(0, bytesRead);
                }
            }
        }

        [Test]
        public async Task Read_WhenServerClosesConnectionAndReadSizeZero()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var bytesRead = await clientStream
                        .ReadAsync(Array.Empty<byte>(), 0, 0, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(0, bytesRead);
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: frame >= buffer.
        //---------------------------------------------------------------------

        [Test]
        public async Task Read_WhenFrameSizeEqualsReadSize()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                var frame = FillBuffer(8);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var buffer = new byte[8];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(8, bytesRead);
                    CollectionAssert.AreEquivalent(frame, buffer);
                }
            }
        }

        [Test]
        public async Task Read_WhenFrameSizeEqualToTwiceReadSize()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                var frame = FillBuffer(8);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var buffer = new byte[8];

                    // Read first half
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, 4, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Read next half.
                    bytesRead += await clientStream
                        .ReadAsync(buffer, 4, 4, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(8, bytesRead);
                    CollectionAssert.AreEquivalent(frame, buffer);
                }
            }
        }

        [Test]
        public async Task Read_WhenFrameLessThanSizeTwiceReadSize()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                var frame = FillBuffer(8);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var buffer = new byte[9];

                    // Read first half
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, 4, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Read next half.
                    bytesRead += await clientStream
                        .ReadAsync(buffer, 4, 5, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(8, bytesRead);
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: frame < buffer.
        //---------------------------------------------------------------------

        [Test]
        public async Task Read_WhenFrameLessThanReadSize()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                // Send 2 frames
                var frame = FillBuffer(4);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    // Use a buffer that could fit both frames.
                    var buffer = new byte[8];

                    // Read first frame
                    var bytesRead1 = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(4, bytesRead1);

                    var bytesRead2 = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(4, bytesRead2);
                }
            }
        }

        //---------------------------------------------------------------------
        // Write: closing.
        //---------------------------------------------------------------------

        [Test]
        public async Task Write_WhenServerClosedConnection()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.InternalServerError)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    var buffer = FillBuffer(8);
                    await clientStream
                        .WriteAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    var frame = new byte[8];
                    await connection.Server
                        .ReceiveBinaryFrameAsync(frame)
                        .ConfigureAwait(false);

                    CollectionAssert.AreEquivalent(buffer, frame);
                }
            }
        }

        [Test]
        public async Task Write_WhenConnectionClosedByClient()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    await clientStream
                        .CloseAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    var buffer = FillBuffer(8);
                    await ExceptionAssert
                        .ThrowsAsync<NetworkStreamClosedException>(() => clientStream
                            .WriteAsync(buffer, 0, buffer.Length, CancellationToken.None))
                        .ConfigureAwait(false);
                }
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task Close_WhenConnectionClosedByClient()
        {
            using (var connection = await this.Server.ConnectAsync())
            {
                using (var clientStream = new WebSocketStream(connection.Client))
                {
                    await clientStream
                        .CloseAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    await ExceptionAssert
                        .ThrowsAsync<WebSocketStreamClosedByClientException>(() => clientStream
                            .CloseAsync(CancellationToken.None))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
