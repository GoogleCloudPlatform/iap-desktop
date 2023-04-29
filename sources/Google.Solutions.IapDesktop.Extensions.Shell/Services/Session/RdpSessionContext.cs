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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    internal class RdpSessionContext : ISessionContext<RdpCredential>
    {
        private readonly ITunnelBrokerService tunnelBroker;


        //---------------------------------------------------------------------
        // ISessionContext.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; }


        public bool AllowPersistentCredentials { get; set; }

        public Task<RdpCredential> CreateCredentialAsync(CancellationToken cancellationToken)
        {
            // TODO: XX - can't prompt from within a job! Do in factory?
            throw new System.NotImplementedException();
        }

        public Task<Transport> CreateTransportAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
            //return Transport.CreateIapTunnelAsync(
            //    this.tunnelBroker,
            //    this.Instance,
            //    ...
            //    )
        }
        // all settings
    }
}
