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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    
    internal sealed class RdpSessionContext : ISessionContext<RdpCredential, RdpSessionParameters>
    {
        private readonly ITunnelBrokerService tunnelBroker;

        internal RdpSessionContext(
            ITunnelBrokerService tunnelBroker,
            InstanceLocator instance,
            RdpCredential credential,
            RdpSessionParameters.ParameterSources sources)
        {
            this.tunnelBroker = tunnelBroker.ExpectNotNull(nameof(tunnelBroker));
            this.Instance = instance.ExpectNotNull(nameof(instance));
            this.Credential = credential.ExpectNotNull(nameof(credential));
            this.Parameters = new RdpSessionParameters()
            {
                Sources = sources
            };
        }

        public RdpCredential Credential { get; }

        //---------------------------------------------------------------------
        // ISessionContext.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; }

        public RdpSessionParameters Parameters { get; }

        public Task<RdpCredential> AuthorizeCredentialAsync(CancellationToken cancellationToken)
        {
            //
            // RDP credentials are ready to go.
            //
            return Task.FromResult(this.Credential);
        }

        public async Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken)
        {
            return await Transport
                .CreateIapTransportAsync(
                    this.tunnelBroker,
                    this.Instance,
                    this.Parameters.Port,
                    this.Parameters.ConnectionTimeout)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
        }
    }
}