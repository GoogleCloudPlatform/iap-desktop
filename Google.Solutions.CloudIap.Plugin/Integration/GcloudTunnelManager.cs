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
using Google.Solutions.Compute.Test.Net;
using System;
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
    internal class GcloudTunnelManager : TunnelManagerBase
    {
        private readonly PluginConfigurationStore configurationStore;

        public GcloudTunnelManager(PluginConfigurationStore configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        protected override Task<ITunnel> CreateTunnelAsync(TunnelDestination endpoint, TimeSpan timeout)
        {
            // Fetch latest configuration.
            var configuration = configurationStore.Configuration;

            if (string.IsNullOrEmpty(configuration.GcloudCommandPath) ||
                !File.Exists(configuration.GcloudCommandPath) ||
                !configuration.GcloudCommandPath.EndsWith("gcloud.cmd", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException(
                    "Cloud SDK not found. \n\n" +
                    "Use the settings dialog to configure the path to 'gcloud.cmd'.");
            }

            return GcloudTunnel.Open(
                new FileInfo(configuration.GcloudCommandPath),
                endpoint,
                configuration.IapConnectionTimeout);
        }
    }
    

    internal class GcloudTunnel : ITunnel
    {
        private readonly GcloudTunnelProcess gcloudProcess;

        private GcloudTunnel(
            TunnelDestination endpoint, 
            int localPort, 
            GcloudTunnelProcess gcloudProcess)
        {
            this.Endpoint = endpoint;
            this.LocalPort = localPort;
            this.gcloudProcess = gcloudProcess;
        }

        public TunnelDestination Endpoint { get; private set; }

        public int LocalPort { get; private set; }

        public int? ProcessId => this.gcloudProcess.Id;

        private GcloudTunnel(
            GcloudTunnelProcess gcloudProcess, 
            TunnelDestination endpoint, 
            int localPort)
        {
            this.gcloudProcess = gcloudProcess;
            this.Endpoint = endpoint;
            this.LocalPort = localPort;
        }

        public static async Task<ITunnel> Open(
            FileInfo gcloudExecutable, 
            TunnelDestination endpoint, 
            TimeSpan timeout)
        {
            // Find a local port that is not occupied and can be used for tunneling.
            var localPort = PortFinder.FindFreeLocalPort();

            var gcloudProcess = new GcloudTunnelProcess(
                gcloudExecutable,
                endpoint,
                localPort);

            await gcloudProcess.WaitForPort(timeout);
            return new GcloudTunnel(gcloudProcess, endpoint, localPort);
        }

        public void Close()
        {
            this.gcloudProcess.Kill();
        }

        public Task Probe(TimeSpan timeout)
        {
            return Task.FromResult<int>(0);
        }
    }

    internal class GcloudTunnelProcess : GcloudProcess
    {
        private readonly int listenPort;
        
        public GcloudTunnelProcess(
            FileInfo gcloudExecutable,
            TunnelDestination endpoint,
            int listenPort) : base(
                gcloudExecutable,
                CreateArguments(endpoint, listenPort))
        {
            this.listenPort = listenPort;
        }

        private static string CreateArguments(TunnelDestination endpoint, int listenPort)
        {
            return "compute start-iap-tunnel " +
                    $"{endpoint.Instance.InstanceName} {endpoint.RemotePort} " +
                    $"--local-host-port=localhost:{listenPort} " +
                    $"--project={endpoint.Instance.ProjectId} " +
                    $"--zone={endpoint.Instance.Zone}";
        }

        public Task WaitForPort(TimeSpan timeout)
        {
            return Task.Run(() =>
            {
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    if (this.ErrorOutput != null && this.ErrorOutput.Contains("alternate release tracks"))
                    {
                        throw new ApplicationException(
                            "The Cloud SDK installed on this machine is outdated. \n\n"+
                            "Please run 'gcloud components update' in an elevated command prompt and try again.");
                    }
                    else if (!String.IsNullOrWhiteSpace(this.ErrorOutput))
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
