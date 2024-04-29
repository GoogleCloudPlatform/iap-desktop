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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol
{
    public abstract class SessionContextBase<TCredential, TParameters>
        : ISessionContext<TCredential, TParameters>
    {
        private readonly IIapTransportFactory iapTransportFactory;
        private readonly IDirectTransportFactory directTransportFactory;

        protected async Task<ITransport> ConnectTransportAsync(
            IProtocol protocol,
            SessionTransportType transportType,
            ushort port,
            TimeSpan connectionTimeout,
            CancellationToken cancellationToken)
        {
            return transportType switch
            {
                SessionTransportType.IapTunnel => await this.iapTransportFactory
                    .CreateTransportAsync(
                        protocol,
                        new CurrentProcessPolicy(),
                        this.Instance,
                        port,
                        null, // Auto-assign
                        connectionTimeout,
                        cancellationToken)
                    .ConfigureAwait(false),

                SessionTransportType.Vpc => await this.directTransportFactory
                    .CreateTransportAsync(
                        protocol,
                        this.Instance,
                        NetworkInterfaceType.PrimaryInternal,
                        port,
                        cancellationToken)
                    .ConfigureAwait(false),

                _ => throw new ArgumentException("Unrecognized transport type"),
            };
        }

        protected SessionContextBase(
            IIapTransportFactory iapTransportFactory,
            IDirectTransportFactory directTransportFactory,
            InstanceLocator instance,
            TParameters parameters)
        {
            this.iapTransportFactory = iapTransportFactory.ExpectNotNull(nameof(SessionContextBase<TCredential, TParameters>.iapTransportFactory));
            this.directTransportFactory = directTransportFactory.ExpectNotNull(nameof(directTransportFactory));
            this.Instance = instance.ExpectNotNull(nameof(instance));
            this.Parameters = parameters;
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
