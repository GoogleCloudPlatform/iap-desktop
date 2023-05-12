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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport
{
    /// <summary>
    /// Creates and brokers transport objects, intended to be
    /// used as singleton.
    /// 
    /// Callers must dispose transports when they're done. Once a
    /// transport's reference count drops to zero, it's closed.
    /// 
    /// Disposing force-closes all transports.
    /// </summary>
    public interface ITransportBroker : IDisposable
    {
        /// <summary>
        /// Return a list of all currently active transports.
        /// </summary>
        IEnumerable<ITransport> Active { get; }

        /// <summary>
        /// Create or get a transport to a VM instance/port.
        /// 
        /// IAP transports are always shared.
        /// </summary>
        Task<ITransport> CreateIapTransportAsync(
            IProtocol protocol,
            ISshRelayPolicy policy,
            InstanceLocator instance,
            ushort remotePort,
            IPAddress localAddress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Create or get a VPC transport to a VM instance/port.
        /// 
        /// IAP transports are never shared.
        /// </summary>
        Task<ITransport> CreateIpTransportAsync(
            IProtocol protocol,
            ISshRelayPolicy policy,
            IPAddress remoteAddress,
            IPAddress localAddress,
            CancellationToken cancellationToken);

        //Task<ITransport> CreateIapSocksTransportAsync(
        //    IProtocol protocol,
        //    ISshRelayPolicy policy,
        //    DestinationGroupLocator destinationGroup,
        //    CancellationToken cancellationToken);
    }
}
