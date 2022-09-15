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

        [SetUp]
        public void StartServer()
        {
            this.server = new WebSocketServer();
        }

        [TearDown]
        public void StopServer()
        {
            this.server.Dispose();
        }

        [Test]
        public async Task WhenConnectionClosedImmediately_ThenReadThrowsWebSocketStreamClosedByServerException()
        {
            using (var client = new ClientWebSocket())
            {
                var connectTask = client.ConnectAsync(
                        this.server.Endpoint,
                        CancellationToken.None);

                using (var server = await this.server.AcceptConnectionAsync())
                {
                    await connectTask.ConfigureAwait(false);

                    using (var clientStream = new WebSocketStream(
                        client,
                        64))
                    {
                        await server
                            .CloseOutputAsync(WebSocketCloseStatus.InternalServerError)
                            .ConfigureAwait(false);

                        var buffer = new byte[64];

                        ExceptionAssert.ThrowsAggregateException<WebSocketStreamClosedByServerException>(
                            () => clientStream
                                .ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)
                                .Wait());
                    }
                }
            }
        }
    }
}
