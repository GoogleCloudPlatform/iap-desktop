//
// Copyright 2021 Google LLC
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
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Socks5
{
    public interface ISocks5Relay
    {
        Task<ushort> CreateRelayPortAsync(
            IPEndPoint clientEndpoint,
            string destinationHost,
            CancellationToken cancellationToken);
    }

    public class Socks5Listener
    {
        private static byte[] LoopbackAddress = new byte[] { 127, 0, 0, 1 };
        private static byte[] InvalidAddress = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        private static ushort InvalidPort = 0xFFFF;

        private const int BacklogLength = 32;

        private readonly ISshRelayEndpointResolver resolver;
        private readonly ISshRelayPolicy policy;
        private readonly TcpListener listener;

        public int ListenPort { get; }
        public ConnectionStatistics Statistics { get; } = new ConnectionStatistics();

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public Socks5Listener(
            ISshRelayEndpointResolver resolver,
            ISshRelayPolicy policy,
            int listenPort)
        {
            this.resolver = resolver;
            this.policy = policy;
            this.ListenPort = listenPort;
            this.listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, listenPort));
        }

        private async Task HandleConnectionAsync(
            IPEndPoint clientEndpoint,
            Socks5Stream stream,
            CancellationToken cancellationToken)
        {
            var negotiateMethodRequest = await stream
                .ReadNegotiateMethodRequestAsync(cancellationToken)
                .ConfigureAwait(false);

            //
            // Negotiate authentication method.
            //
            if (negotiateMethodRequest.Version == Socks5Stream.ProtocolVersion &&
                negotiateMethodRequest.Methods.Contains(AuthenticationMethod.NoAuthenticationRequired))
            {
                await stream.WriteNegotiateMethodResponseAsync(
                        new NegotiateMethodResponse(
                            Socks5Stream.ProtocolVersion, 
                            AuthenticationMethod.NoAuthenticationRequired),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                IapTraceSources.Default.TraceWarning(
                    "Socks5Listener: No matching authentication methods for client {0}",
                    clientEndpoint);

                await stream.WriteNegotiateMethodResponseAsync(
                        new NegotiateMethodResponse(
                            Socks5Stream.ProtocolVersion,
                            AuthenticationMethod.NoAcceptableMethods),
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            //
            // Connect.
            //
            var connectionRequest = await stream
                .ReadConnectionRequestAsync(cancellationToken)
                .ConfigureAwait(false);

            if (connectionRequest.Version != Socks5Stream.ProtocolVersion ||
                connectionRequest.Command != Command.Connect)
            {
                await stream
                    .WriteConnectionResponseAsync(
                        new ConnectionResponse(
                            Socks5Stream.ProtocolVersion,
                            ConnectionReply.CommandNotSupported,
                            AddressType.IPv4,
                            InvalidAddress,
                            InvalidPort),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (connectionRequest.AddressType == AddressType.DomainName)
            {
                var domainName = Encoding.ASCII.GetString(
                    connectionRequest.DestinationAddress, 
                    1, 
                    connectionRequest.DestinationAddress.Length - 1);

                IapTraceSources.Default.TraceVerbose(
                    "Socks5Listener: Connection request from {0} to {1}",
                    clientEndpoint,
                    domainName);

                //
                // Check if the client is allowed at all.
                //
                if (this.policy.IsClientAllowed(clientEndpoint))
                {
                    IapTraceSources.Default.TraceInformation(
                        "Connection from {0} to {1} allowed by policy",
                        clientEndpoint,
                        domainName);
                }
                else
                {
                    IapTraceSources.Default.TraceWarning(
                        "Connection from {0} to {1} rejected by policy",
                        clientEndpoint,
                        domainName);

                    await stream
                        .WriteConnectionResponseAsync(
                            new ConnectionResponse(
                                Socks5Stream.ProtocolVersion,
                                ConnectionReply.ConnectionNotAllowed,
                                AddressType.IPv4,
                                InvalidAddress,
                                InvalidPort),
                            cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                //
                // Resolve the SOCKS-style domain name to an actual endpoint.
                // 
                ISshRelayEndpoint endpoint;
                try
                {
                    endpoint = await this.resolver
                        .ResolveEndpointAsync(domainName, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    IapTraceSources.Default.TraceWarning(
                        "Endpoint {0} cannot be resolved",
                        domainName);

                    await stream
                        .WriteConnectionResponseAsync(
                            new ConnectionResponse(
                                Socks5Stream.ProtocolVersion,
                                ConnectionReply.NetworkUnreachable,
                                AddressType.IPv4,
                                InvalidAddress,
                                InvalidPort),
                            cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                //
                // Create a new listener and keep it alive for a single connection.
                //
                // Use the same policy so that the client is checked again
                // when connecting to the listener.
                //
                var relayListener = SshRelayListener.CreateLocalListener(
                    endpoint,
                    this.policy);
                relayListener.ClientAcceptLimit = 1;

                #pragma warning disable CS4014 // Call not awaited
                relayListener.ListenAsync(cancellationToken)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            IapTraceSources.Default.TraceError(
                                "Socks5SshRelay: Connection failed", t.Exception);
                        }
                    });
                #pragma warning restore CS4014

                await stream
                    .WriteConnectionResponseAsync(
                        new ConnectionResponse(
                            Socks5Stream.ProtocolVersion,
                            ConnectionReply.GeneralServerFailure,
                            AddressType.IPv4,
                            LoopbackAddress,
                            (ushort)relayListener.LocalPort),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await stream
                    .WriteConnectionResponseAsync(
                        new ConnectionResponse(
                            Socks5Stream.ProtocolVersion,
                            ConnectionReply.AddressTypeNotSupported,
                            AddressType.IPv4,
                            InvalidAddress,
                            InvalidPort),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

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
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            var socket = this.listener.AcceptSocket();

                            var socksStream = new Socks5Stream(
                                new BufferedNetworkStream(
                                    new SocketStream(socket, this.Statistics)));
                            
                            HandleConnectionAsync(
                                (IPEndPoint)socket.RemoteEndPoint,
                                socksStream,
                                token)
                            .ContinueWith(t =>
                            {
                                socksStream.Dispose();

                                if (t.IsFaulted)
                                {
                                    IapTraceSources.Default.TraceError(
                                        "Socks5Listener: Connection failed", t.Exception);
                                }
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
