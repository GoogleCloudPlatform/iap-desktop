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

using Google.Solutions.Compute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    internal abstract class TunnelManagerBase
    {
        private readonly object tunnelsLock = new object();
        private readonly IDictionary<TunnelDestination, Task<ITunnel>> tunnels =
            new Dictionary<TunnelDestination, Task<ITunnel>>();

        public IEnumerable<ITunnel> OpenTunnels
        {
            get
            {
                return this.tunnels.Values
                    .Where(t => t.IsCompleted && !t.IsFaulted)
                    .Select(t => t.Result);
            }
        }

        protected abstract Task<ITunnel> CreateTunnelAsync(TunnelDestination endpoint, TimeSpan timeout);

        private Task<ITunnel> ConnectAndCacheAsync(TunnelDestination endpoint, TimeSpan timeout)
        {
            var tunnel = CreateTunnelAsync(endpoint, timeout);
            this.tunnels[endpoint] = tunnel;
            return tunnel;
        }

        public bool IsConnected(TunnelDestination endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (this.tunnels.TryGetValue(endpoint, out Task<ITunnel> tunnel))
                {
                    return !tunnel.IsFaulted;
                }
                else
                {
                    return false;
                }
            }
        }

        public Task<ITunnel> ConnectAsync(TunnelDestination endpoint, TimeSpan timeout)
        {
            lock (this.tunnelsLock)
            {
                if (!this.tunnels.TryGetValue(endpoint, out Task<ITunnel> tunnel))
                {
                    return ConnectAndCacheAsync(endpoint, timeout);
                }
                else if (tunnel.IsFaulted)
                {
                    // There is no point in handing out a faulty attempt
                    // to create a tunnel. So start anew.
                    return ConnectAndCacheAsync(endpoint, timeout);
                }
                else
                {
                    // This tunnel is good or still in the process
                    // of connecting.
                    return tunnel;
                }
            }
        }

        public void CloseTunnel(TunnelDestination endpoint)
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
        }

        public void CloseTunnels()
        {
            lock (this.tunnelsLock)
            {
                var copyOfEndpoints = new List<TunnelDestination>(this.tunnels.Keys);

                var exceptions = new List<Exception>();
                foreach (var endpoint in copyOfEndpoints)
                {
                    try
                    {
                        CloseTunnel(endpoint);
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

    internal interface ITunnel
    {
        TunnelDestination Endpoint { get; }

        int LocalPort { get; }

        int? ProcessId { get; }

        void Close();
    }

    internal class TunnelDestination : IEquatable<TunnelDestination>
    {
        public VmInstanceReference Instance { get; private set; }

        public ushort RemotePort { get; private set; }

        public TunnelDestination(VmInstanceReference instance, ushort remotePort)
        {
            this.Instance = instance;
            this.RemotePort = remotePort;
        }

        public bool Equals(TunnelDestination other)
        {
            return
                other != null &&
                other.Instance.Equals(this.Instance) &&
                other.RemotePort == this.RemotePort;
        }

        public override bool Equals(object obj)
        {
            return obj is TunnelDestination && Equals((TunnelDestination)obj);
        }

        public override int GetHashCode()
        {
            return this.Instance.GetHashCode() ^
                (int)this.RemotePort;
        }

        public override string ToString()
        {
            return $"{this.Instance.InstanceName}:{this.RemotePort}";
        }
    }
}
