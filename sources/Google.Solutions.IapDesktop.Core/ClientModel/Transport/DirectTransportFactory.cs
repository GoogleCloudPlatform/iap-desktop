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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Transport
{
    /// <summary>
    /// Factory for direct (i.e., non-IAP based) transports.
    /// </summary>
    public interface IDirectTransportFactory
    {
        /// <summary>
        /// Create a VPC transport to a VM instance/port.
        /// </summary>
        Task<ITransport> CreateTransportAsync(
            IProtocol protocol,
            InstanceLocator instance,
            NetworkInterfaceType type,
            ushort port,
            CancellationToken cancellationToken);
    }

    public class DirectTransportFactory : IDirectTransportFactory
    {
        private readonly IAddressResolver addressResolver;

        public DirectTransportFactory(IAddressResolver addressResolver)
        {
            this.addressResolver = addressResolver.ExpectNotNull(nameof(addressResolver));
        }

        //---------------------------------------------------------------------
        // IDirectTransportFactory.
        //---------------------------------------------------------------------

        public async Task<ITransport> CreateTransportAsync(
            IProtocol protocol,
            InstanceLocator instance,
            NetworkInterfaceType type,
            ushort port,
            CancellationToken cancellationToken)
        {
            instance.ExpectNotNull(nameof(instance));

            using (CoreTraceSource.Log.TraceMethod().WithParameters(instance, type))
            {
                var address = await this.addressResolver
                    .GetAddressAsync(
                        instance,
                        type,
                        cancellationToken)
                    .ConfigureAwait(false);

                return new Transport(
                    protocol,
                    new IPEndPoint(address, port),
                    instance);
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Transport : DisposableBase, ITransport
        {
            public Transport(
                IProtocol protocol,
                IPEndPoint endpoint,
                InstanceLocator target)
            {
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.Endpoint = endpoint.ExpectNotNull(nameof(endpoint));
                this.Target = target.ExpectNotNull(nameof(target));
            }

            //-----------------------------------------------------------------
            // ITransport.
            //-----------------------------------------------------------------

            public IProtocol Protocol { get; }

            public IPEndPoint Endpoint { get; }

            public ResourceLocator Target { get; }
        }
    }
}
