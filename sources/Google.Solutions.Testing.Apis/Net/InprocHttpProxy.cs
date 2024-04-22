//
// Copyright 2020 Google LLC
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Net
{
    /// <summary>
    /// Simple implementation of a HTTP proxy that can be used in tests.
    /// </summary>
    public class InProcessHttpProxy : IDisposable
    {
        private static readonly Regex ConnectRequestPattern
            = new Regex(@"^CONNECT ([a-zA-Z0-9\.*]+):(\d+) HTTP/1.1");
        private static readonly Regex GetRequestPattern
            = new Regex(@"^GET (.*) HTTP/1.1");

        // NB. Avoid reusing the same port twice in the same process.
        private static ushort nextProxyPort = 3128;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly LinkedList<string> connectionTargets = new LinkedList<string>();
        private readonly TcpListener listener;

        private readonly Dictionary<string, string> staticFiles =
            new Dictionary<string, string>();

        public IEnumerable<string> ConnectionTargets => this.connectionTargets;

        public ushort Port { get; }

        private void DispatchRequests()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                var socket = new NetworkStream(this.listener.AcceptSocket(), true);
                var _ = DispatchRequestAsync(socket).ConfigureAwait(false);
            }
        }

        private static string ReadLine(Stream stream)
        {
            var buffer = new StringBuilder();
            while (true)
            {
                var b = stream.ReadByte();
                if (b == -1 || b == (byte)'\n')
                {
                    return buffer.ToString();
                }
                else if (b == (byte)'\r')
                { }
                else
                {
                    buffer.Append((char)b);
                }
            }
        }

        private async Task DispatchRequestAsync(NetworkStream clientStream)
        {
            using (clientStream)
            {
                var firstLine = ReadLine(clientStream);
                if (ConnectRequestPattern.Match(firstLine) is Match matchConnect && matchConnect.Success)
                {
                    //
                    // Read headers.
                    //
                    var headers = new Dictionary<string, string>();
                    string line;
                    while (!string.IsNullOrEmpty((line = ReadLine(clientStream))))
                    {
                        var parts = line.Split(':');
                        headers.Add(parts[0].ToLower(), parts[1].Trim());
                    }

                    this.connectionTargets.AddLast(matchConnect.Groups[1].Value);

                    await DispatchRequestAsync(
                            matchConnect.Groups[1].Value,
                            ushort.Parse(matchConnect.Groups[2].Value),
                            headers,
                            clientStream)
                        .ConfigureAwait(true);
                }
                else if (GetRequestPattern.Match(firstLine) is Match getMatch &&
                    getMatch.Success &&
                    this.staticFiles.TryGetValue(getMatch.Groups[1].Value, out var responseBody))
                {
                    var response = Encoding.ASCII.GetBytes(
                        "HTTP/1.1 200 OK\r\n" +
                        $"Content-Length: {responseBody.Length}\r\n" +
                        $"Content-Type: application/x-ns-proxy-autoconfig\r\n" +
                        "\r\n" +
                        responseBody);
                    clientStream.Write(response, 0, response.Length);
                }
                else
                {
                    var error = Encoding.ASCII.GetBytes($"HTTP /1.1 400 Bad Request");
                    clientStream.Write(error, 0, error.Length);
                }
            }
        }

        protected virtual async Task DispatchRequestAsync(
            string server,
            ushort serverPort,
            IDictionary<string, string> headers,
            NetworkStream clientStream)
        {
            //
            // Send response.
            //
            var response = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n\r\n");
            clientStream.Write(response, 0, response.Length);
            clientStream.Flush();

            //
            // Relay streams.
            //
            using (var client = new TcpClient(server, serverPort))
            {
                var serverStream = client.GetStream();

                await Task.WhenAll(
                        clientStream.CopyToAsync(serverStream),
                        serverStream.CopyToAsync(clientStream))
                    .ConfigureAwait(false);
            }
        }

        public InProcessHttpProxy(ushort port)
        {
            this.Port = port;
            this.listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));
            this.listener.Start();

            Task.Run(() => DispatchRequests());
        }

        public InProcessHttpProxy() : this(nextProxyPort++)
        {
        }

        public void AddStaticFile(string path, string body)
        {
            this.staticFiles.Add(path, body);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.cancellation.Cancel();
                this.listener.Stop();
            }
        }
    }

    public class InProcessAuthenticatingHttpProxy : InProcessHttpProxy
    {
        public string Realm { get; set; } = "default";
        public NetworkCredential Credential { get; set; }

        public InProcessAuthenticatingHttpProxy(
            ushort port,
            NetworkCredential credential)
            : base(port)
        {
            this.Credential = credential;
        }

        public InProcessAuthenticatingHttpProxy(
            NetworkCredential credential)
            : base()
        {
            this.Credential = credential;
        }

        protected override async Task DispatchRequestAsync(
            string server,
            ushort serverPort,
            IDictionary<string, string> headers,
            NetworkStream clientStream)
        {
            if (headers.TryGetValue("proxy-authorization", out var proxyAuthHeader))
            {
                var proxyAuth = AuthenticationHeaderValue.Parse(proxyAuthHeader);
                if (proxyAuth.Scheme.ToLower() != "basic")
                {
                    SendUnauthenticatedError(clientStream);
                    return;
                }

                var credentials = Encoding.ASCII.GetString(
                        Convert.FromBase64String(proxyAuth.Parameter)).Split(':');

                if (credentials.Length != 2 ||
                    credentials[0] != this.Credential.UserName ||
                    credentials[1] != this.Credential.Password)
                {
                    SendUnauthenticatedError(clientStream);
                }
                else
                {
                    await base.DispatchRequestAsync(server, serverPort, headers, clientStream)
                        .ConfigureAwait(true);
                }
            }
            else
            {
                SendUnauthenticatedError(clientStream);
            }
        }

        private void SendUnauthenticatedError(NetworkStream clientStream)
        {
            var response = Encoding.ASCII.GetBytes(
                "HTTP/1.1 407 Proxy Authentication Required\r\n" +
                $"Proxy-Authenticate: Basic realm={this.Realm}\r\n" +
                "\r\n");
            clientStream.Write(response, 0, response.Length);
            clientStream.Flush();
        }
    }
}
