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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    public abstract class SessionContextBase<TCredential, TParameters>
        : ISessionContext<TCredential, TParameters>
        where TCredential : ISessionCredential
    {
        private readonly ITunnelBrokerService tunnelBroker;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        protected async Task<ITransport> ConnectTransportAsync(
            Transport.TransportType transportType,
            ushort port,
            TimeSpan connectionTimeout,
            CancellationToken cancellationToken)
        {
            switch (transportType)
            {
                case Transport.TransportType.IapTunnel:
                    return await Transport
                        .CreateIapTransportAsync(
                            this.tunnelBroker,
                            this.Instance,
                            port,
                            connectionTimeout)
                        .ConfigureAwait(false);

                case Transport.TransportType.Vpc:
                    return await Transport
                        .CreateVpcTransportAsync(
                            this.computeEngineAdapter,
                            this.Instance,
                            port,
                            cancellationToken)
                        .ConfigureAwait(false);

                default:
                    throw new ArgumentException("Unrecognized transport type");
            }
        }

        protected SessionContextBase(
            ITunnelBrokerService tunnelBroker,
            IComputeEngineAdapter computeEngineAdapter,
            InstanceLocator instance,
            TParameters parameters)
        {
            this.tunnelBroker = tunnelBroker.ExpectNotNull(nameof(tunnelBroker));
            this.computeEngineAdapter = computeEngineAdapter.ExpectNotNull(nameof(computeEngineAdapter));
            this.Instance = instance.ExpectNotNull(nameof(instance));
            this.Parameters = parameters.ExpectNotNull(nameof(parameters));
        }

        //---------------------------------------------------------------------
        // ISessionContext.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; }

        public TParameters Parameters { get; }

        public abstract Task<TCredential> AuthorizeCredentialAsync(
            CancellationToken cancellationToken);

        public abstract Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken);

        public virtual void Dispose()
        {
        }
    }
}
