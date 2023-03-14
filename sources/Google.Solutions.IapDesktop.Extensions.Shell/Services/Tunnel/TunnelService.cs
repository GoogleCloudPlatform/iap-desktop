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

using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapTunneling.Iap;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel
{
    public interface ITunnelService
    {
        Task<ITunnel> CreateTunnelAsync(
            TunnelDestination tunnelEndpoint,
            ISshRelayPolicy relayPolicy);
    }

    [Service(typeof(ITunnelService))]
    public class TunnelService : ITunnelService
    {
        private readonly IAuthorization authorization;

        public TunnelService(IAuthorization authorization)
        {
            this.authorization = authorization.ThrowIfNull(nameof(authorization));
        }

        public Task<ITunnel> CreateTunnelAsync(
            TunnelDestination tunnelEndpoint,
            ISshRelayPolicy relayPolicy)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(tunnelEndpoint))
            {
                var clientCertificate =
                        (this.authorization.DeviceEnrollment != null &&
                        this.authorization.DeviceEnrollment.State == DeviceEnrollmentState.Enrolled)
                    ? this.authorization.DeviceEnrollment.Certificate
                    : null;

                if (clientCertificate != null)
                {
                    ApplicationTraceSources.Default.TraceInformation(
                        "Using client certificate (valid till {0})", clientCertificate.NotAfter);
                }

                var iapEndpoint = new IapTunnelingEndpoint(
                    this.authorization.Credential,
                    tunnelEndpoint.Instance,
                    tunnelEndpoint.RemotePort,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    Install.UserAgent,
                    clientCertificate);

                // Start listener to enable clients to connect. Do not await
                // the listener as we want to continue listeining in the
                // background.
                var listener = SshRelayListener.CreateLocalListener(
                    iapEndpoint,
                    relayPolicy);
                var cts = new CancellationTokenSource();

                _ = listener.ListenAsync(cts.Token);

                // Return the tunnel which allows the listener to be stopped
                // via the CancellationTokenSource.
                return Task.FromResult<ITunnel>(new Tunnel(iapEndpoint, listener, cts));
            }
        }
    }
}
