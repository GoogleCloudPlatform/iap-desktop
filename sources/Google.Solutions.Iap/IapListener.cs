﻿//
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
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap
{
    /// <summary>
    /// Opens a local TCP socket and forwards connection to an IAP client,
    /// effectively acting as a port forwarder.
    /// </summary>
    public interface IIapListener
    {
        /// <summary>
        /// Local port that the listener is bound to.
        /// </summary>
        int LocalPort { get; }

        /// <summary>
        /// Statistics for all connections made using
        /// this listener.
        /// </summary>
        NetworkStatistics Statistics { get; }

        /// <summary>
        /// Perpetually listen and relay traffic until cancelled.
        /// </summary>
        Task ListenAsync(CancellationToken token);
    }

    /// <summary>
    /// Policy that determines which clients can connect to
    /// a listener.
    /// </summary>
    public interface IapListenerPolicy
    {
        /// <summary>
        /// Decide whether a remote client should be allowed access.
        /// </summary>
        bool IsClientAllowed(IPEndPoint remote);
    }

    public class IapListener : IIapListener
    {
        private const int BacklogLength = 32;

        private readonly ISshRelayEndpoint server;
        private readonly IapListenerPolicy policy;
        private readonly TcpListener listener;

        public int LocalPort { get; }
        public NetworkStatistics Statistics { get; } = new NetworkStatistics();

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

        private IapListener(
            ISshRelayEndpoint server,
            IapListenerPolicy policy,
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
            this.ClientConnected?.Invoke(this, new ClientEventArgs(client));
        }

        private void OnClientDisconnected(string client)
        {
            this.ClientDisconnected?.Invoke(this, new ClientEventArgs(client));
        }

        private void OnConnectionFailed(Exception e)
        {
            this.ConnectionFailed?.Invoke(this, new ConnectionFailedEventArgs(e));
        }

        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

        /// <summary>
        ///  Create a listener using a dynamically selected, unused local port.
        /// </summary>
        public static IapListener CreateLocalListener(
            ISshRelayEndpoint server,
            IapListenerPolicy policy)
        {
            return CreateLocalListener(server, policy, PortFinder.FindFreeLocalPort());
        }

        /// <summary>
        ///  Create a listener using a defined local port.
        /// </summary>
        public static IapListener CreateLocalListener(
            ISshRelayEndpoint server,
            IapListenerPolicy policy,
            int port)
        {
            if (port < 0 || port > ushort.MaxValue)
            {
                throw new ArgumentException("port");
            }

            return new IapListener(server, policy, port);
        }

        public Task ListenAsync(CancellationToken token)
        {
            //
            // Start listening before returning from the menthod.
            //
            this.listener.Start(BacklogLength);

            //
            // All communication is then handled asynchronously.
            //
            return Task.Run(() =>
            {
                using (token.Register(this.listener.Stop))
                {
                    while (this.ClientAcceptLimit == 0 || this.ClientAcceptLimit > this.ClientsAccepted)
                    {
                        try
                        {
                            var socket = this.listener.AcceptSocket();
                            if (this.policy.IsClientAllowed((IPEndPoint)socket.RemoteEndPoint))
                            {
                                IapTraceSources.Default.TraceInformation(
                                    "Connection from {0} allowed by policy", socket.RemoteEndPoint);

                            }
                            else
                            {
                                IapTraceSources.Default.TraceWarning(
                                    "Connection from {0} rejected by policy", socket.RemoteEndPoint);
                                socket.Close();
                                continue;
                            }

                            var clientStream = new SocketStream(socket, this.Statistics);
                            var serverStream = new SshRelayStream(this.server);

                            OnClientConnected(clientStream.ToString());
                            this.ClientsAccepted++;

                            Task.WhenAll(
                                    clientStream.RelayToAsync(serverStream, SshRelayStream.MaxWriteSize, token),
                                    serverStream.RelayToAsync(clientStream, SshRelayStream.MinReadSize, token))
                                .ContinueWith(t =>
                                {
                                    IapTraceSources.Default.TraceVerbose("SshRelayListener: Closed connection");

                                    if (t.IsFaulted)
                                    {
                                        OnConnectionFailed(t.Exception);
                                    }

                                    OnClientDisconnected(clientStream.ToString());

                                    clientStream.Dispose();
                                    serverStream.Dispose();
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