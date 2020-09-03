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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Test.Net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Opens a TCP local socket and relays all sent data to an
    /// SSH Relay tunnel (and vice versa). The local socket effectively
    /// serves as a "proxy" for the target port of the SSH Relay tunnel.
    /// </summary>
    public class SshRelayListener
    {
        private const int BacklogLength = 32;

        private readonly ISshRelayEndpoint server;
        private readonly ISshRelayPolicy policy;
        private readonly TcpListener listener;

        public int LocalPort { get; }

        public event EventHandler<ClientEventArgs> ClientConnected;
        public event EventHandler<ClientEventArgs> ClientDisconnected;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;

        public int ClientsAccepted { get; private set; } = 0;

        // For testing only.
        internal int ClientAcceptLimit { get; set; } = 0;

        public class ClientEventArgs : EventArgs
        {
            public string Client { get; }

            public ClientEventArgs(string client)
            {
                this.Client = client;
            }
        }

        public class ConnectionFailedEventArgs : EventArgs
        {
            public Exception Exception { get; }

            public ConnectionFailedEventArgs(Exception exception)
            {
                this.Exception = exception;
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        private SshRelayListener(
            ISshRelayEndpoint server,
            ISshRelayPolicy policy,
            int localPort)
        {
            this.server = server;
            this.policy = policy;
            this.LocalPort = localPort;

            this.listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, localPort));
        }

        //---------------------------------------------------------------------
        // Events
        //---------------------------------------------------------------------

        private void OnClientConnected(string client)
        {
            if (this.ClientConnected != null)
            {
                this.ClientConnected(this, new ClientEventArgs(client));
            }
        }
        private void OnClientDisconnected(string client)
        {
            if (this.ClientDisconnected != null)
            {
                this.ClientDisconnected(this, new ClientEventArgs(client));
            }
        }
        private void OnConnectionFailed(Exception e)
        {
            if (this.ConnectionFailed != null)
            {
                this.ConnectionFailed(this, new ConnectionFailedEventArgs(e));
            }
        }

        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

        /// <summary>
        ///  Create a listener using a dynamically selected, unused local port.
        /// </summary>
        public static SshRelayListener CreateLocalListener(
            ISshRelayEndpoint server,
            ISshRelayPolicy policy)
        {
            return CreateLocalListener(server, policy, PortFinder.FindFreeLocalPort());
        }

        /// <summary>
        ///  Create a listener using a defined local port.
        /// </summary>
        public static SshRelayListener CreateLocalListener(
            ISshRelayEndpoint server,
            ISshRelayPolicy policy,
            int port)
        {
            if (port < 0 || port > ushort.MaxValue)
            {
                throw new ArgumentException("port");
            }

            return new SshRelayListener(server, policy, port);
        }

        /// <summary>
        /// Perpetually listen and relay traffic until cancelled.
        /// </summary>
        public Task ListenAsync(CancellationToken token)
        {
            // Start listening before returning from the menthod.
            this.listener.Start(BacklogLength);

            // All communication is then handled asynchronously.
            return Task.Run(() =>
            {
                using (token.Register(this.listener.Stop))
                {
                    while (this.ClientAcceptLimit == 0 || this.ClientAcceptLimit > this.ClientsAccepted)
                    {
                        try
                        {
                            var socket = this.listener.AcceptSocket();
                            if (!this.policy.IsClientAllowed((IPEndPoint)socket.RemoteEndPoint))
                            {
                                TraceSources.Compute.TraceWarning(
                                    "Rejecting connection attempt from {0}", socket.RemoteEndPoint);
                                socket.Close();
                                continue;
                            }

                            var clientStream = new SocketStream(socket);
                            var serverStream = new SshRelayStream(this.server);

                            OnClientConnected(clientStream.ToString());
                            this.ClientsAccepted++;

                            Task.WhenAll(
                                    clientStream.RelayToAsync(serverStream, token),
                                    serverStream.RelayToAsync(clientStream, token))
                                .ContinueWith(t =>
                                {
                                    TraceSources.Compute.TraceVerbose("SshRelayListener: Closed connection");

                                    if (t.IsFaulted)
                                    {
                                        OnConnectionFailed(t.Exception);
                                    }

                                    OnClientDisconnected(clientStream.ToString());
                                });
                        }
                        catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted)
                        {
                            // Operation cancelled, terminate gracefully.
                            break;
                        }
                    }
                }
            });
        }
    }
}
