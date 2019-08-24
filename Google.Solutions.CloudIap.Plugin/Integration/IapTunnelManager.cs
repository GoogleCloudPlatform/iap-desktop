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

using Google.Solutions.CloudIap.Plugin.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    /// <summary>
    /// Manages IAP tunnels so that there is at most one active tunnel
    /// per target VM.
    /// </summary>
    internal class IapTunnelManager
    {
        private readonly object tunnelsLock = new object();
        private readonly IDictionary<IapTunnelEndpoint, Task<IapTunnel>> tunnels = 
            new Dictionary<IapTunnelEndpoint, Task<IapTunnel>>();
        private readonly PluginConfiguration configuration;

        public IapTunnelManager(PluginConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<IapTunnel> OpenTunnels
        {
            get
            {
                return this.tunnels.Values
                    .Where(t => t.IsCompleted && !t.IsFaulted)
                    .Select(t => t.Result);
            }
        }

        private Task<IapTunnel> ConnectAndCache(IapTunnelEndpoint endpoint, TimeSpan timeout)
        {
            var tunnel = IapTunnel.Open(
                        configuration.GcloudCommandPath,
                        endpoint,
                        configuration.IapConnectionTimeout);
            this.tunnels[endpoint] = tunnel;
            return tunnel;
        }

        public bool IsConnected(IapTunnelEndpoint endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (this.tunnels.TryGetValue(endpoint, out Task<IapTunnel> tunnel))
                {
                    return !tunnel.IsFaulted;
                }
                else
                {
                    return false;
                }
            }
        }

        public Task<IapTunnel> Connect(IapTunnelEndpoint endpoint, TimeSpan timeout)
        {
            lock (this.tunnelsLock)
            {
                if (!this.tunnels.TryGetValue(endpoint, out Task<IapTunnel> tunnel))
                {
                    return ConnectAndCache(endpoint, timeout);
                }
                else if (tunnel.IsFaulted)
                {
                    // There is no point in handing out a faulty attempt
                    // to create a tunnel. So start anew.
                    return ConnectAndCache(endpoint, timeout);
                }
                else
                {
                    // This tunnel is good or still in the process
                    // of connecting.
                    return tunnel;
                }
            }
        }

        public void CloseTunnel(IapTunnelEndpoint endpoint)
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
                var copyOfEndpoints = new List<IapTunnelEndpoint>(this.tunnels.Keys);

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

    internal class IapTunnelEndpoint : IEquatable<IapTunnelEndpoint>
    {
        public VmInstanceReference Instance { get; private set; }
        
        public uint RemotePort { get; private set; }

        public IapTunnelEndpoint(VmInstanceReference instance, uint remotePort)
        {
            this.Instance = instance;
            this.RemotePort = remotePort;
        }

        public bool Equals(IapTunnelEndpoint other)
        {
            return
                other != null &&
                other.Instance.Equals(this.Instance) &&
                other.RemotePort == this.RemotePort;
        }

        public override bool Equals(object obj)
        {
            return obj is IapTunnelEndpoint && Equals((IapTunnelEndpoint)obj);
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

    internal class IapTunnel
    {
        private readonly GCloudIapTunnelProcess gcloudProcess;

        private IapTunnel(IapTunnelEndpoint endpoint, int localPort, GCloudIapTunnelProcess gcloudProcess)
        {
            this.Endpoint = endpoint;
            this.LocalPort = localPort;
            this.gcloudProcess = gcloudProcess;
        }

        public IapTunnelEndpoint Endpoint { get; private set; }

        public int LocalPort { get; private set; }

        public int ProcessId => this.gcloudProcess.Id;

        private  IapTunnel(GCloudIapTunnelProcess gcloudProcess, IapTunnelEndpoint endpoint, int localPort)
        {
            this.gcloudProcess = gcloudProcess;
            this.Endpoint = endpoint;
            this.LocalPort = localPort;
        }

        public static async Task<IapTunnel> Open(
            FileInfo gcloudExecutable, 
            IapTunnelEndpoint endpoint, 
            TimeSpan timeout)
        {
            // Find a local port that is not occupied and can be used for tunneling.
            var localPort = PortFinder.FindFreeLocalPort();

            var gcloudProcess = GCloudIapTunnelProcess.Start(
                gcloudExecutable,
                endpoint,
                localPort);

            await gcloudProcess.WaitForPort(timeout);
            return new IapTunnel(gcloudProcess, endpoint, localPort);
        }

        public void Close()
        {
            this.gcloudProcess.Kill();
        }
    }

    internal class GCloudIapTunnelProcess : GcloudProcess
    {
        private readonly int listenPort;
        
        private GCloudIapTunnelProcess(Process wrapperProcess, int listenPort)
            : base(wrapperProcess)
        {
            this.listenPort = listenPort;
        }

        public static GCloudIapTunnelProcess Start(
            FileInfo gcloudExecutable,
            IapTunnelEndpoint endpoint,
            int listenPort)
        {
            var startInfo = GcloudProcess.CreateStartInfo(
                gcloudExecutable,
                "beta compute start-iap-tunnel " +
                    $"{endpoint.Instance.InstanceName} {endpoint.RemotePort} " +
                    $"--local-host-port=localhost:{listenPort} " +
                    $"--project={endpoint.Instance.ProjectId} " +
                    $"--zone={endpoint.Instance.Zone}");

            return new GCloudIapTunnelProcess(
                Process.Start(startInfo), 
                listenPort);
        }

        public Task WaitForPort(TimeSpan timeout)
        {
            return Task.Run(() =>
            {
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    if (!String.IsNullOrWhiteSpace(this.ErrorOutput))
                    {
                        throw new ApplicationException(
                            "gcloud failed to create IAP tunnel",
                            new GCloudCommandException(this.ErrorOutput.ToString()));
                    }

                    if (IPGlobalProperties.GetIPGlobalProperties()
                        .GetActiveTcpListeners()
                        .FirstOrDefault(l => l.Port == this.listenPort) != null)
                    {
                        return;
                    }

                    Thread.Sleep((int)(timeout.TotalMilliseconds / 10));
                }

                throw new TimeoutException(
                    "Timeout waiting for TCP tunnel to be created. Check that "+
                    "the respective VM instance permits RDP ingress traffic from Cloud IAP.");
            });
        }
    }
}
