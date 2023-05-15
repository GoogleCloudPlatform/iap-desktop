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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap.Protocol;
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
            IPEndPoint remoteEndpoint,
            CancellationToken cancellationToken);
    }

    public class DirectTransportFactory : IDirectTransportFactory
    {
        public Task<ITransport> CreateTransportAsync(
            IProtocol protocol,
            IPEndPoint remoteEndpoint,
            CancellationToken cancellationToken)
        {
            using (CoreTraceSources.Default.TraceMethod().WithParameters(remoteEndpoint))
            {
                var transport = new Transport(protocol, remoteEndpoint);
                return Task.FromResult((ITransport)transport);
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Transport : DisposableBase, ITransport
        {
            public Transport(IProtocol protocol, IPEndPoint endpoint)
            {
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.Endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            }

            //-----------------------------------------------------------------
            // ITransport.
            //-----------------------------------------------------------------

            public IProtocol Protocol { get; }

            public IPEndPoint Endpoint { get; }
        }
    }
}
