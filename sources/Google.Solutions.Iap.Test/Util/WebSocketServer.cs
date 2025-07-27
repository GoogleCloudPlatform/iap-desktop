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
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Util
{
    internal sealed class WebSocketServer : IDisposable
    {
        private readonly HttpListener listener;

        public Uri Endpoint { get; }

        public WebSocketServer()
        {
            this.listener = new HttpListener();

            var port = new PortFinder().FindPort(out var _);
            this.Endpoint = new Uri($"ws://localhost:{port}/");

            this.listener.Prefixes.Add($"http://localhost:{port}/");
            this.listener.Start();
        }

        public async Task<WebSocketConnection> ConnectAsync()
        {
            //
            // Begin connecting client (the server is listening already).
            //
            var clientSocket = new ClientWebSocket();
            var connectTask = clientSocket.ConnectAsync(
                this.Endpoint,
                CancellationToken.None);

            //
            // Let server accept connection.
            //
            var context = await this.listener
                .GetContextAsync()
                .ConfigureAwait(false);

            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                throw new InvalidOperationException(
                    "HTTP request is not a WebSocket request");
            }
            else
            {
                var serverSocket = await context
                    .AcceptWebSocketAsync(null)
                    .ConfigureAwait(false);

                await connectTask.ConfigureAwait(false);

                return new WebSocketConnection(
                    new ServerWebSocketConnection(serverSocket),
                    clientSocket);
            }
        }

        public void Dispose()
        {
            this.listener.Stop();
        }
    }

    internal sealed class WebSocketConnection : IDisposable
    {
        public ServerWebSocketConnection Server { get; }
        public ClientWebSocket Client { get; }

        public WebSocketConnection(
            ServerWebSocketConnection server,
            ClientWebSocket client)
        {
            this.Server = server;
            this.Client = client;
        }

        public void Dispose()
        {
            this.Client.Dispose();
            this.Server.Dispose();
        }
    }

    internal sealed class ServerWebSocketConnection : IDisposable
    {
        public HttpListenerWebSocketContext Context { get; }

        private void ThrowIfNotConnected()
        {
            if (this.Context == null)
            {
                throw new InvalidOperationException("WebSocket not connected");
            }
        }

        public ServerWebSocketConnection(HttpListenerWebSocketContext webSocket)
        {
            this.Context = webSocket;
        }

        public Task SendBinaryFrameAsync(byte[] data)
            => SendBinaryFrameAsync(data, 0, data.Length);

        public async Task SendBinaryFrameAsync(byte[] data, int offset, int length)
        {
            ThrowIfNotConnected();

            await this.Context.WebSocket.SendAsync(
                    new ArraySegment<byte>(data, offset, length),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task<int> ReceiveBinaryFrameAsync(byte[] buffer)
        {
            ThrowIfNotConnected();

            var result = await this.Context.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None)
                .ConfigureAwait(false);
            return result.Count;
        }

        public async Task CloseOutputAsync(WebSocketCloseStatus status)
        {
            ThrowIfNotConnected();

            await this.Context.WebSocket.CloseOutputAsync(
                    status,
                    status.ToString(),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task CloseAsync(WebSocketCloseStatus status)
        {
            ThrowIfNotConnected();

            await this.Context.WebSocket.CloseAsync(
                    status,
                    status.ToString(),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.Context.WebSocket.Dispose();
        }
    }
}
