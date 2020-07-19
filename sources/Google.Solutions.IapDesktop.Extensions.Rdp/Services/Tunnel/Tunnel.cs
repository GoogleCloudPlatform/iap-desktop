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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapTunneling.Iap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Tunnel
{
    public interface ITunnel
    {
        TunnelDestination Destination { get; }
        int LocalPort { get; }
        Task Probe(TimeSpan timeout);
        void Close();
    }

    public class Tunnel : ITunnel
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly SshRelayListener listener;
        private readonly IapTunnelingEndpoint endpoint;

        public TunnelDestination Destination => new TunnelDestination(
            this.Endpoint.VmInstance, this.Endpoint.Port);

        public virtual int LocalPort => listener.LocalPort;

        public IapTunnelingEndpoint Endpoint => endpoint;

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

        public virtual async Task Probe(TimeSpan timeout)
        {
            // Probe connection to fail fast if there is an 'access denied'
            // issue.
            using (var stream = new SshRelayStream(this.Endpoint))
            {
                await stream.TestConnectionAsync(timeout).ConfigureAwait(false);
            }
        }
    }

    public class TunnelDestination : IEquatable<TunnelDestination>
    {
        public InstanceLocator Instance { get; private set; }

        public ushort RemotePort { get; private set; }

        public TunnelDestination(InstanceLocator instance, ushort remotePort)
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
            return $"{this.Instance.Name}:{this.RemotePort}";
        }
    }
}
