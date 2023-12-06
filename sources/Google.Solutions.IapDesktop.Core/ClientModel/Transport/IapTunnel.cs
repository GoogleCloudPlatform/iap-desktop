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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Transport
{
    /// <summary>
    /// An IAP-based tunnel that can be used to implement transports.
    /// </summary>
    public interface IIapTunnel
    {
        /// <summary>
        /// Traffic statistics.
        /// </summary>
        IapTunnelStatistics Statistics { get; }

        /// <summary>
        /// Local endpoints for sessions/clients to connect to.
        /// </summary>
        IPEndPoint LocalEndpoint { get; }

        /// <summary>
        /// Flags characterizing this tunnel.
        /// </summary>
        IapTunnelFlags Flags { get; }

        /// <summary>
        /// Target instance.
        /// </summary>
        InstanceLocator TargetInstance { get; }

        /// <summary>
        /// Target port.
        /// </summary>
        ushort TargetPort { get; }

        /// <summary>
        /// Policy that controls which remote peers are allowed to 
        /// connect to the listener.
        /// </summary>
        ITransportPolicy Policy { get; }

        /// <summary>
        /// Protocol that this transport is used for.
        /// </summary>
        IProtocol Protocol { get; }
    }

    [Flags]
    public enum IapTunnelFlags
    {
        None,

        /// <summary>
        /// Transport is using mTLS.
        /// </summary>
        Mtls
    }

    public struct IapTunnelStatistics
    {
        public ulong BytesReceived;
        public ulong BytesTransmitted;
    }

    //-------------------------------------------------------------------------
    // Implementation.
    //-------------------------------------------------------------------------

    /// <summary>
    /// A sharable tunnel that uses an IAP relay listener.
    /// </summary>
    public class IapTunnel : ReferenceCountedDisposableBase, IIapTunnel
    {
        private readonly CancellationTokenSource stopListenerSource;
        private readonly IIapListener listener;
        private readonly Task listenTask;

        internal event EventHandler? Closed;

        internal Profile Details { get; }

        internal IapTunnel(
            IIapListener listener,
            Profile profile,
            IapTunnelFlags flags)
        {
            this.listener = listener.ExpectNotNull(nameof(listener));
            this.stopListenerSource = new CancellationTokenSource();
            this.listenTask = this.listener.ListenAsync(this.stopListenerSource.Token);
            this.Details = profile;
            this.Flags = flags;

            Debug.Assert(
                profile.LocalEndpoint == null || // Auto-assigned
                Equals(profile.LocalEndpoint, listener.LocalEndpoint));
        }

        internal Task CloseAsync()
        {
            this.stopListenerSource.Cancel();

            this.Closed?.Invoke(this, EventArgs.Empty);

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            return this.listenTask;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //
                // Last reference is gone, stop the listener.
                //
                _ = CloseAsync();
            }

            base.Dispose(disposing);
        }

        //-----------------------------------------------------------------
        // IIapTunnel.
        //-----------------------------------------------------------------

        public IPEndPoint LocalEndpoint => this.listener.LocalEndpoint;

        public IapTunnelFlags Flags { get; }

        public IapTunnelStatistics Statistics
        {
            get
            {
                var stats = this.listener.Statistics;
                return new IapTunnelStatistics()
                {
                    //
                    // NB. If we send something, the listener receives it,
                    // so the listener's statistics are reversed.
                    //
                    BytesReceived = stats.BytesTransmitted,
                    BytesTransmitted = stats.BytesReceived
                };
            }
        }

        public InstanceLocator TargetInstance => this.Details.TargetInstance;
        public ushort TargetPort => this.Details.TargetPort;

        public ITransportPolicy Policy => this.Details.Policy;

        public IProtocol Protocol => this.Details.Protocol;

        //-----------------------------------------------------------------
        // Inner classes.
        //-----------------------------------------------------------------

        /// <summary>
        /// Defines all the parameters that define a tunnel. If the profile
        /// is the same, a tunnel can be shared.
        /// </summary>
        public class Profile : IEquatable<Profile>
        {
            internal IProtocol Protocol { get; }
            internal ITransportPolicy Policy { get; }

            /// <summary>
            /// Instance to connect to.
            /// </summary>
            public InstanceLocator TargetInstance { get; }

            /// <summary>
            /// Port to connect to.
            /// </summary>
            public ushort TargetPort { get; }

            /// <summary>
            /// Custom local endpoint. If null, an endpoint is assigned
            /// automatically.
            /// </summary>
            public IPEndPoint? LocalEndpoint { get; }

            internal Profile(
                IProtocol protocol,
                ITransportPolicy policy,
                InstanceLocator targetInstance,
                ushort targetPort,
                IPEndPoint? localEndpoint = null)
            {
                this.Policy = policy.ExpectNotNull(nameof(policy));
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.TargetInstance = targetInstance.ExpectNotNull(nameof(targetInstance));
                this.TargetPort = targetPort;
                this.LocalEndpoint = localEndpoint; // Optional.
            }

            public override int GetHashCode()
            {
                return
                    this.Policy.GetHashCode() ^
                    this.Protocol.GetHashCode() ^
                    this.TargetInstance.GetHashCode() ^
                    this.TargetPort ^
                    (this.LocalEndpoint?.GetHashCode() ?? 0);
            }

            public bool Equals(Profile? other)
            {
                return other != null &&
                    Equals(this.Policy, other.Policy) &&
                    Equals(this.Protocol, other.Protocol) &&
                    Equals(this.TargetInstance, other.TargetInstance) &&
                    this.TargetPort == other.TargetPort &&
                    Equals(this.LocalEndpoint, other.LocalEndpoint);
            }

            public override bool Equals(object obj)
            {
                return Equals((Profile)obj);
            }

            public static bool operator ==(Profile? obj1, Profile? obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(Profile? obj1, Profile? obj2)
            {
                return !(obj1 == obj2);
            }

            public override string ToString()
            {
                return $"{this.TargetInstance}, port: {this.TargetPort}, " +
                       $"protocol: {this.Protocol.Name}, policy: {this.Policy.Name}";
            }
        }

        /// <summary>
        /// Factory for tunnels. Can be derived/overridden in unit tests.
        /// </summary>
        public class Factory
        {
            private readonly IIapClient client;

            public Factory(IIapClient client)
            {
                this.client = client.ExpectNotNull(nameof(client));
            }

            public virtual async Task<IapTunnel> CreateTunnelAsync(
                Profile profile,
                TimeSpan probeTimeout,
                CancellationToken cancellationToken)
            {
                using (CoreTraceSource.Log.TraceMethod().WithParameters(profile))
                {
                    if (profile.LocalEndpoint != null &&
                        profile.LocalEndpoint.Address != IPAddress.Loopback)
                    {
                        throw new ArgumentException(
                            "This implementation only supports loopback tunnels");
                    }

                    var target = this.client.GetTarget(
                        profile.TargetInstance,
                        profile.TargetPort,
                        IapClient.DefaultNetworkInterface);

                    //
                    // Check if we can actually connect to this instance before we
                    // start a local listener.
                    //
                    await target
                        .ProbeAsync(probeTimeout)
                        .ConfigureAwait(false);

                    var localEndpoint = profile.LocalEndpoint;
                    if (localEndpoint == null)
                    {
                        //
                        // Try to use the same port number every time. For
                        // client apps, this helps avoid polluting their
                        // connection history and possibly to save credentials.
                        // 
                        var portFinder = new PortFinder();
                        portFinder.AddSeed(Encoding.ASCII.GetBytes(profile.TargetInstance.ProjectId));
                        portFinder.AddSeed(Encoding.ASCII.GetBytes(profile.TargetInstance.Zone));
                        portFinder.AddSeed(Encoding.ASCII.GetBytes(profile.TargetInstance.Name));
                        portFinder.AddSeed(BitConverter.GetBytes(profile.TargetPort));

                        localEndpoint = new IPEndPoint(
                            IPAddress.Loopback,
                            portFinder.FindPort(out var _));
                    }

                    var listener = new IapListener(
                        target,
                        profile.Policy,
                        localEndpoint);

                    var tunnel = new IapTunnel(
                        listener,
                        profile,
                        target.IsMutualTlsEnabled ? IapTunnelFlags.Mtls : IapTunnelFlags.None);

                    return tunnel;
                }
            }
        }
    }
}
