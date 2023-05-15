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

using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Core.Net.Protocol;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Net.Transport
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
        private readonly ISshRelayListener listener;
        private readonly Task listenTask;

        internal event EventHandler Closed;

        public Profile Details { get; }

        internal IapTunnel(
            ISshRelayListener listener,
            Profile profile,
            IapTunnelFlags flags)
        {
            this.listener = listener.ExpectNotNull(nameof(listener));
            this.stopListenerSource = new CancellationTokenSource();
            this.listenTask = this.listener.ListenAsync(this.stopListenerSource.Token);
            this.Details = profile;
            this.Flags = flags;

            Debug.Assert(profile.LocalEndpoint.Port == listener.LocalPort);
        }

        internal Task CloseAsync()
        {
            this.stopListenerSource.Cancel();

            this.Closed?.Invoke(this, EventArgs.Empty);

            return this.listenTask;
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

        public IPEndPoint LocalEndpoint => this.Details.LocalEndpoint;

        public IapTunnelFlags Flags { get; }

        public IapTunnelStatistics Statistics
        {
            get
            {
                var stats = this.listener.Statistics;
                return new IapTunnelStatistics()
                {
                    BytesReceived = stats.BytesReceived,
                    BytesTransmitted = stats.BytesTransmitted
                };
            }
        }

        //-----------------------------------------------------------------
        // Inner classes.
        //-----------------------------------------------------------------

        /// <summary>
        /// Defines all the parameters that define a tunnel. If the profile
        /// is the same, a tunnel can be shared.
        /// </summary>
        public class Profile : IEquatable<Profile>
        {
            public IProtocol Protocol { get; }
            public ISshRelayPolicy Policy { get; }
            public InstanceLocator TargetInstance { get; }
            public ushort TargetPort { get; }
            public IPEndPoint LocalEndpoint { get; }

            internal Profile(
                IProtocol protocol,
                ISshRelayPolicy policy,
                InstanceLocator targetInstance,
                ushort targetPort,
                IPEndPoint localEndpoint)
            {
                this.Policy = policy.ExpectNotNull(nameof(policy));
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.TargetInstance = targetInstance.ExpectNotNull(nameof(targetInstance));
                this.TargetPort = targetPort;
                this.LocalEndpoint = localEndpoint.ExpectNotNull(nameof(localEndpoint));
            }

            public override int GetHashCode()
            {
                return
                    this.Policy.Id.GetHashCode() ^
                    this.Protocol.Id.GetHashCode() ^
                    this.TargetInstance.GetHashCode() ^
                    this.TargetPort ^
                    this.LocalEndpoint.GetHashCode();
            }

            public bool Equals(Profile other)
            {
                return other != null &&
                    Equals(this.Policy.Id, other.Policy.Id) &&
                    Equals(this.Protocol.Id, other.Protocol.Id) &&
                    Equals(this.TargetInstance, other.TargetInstance) &&
                    this.TargetPort == other.TargetPort &&
                    Equals(this.LocalEndpoint, other.LocalEndpoint);
            }

            public override bool Equals(object obj)
            {
                return Equals((Profile)obj);
            }

            public static bool operator ==(Profile obj1, Profile obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(Profile obj1, Profile obj2)
            {
                return !(obj1 == obj2);
            }

            public override string ToString()
            {
                return $"{this.TargetInstance}, port: {this.TargetPort}, " +
                       $"protocol: {this.Protocol.Id}, policy: {this.Policy.Id}";
            }
        }

        /// <summary>
        /// Factory for tunnels. Can be derived/overridden in unit tests.
        /// </summary>
        public class Factory
        {
            public async virtual Task<IapTunnel> CreateTunnelAsync( // TODO: Add Integration test, double-check/compare logic
                IAuthorization authorization,
                UserAgent userAgent,
                Profile profile,
                TimeSpan probeTimeout,
                CancellationToken cancellationToken)
            {
                using (CoreTraceSources.Default.TraceMethod().WithParameters(profile))
                {
                    if (profile.LocalEndpoint.Address != IPAddress.Loopback)
                    {
                        throw new NotImplementedException(
                            "This implementation only supports loopback tunnels");
                    }

                    var clientCertificate =
                            (authorization.DeviceEnrollment != null &&
                            authorization.DeviceEnrollment.State == DeviceEnrollmentState.Enrolled)
                        ? authorization.DeviceEnrollment.Certificate
                        : null;

                    if (clientCertificate != null)
                    {
                        CoreTraceSources.Default.TraceInformation(
                            "Using client certificate (valid till {0})", clientCertificate.NotAfter);
                    }

                    var client = new IapTunnelingEndpoint(
                        authorization.Credential,
                        profile.TargetInstance,
                        profile.TargetPort,
                        IapTunnelingEndpoint.DefaultNetworkInterface,
                        userAgent,
                        clientCertificate);

                    //
                    // Check if we can actually connect to this instance before we
                    // start a local listener.
                    //
                    using (var stream = new SshRelayStream(client))
                    {
                        await stream
                            .ProbeConnectionAsync(probeTimeout) // TODO: Add Integration test -> don't pool if probe fails
                            .ConfigureAwait(false);
                    }

                    var listener = SshRelayListener.CreateLocalListener(
                        client,
                        profile.Policy,
                        profile.LocalEndpoint.Port);

                    var tunnel = new IapTunnel(
                        listener,
                        profile,
                        clientCertificate != null ? IapTunnelFlags.Mtls : IapTunnelFlags.None);

                    return tunnel;
                }
            }
        }
    }
}
