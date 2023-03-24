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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
{
    public class ConnectionServiceBase
    {
        private readonly IJobService jobService;
        private readonly ITunnelBrokerService tunnelBroker;

        public ConnectionServiceBase(IJobService jobService, ITunnelBrokerService tunnelBroker)
        {
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.tunnelBroker = tunnelBroker.ExpectNotNull(nameof(tunnelBroker));
        }

        protected async Task<TransportParameters> PrepareTransportAsync(
            InstanceLocator targetInstance,
            ushort targetPort,
            TimeSpan timeout)
        {
            var tunnel = await this.jobService
                .RunInBackground(
                    new JobDescription(
                        $"Opening IAP-TCP tunnel to {targetInstance.Name}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    async token =>
                    {
                        try
                        {
                            var destination = new TunnelDestination(
                                targetInstance,
                                targetPort);

                            return await this.tunnelBroker
                                .ConnectAsync(
                                    destination,
                                    new SameProcessRelayPolicy(),
                                    timeout)
                                .ConfigureAwait(false);
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
                    })
                .ConfigureAwait(false);

            return new TransportParameters(
                TransportParameters.TransportType.IapTunnel,
                targetInstance,
                new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort));
        }
    }
}
