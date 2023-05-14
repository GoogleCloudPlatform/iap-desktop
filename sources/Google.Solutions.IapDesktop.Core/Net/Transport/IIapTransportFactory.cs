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
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Net.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Net.Transport
{
    /// <summary>
    /// Factory for IAP transports.
    /// </summary>
    public interface IIapTransportFactory
    {
        /// <summary>
        /// Returns the pool of tunnels that the factory
        /// uses to satisfy requests.
        /// </summary>
        IEnumerable<IIapTunnel> Pool { get; }

        /// <summary>
        /// Create a transport to a VM instance/port.
        /// </summary>
        Task<ITransport> CreateIapTransportAsync(
            IProtocol protocol,
            ISshRelayPolicy policy,
            InstanceLocator targetInstance,
            ushort targetPort,
            IPEndPoint localEndpoint,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Tunnel used to provide a transport.
    /// </summary>
    public interface IIapTunnel
    {
        IapTunnelStatistics Statistics { get; }
        IPEndPoint LocalEndpoint { get; }

        IapTunnelFlags Flags { get; }
    }

    public struct IapTunnelStatistics
    {
        public ulong BytesReceived;
        public ulong BytesTransmitted;
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

    /// <summary>
    /// Factory for VPC transports.
    /// </summary>
    public interface IVpcTransportFactory
    { 
        /// <summary>
        /// Create a VPC transport to a VM instance/port.
        /// </summary>
        Task<ITransport> CreateIpTransportAsync(
            IProtocol protocol,
            ISshRelayPolicy policy,
            IPAddress remoteAddress,
            IPAddress targetPort,
            CancellationToken cancellationToken);

        //Task<ITransport> CreateIapSocksTransportAsync(
        //    IProtocol protocol,
        //    ISshRelayPolicy policy,
        //    DestinationGroupLocator destinationGroup,
        //    CancellationToken cancellationToken);
    }
}
