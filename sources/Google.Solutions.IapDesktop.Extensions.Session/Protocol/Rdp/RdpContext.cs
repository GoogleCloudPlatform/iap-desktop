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
using Google.Solutions.Common;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.Platform.Net;
using System.Collections.Generic;
using System.Management;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp
{

    /// <summary>
    /// Encapsulates settings and logic to create an RDP session.
    /// </summary>
    internal sealed class RdpContext
        : SessionContextBase<RdpCredential, RdpParameters>
    {
        internal RdpContext(
            IIapTransportFactory iapTransportFactory,
            IDirectTransportFactory directTransportFactory,
            InstanceLocator instance,
            RdpCredential credential,
            RdpParameters.ParameterSources sources)
            : base(
                  iapTransportFactory,
                  directTransportFactory,
                  instance,
                  new RdpParameters()
                  {
                      Sources = sources
                  })
        {
            this.Credential = credential.ExpectNotNull(nameof(credential));
        }

        public RdpCredential Credential { get; }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override Task<RdpCredential> AuthorizeCredentialAsync(
            CancellationToken cancellationToken)
        {
            //
            // RDP credentials are ready to go.
            //

            var isProbablyDomainCredential = new NetworkCredential(
                this.Credential.User,
                string.Empty,
                this.Credential.Domain).IsDomainOrHostQualified();

            TelemetryLog.Current.Write(
                "session_rdp_auth",
                new Dictionary<string, object>
                {
                    {
                        "rdpct", // Credential type
                        isProbablyDomainCredential ? "Domain" : "Local"
                    }
                });

            return Task.FromResult(this.Credential);
        }

        public override Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken)
        {
            return ConnectTransportAsync(
                RdpProtocol.Protocol,
                this.Parameters.TransportType,
                this.Parameters.Port,
                this.Parameters.ConnectionTimeout,
                cancellationToken);
        }
    }
}