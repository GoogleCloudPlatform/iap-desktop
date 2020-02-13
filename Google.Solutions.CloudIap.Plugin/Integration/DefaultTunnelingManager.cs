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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Compute.Iap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    internal class DefaultTunnelingManager : TunnelManagerBase
    {
        private readonly ICredential credential;

        public DefaultTunnelingManager(ICredential credential)
        {
            this.credential = credential;
        }

        protected override Task<ITunnel> CreateTunnelAsync(
            TunnelDestination tunnelEndpoint, 
            TimeSpan timeout)
        {
            var iapEndpoint = new IapTunnelingEndpoint(
                this.credential,
                tunnelEndpoint.Instance,
                tunnelEndpoint.RemotePort,
                IapTunnelingEndpoint.DefaultNetworkInterface);


            // Start listener to enable clients to connect. Do not await
            // the listener as we want to continue listeining in the
            // background.
            var listener = SshRelayListener.CreateLocalListener(iapEndpoint);
            var cts = new CancellationTokenSource();

            _ = listener.ListenAsync(cts.Token);

            // Return the tunnel which allows the listener to be stopped
            // via the CancellationTokenSource.
            return Task.FromResult<ITunnel>(new Tunnel(iapEndpoint, listener, cts));
        }

        internal class Tunnel : ITunnel
        {
            private readonly CancellationTokenSource cancellationTokenSource;
            private readonly SshRelayListener listener;
            private readonly IapTunnelingEndpoint endpoint;

            public TunnelDestination Endpoint => new TunnelDestination(
                this.endpoint.VmInstance, this.endpoint.Port);

            public int LocalPort => listener.LocalPort;
            public int? ProcessId => null;

            public Tunnel(
                IapTunnelingEndpoint endpoint, 
                SshRelayListener listener,
                CancellationTokenSource cancellationTokenSource)
            {
                this.endpoint = endpoint;
                this.listener = listener;
                this.cancellationTokenSource = cancellationTokenSource;
            }

            public void Close()
            {
                this.cancellationTokenSource.Cancel();
            }

            public async Task Probe(TimeSpan timeout)
            {
                // Probe connection to fail fast if there is an 'access denied'
                // issue.
                using (var stream = new SshRelayStream(this.endpoint))
                {
                    await stream.TestConnectionAsync(timeout).ConfigureAwait(false);
                }
            }
        }
    }
}
