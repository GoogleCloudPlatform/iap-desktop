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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Integration
{
    public class TunnelBrokerService
    {
        private readonly ITunnelService tunnelService;
        private readonly IEventService eventService;

        private readonly object tunnelsLock = new object();
        private readonly IDictionary<TunnelDestination, Task<Tunnel>> tunnels =
            new Dictionary<TunnelDestination, Task<Tunnel>>();

        public TunnelBrokerService(
            ITunnelService tunnelService,
            IEventService eventService)
        {
            this.tunnelService = tunnelService;
            this.eventService = eventService;
        }

        public TunnelBrokerService(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<ITunnelService>(),
                  serviceProvider.GetService<IEventService>())
        {
        }

        public IEnumerable<Tunnel> OpenTunnels =>
            this.tunnels.Values
                .Where(t => t.IsCompleted && !t.IsFaulted)
                .Select(t => t.Result);

        private Task<Tunnel> ConnectAndCacheAsync(TunnelDestination endpoint)
        {
            var tunnel = this.tunnelService.CreateTunnelAsync(endpoint);

            TraceSources.IapDesktop.TraceVerbose("Created tunnel to {0}", endpoint);

            this.tunnels[endpoint] = tunnel;
            return tunnel;
        }

        public bool IsConnected(TunnelDestination endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (this.tunnels.TryGetValue(endpoint, out Task<Tunnel> tunnel))
                {
                    return !tunnel.IsFaulted;
                }
                else
                {
                    return false;
                }
            }
        }

        private Task<Tunnel> ConnectIfNecessaryAsync(TunnelDestination endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (!this.tunnels.TryGetValue(endpoint, out Task<Tunnel> tunnel))
                {
                    return ConnectAndCacheAsync(endpoint);
                }
                else if (tunnel.IsFaulted)
                {
                    TraceSources.IapDesktop.TraceVerbose(
                        "Tunnel to {0} is faulted.. reconnecting", endpoint);

                    // There is no point in handing out a faulty attempt
                    // to create a tunnel. So start anew.
                    return ConnectAndCacheAsync(endpoint);
                }
                else
                {
                    TraceSources.IapDesktop.TraceVerbose(
                        "Reusing tunnel to {0}", endpoint);

                    // This tunnel is good or still in the process
                    // of connecting.
                    return tunnel;
                }
            }
        }

        public async Task<Tunnel> ConnectAsync(TunnelDestination endpoint, TimeSpan timeout)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(endpoint, timeout))
            {
                var tunnel = await ConnectIfNecessaryAsync(endpoint);

                try
                {
                    // Whether it is a new or existing tunnel, probe it first before 
                    // handing it out. It might be broken after all (because of reauth
                    // or for other reasons).
                    await tunnel.Probe(timeout);

                    TraceSources.IapDesktop.TraceVerbose(
                        "Probing tunnel to {0} succeeded", endpoint);
                }
                catch (Exception e)
                {
                    TraceSources.IapDesktop.TraceVerbose(
                        "Probing tunnel to {0} failed: {1}", endpoint, e.Message);

                    // Un-cache this broken tunnel.
                    await DisconnectAsync(endpoint);
                    throw;
                }

                await this.eventService.FireAsync(new TunnelOpenedEvent(endpoint));

                return tunnel;
            }
        }

        public async Task DisconnectAsync(TunnelDestination endpoint)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(endpoint))
            {
                lock (this.tunnelsLock)
                {
                    if (!this.tunnels.TryGetValue(endpoint, out var tunnel))
                    {
                        throw new KeyNotFoundException($"No active tunnel to {endpoint}");
                    }

                    tunnel.Result.Close();
                    this.tunnels.Remove(endpoint);
                }

                await this.eventService.FireAsync(new TunnelClosedEvent(endpoint));
            }
        }

        public async Task DisconnectAllAsync()
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                // Create a copy of the list to avoid race conditions.
                var copyOfEndpoints = new List<TunnelDestination>(this.tunnels.Keys);

                var exceptions = new List<Exception>();
                foreach (var endpoint in copyOfEndpoints)
                {
                    try
                    {
                        await DisconnectAsync(endpoint);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }

                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions);
                }
            }
        }
    }

    public class TunnelOpenedEvent
    {
        public TunnelDestination Destination { get; }

        public TunnelOpenedEvent(TunnelDestination destination)
        {
            this.Destination = destination;
        }
    }

    public class TunnelClosedEvent
    {
        public TunnelDestination Destination { get; }

        public TunnelClosedEvent(TunnelDestination destination)
        {
            this.Destination = destination;
        }
    }
}
