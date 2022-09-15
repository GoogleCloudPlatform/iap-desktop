using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Net
{
    internal sealed class WebSocketServer : IDisposable
    {
        private readonly HttpListener listener;

        public Uri Endpoint { get; }

        public WebSocketServer()
        {
            this.listener = new HttpListener();

            var port = PortFinder.FindFreeLocalPort();
            this.Endpoint = new Uri($"ws://localhost:{port}/");

            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
        }

        public async Task<WebSocketConnection> AcceptConnectionAsync()
        {
            var context = await listener
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
                return new WebSocketConnection(await context
                    .AcceptWebSocketAsync(null)
                    .ConfigureAwait(false));
            }
        }

        public void Dispose()
        {
            this.listener.Stop();
        }
    }

    

    internal sealed class WebSocketConnection : IDisposable
    { 
        public HttpListenerWebSocketContext Contect { get; }

        private void ThrowIfNotConnected()
        {
            if (this.Contect == null)
            {
                throw new InvalidOperationException("WebSocket not connected");
            }
        }

        public WebSocketConnection(HttpListenerWebSocketContext webSocket)
        {
            this.Contect = webSocket;
        }

        public async Task SendBinaryFrameAsync(byte[] data)
        {
            ThrowIfNotConnected();

            await this.Contect.WebSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task ReceiveBinaryFrameAsync(byte[] buffer)
        {
            ThrowIfNotConnected();

            await this.Contect.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task CloseOutputAsync(WebSocketCloseStatus status)
        {
            ThrowIfNotConnected();

            await this.Contect.WebSocket.CloseOutputAsync(
                    status,
                    status.ToString(),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task CloseAsync(WebSocketCloseStatus status)
        {
            ThrowIfNotConnected();

            await this.Contect.WebSocket.CloseAsync(
                    status,
                    status.ToString(),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.Contect.WebSocket.Dispose();
        }
    }
}
