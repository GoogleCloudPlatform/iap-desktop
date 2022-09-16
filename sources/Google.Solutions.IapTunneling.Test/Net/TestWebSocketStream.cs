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

using Google.Solutions.IapTunneling.Net;
using Google.Solutions.Testing.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Net
{
    [TestFixture]
    public class TestWebSocketStream : IapFixtureBase
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
        public async Task WhenServerClosesConnectionWithError_ThenReadThrowsException()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.InternalServerError)
                    .ConfigureAwait(false);

                var bufferSize = 32;
                using (var clientStream = new WebSocketStream(connection.Client, bufferSize))
                {
                    var buffer = new byte[bufferSize];

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

                    ExceptionAssert.ThrowsAggregateException<NetworkStreamClosedException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task WhenConnectionClosedByClient_ThenReadThrowsException()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.InternalServerError)
                    .ConfigureAwait(false);

                var bufferSize = 32;
                using (var clientStream = new WebSocketStream(connection.Client, bufferSize))
                {
                    await clientStream
                        .CloseAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    var buffer = new byte[bufferSize];
                    ExceptionAssert.ThrowsAggregateException<NetworkStreamClosedException>(
                        () => clientStream
                            .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task WhenServerClosesConnectionNormally_ThenReadReturnsZero()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure)
                    .ConfigureAwait(false);

                var bufferSize = 32;
                using (var clientStream = new WebSocketStream(connection.Client, bufferSize))
                {
                    var buffer = new byte[bufferSize];

                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(0, bytesRead);
                }
            }
        }

        [Test]
        public async Task WhenServerClosesConnectionAndReadSizeZero_ThenReadSucceeds()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                await connection.Server
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure)
                    .ConfigureAwait(false);

                var bufferSize = 32;
                using (var clientStream = new WebSocketStream(connection.Client, bufferSize))
                {
                    var bytesRead = await clientStream
                        .ReadAsync(new byte[0], 0, 0, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(0, bytesRead);
                }
            }
        }

        //---------------------------------------------------------------------
        // Read: frame >= buffer.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFrameSizeEqualsReadSize_ThenReadSucceeds()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                var frame = FillBuffer(8);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                var bufferSize = 8;
                using (var clientStream = new WebSocketStream(connection.Client, bufferSize))
                {
                    var buffer = new byte[bufferSize];
                    var bytesRead = await clientStream
                        .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(8, bytesRead);
                    CollectionAssert.AreEquivalent(frame, buffer);
                }
            }
        }

        [Test]
        public async Task WhenFrameSizeEqualToTwiceReadSize_ThenReadSucceeds()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                var frame = FillBuffer(8);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client, 0))
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
        public async Task WhenFrameLessThanSizeTwiceReadSize_ThenReadSucceeds()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                var frame = FillBuffer(8);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client, 0))
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
        public async Task WhenFrameLessThanReadSize_ThenReadSucceeds()
        {
            using (var connection = await this.server.ConnectAsync())
            {
                // Send 2 frames
                var frame = FillBuffer(4);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);
                await connection.Server
                    .SendBinaryFrameAsync(frame)
                    .ConfigureAwait(false);

                using (var clientStream = new WebSocketStream(connection.Client, 0))
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
    }
}
