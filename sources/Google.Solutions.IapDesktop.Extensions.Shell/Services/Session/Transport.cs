//
// Copyright 2023 Google LLC
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


using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    public interface ITransport
    {
        /// <summary>
        /// Type of transport.
        /// </summary>
        Transport.TransportType Type { get; }

        /// <summary>
        /// Endpoint to connect to. This might be a localhost endpoint.
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Connection target.
        /// </summary>
        InstanceLocator Instance { get; }
    }

    public class Transport : ITransport
    {
        public InstanceLocator Instance { get; }
        public TransportType Type { get; }

        public IPEndPoint Endpoint { get; }

        private Transport(
            TransportType type,
            InstanceLocator instance,
            IPEndPoint endpoint)
        {
            this.Type = type;
            this.Instance = instance.ExpectNotNull(nameof(instance));
            this.Endpoint = endpoint.ExpectNotNull(nameof(endpoint));
        }

        public enum TransportType
        {
            IapTunnel
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        internal async static Task<Transport> CreateIapTransportAsync(
            ITunnelBrokerService tunnelBroker,
            InstanceLocator targetInstance,
            ushort targetPort,
            TimeSpan timeout)
        {
            tunnelBroker.ExpectNotNull(nameof(tunnelBroker));
            targetInstance.ExpectNotNull(nameof(targetInstance));

            try
            {
                var destination = new TunnelDestination(
                    targetInstance,
                    targetPort);

                var tunnel = await tunnelBroker
                    .ConnectAsync(
                        destination,
                        new SameProcessRelayPolicy(),
                        timeout)
                    .ConfigureAwait(false);

                return new Transport(
                    TransportType.IapTunnel,
                    targetInstance,
                    new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort));
            }
            catch (SshRelayDeniedException e)
            {
                throw new ConnectionFailedException(
                    "You are not authorized to connect to this VM instance.\n\n" +
                    $"Verify that the Cloud IAP API is enabled in the project {targetInstance.ProjectId} " +
                    "and that your user has the 'IAP-secured Tunnel User' role.",
                    HelpTopics.IapAccess,
                    e);
            }
            catch (NetworkStreamClosedException e)
            {
                throw new ConnectionFailedException(
                    "Connecting to the instance failed. Make sure that you have " +
                    "configured your firewall rules to permit IAP-TCP access " +
                    $"to {targetInstance.Name}",
                    HelpTopics.CreateIapFirewallRule,
                    e);
            }
            catch (WebSocketConnectionDeniedException)
            {
                throw new ConnectionFailedException(
                    "Establishing an IAP-TCP tunnel failed because the server " +
                    "denied access.\n\n" +
                    "If you are using a proxy server, make sure that the proxy " +
                    "server allows WebSocket connections.",
                    HelpTopics.ProxyConfiguration);
            }
        }
    }
}
