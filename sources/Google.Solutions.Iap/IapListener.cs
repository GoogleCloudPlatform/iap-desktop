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
using Google.Solutions.Common.Util;
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
        /// Local endpoint that the listener is bound to.
        /// </summary>
        IPEndPoint LocalEndpoint { get; }

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
    public interface IIapListenerPolicy
    {
        /// <summary>
        /// Decide whether a remote client should be allowed access.
        /// </summary>
        bool IsClientAllowed(IPEndPoint remote);
    }

    public class IapListener : IIapListener
    {
        private const int BacklogLength = 32;

        private readonly ISshRelayTarget server;
        private readonly IIapListenerPolicy policy;
        private readonly TcpListener listener;

        public event EventHandler<ClientEventArgs>? ClientConnected;
        public event EventHandler<ClientEventArgs>? ClientDisconnected;
        public event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;

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

        /// <summary>
        /// Create a listener.
        /// </summary>
        public IapListener(
            ISshRelayTarget server,
            IIapListenerPolicy policy,
            IPEndPoint? localEndpoint)
        {
            this.server = server.ExpectNotNull(nameof(server));
            this.policy = policy.ExpectNotNull(nameof(policy));

            if (localEndpoint == null)
            {
                //
                // The caller doesn't care which endpoint is used,
                // so allocate one dynamically.
                //
                localEndpoint = new IPEndPoint(
                    IPAddress.Loopback,
                    new PortFinder().FindPort(out var _));
            }

            this.listener = new TcpListener(localEndpoint);
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
        // IIapListener
        //---------------------------------------------------------------------

        public NetworkStatistics Statistics { get; } = new NetworkStatistics();

        public IPEndPoint LocalEndpoint => (IPEndPoint)this.listener.LocalEndpoint;

        public Task ListenAsync(CancellationToken token)
        {
            //
            // Start listening before returning from the menthod.
            //
            try
            {
                this.listener.Start(BacklogLength);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AccessDenied)
            {
                //
                // This can happen if the endpoint overlaps with a persistent port
                // reservation.
                //
                throw new PortAccessDeniedException(this.listener.LocalEndpoint);
            }

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
                                IapTraceSource.Log.TraceInformation(
                                    "Connection from {0} allowed by policy", socket.RemoteEndPoint);

                            }
                            else
                            {
                                IapTraceSource.Log.TraceWarning(
                                    "Connection from {0} rejected by policy", socket.RemoteEndPoint);
                                socket.Close();
                                continue;
                            }

                            socket.NoDelay = true;

                            var clientStream = new SocketStream(socket, this.Statistics);
                            var serverStream = new SshRelayStream(this.server);

                            OnClientConnected(clientStream.ToString());
                            this.ClientsAccepted++;

                            Task.WhenAll(
                                    clientStream.RelayToAsync(serverStream, SshRelayStream.MaxWriteSize, token),
                                    serverStream.RelayToAsync(clientStream, SshRelayStream.MinReadSize, token))
                                .ContinueWith(t =>
                                {
                                    IapTraceSource.Log.TraceVerbose("SshRelayListener: Closed connection");

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

    public class PortAccessDeniedException : Exception
    {
        public PortAccessDeniedException(EndPoint endpoint)
            : base(
                  $"Attempting to bind to port {endpoint} failed, " +
                  "possibly because of a persistent port reservation")
        {
        }
    }
}
