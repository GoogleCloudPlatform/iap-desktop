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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.Testing.Common.Integration;
using System;
using System.Net;
using System.Threading;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test
{
    internal class IapTunnel : IDisposable, ITransport
    {
        private readonly SshRelayListener listener;
        private readonly CancellationTokenSource tokenSource;

        public int LocalPort => listener.LocalPort;

        public Transport.TransportType Type => Transport.TransportType.IapTunnel;

        public IPEndPoint Endpoint => new IPEndPoint(IPAddress.Loopback, listener.LocalPort);

        public InstanceLocator Instance { get; }

        private IapTunnel(
            InstanceLocator instance,
            SshRelayListener listener, 
            CancellationTokenSource tokenSource)
        {
            this.Instance = instance;
            this.listener = listener;
            this.tokenSource = tokenSource;
        }

        public static IapTunnel ForRdp(InstanceLocator instance, ICredential credential)
        {
            var listener = SshRelayListener.CreateLocalListener(
                new IapTunnelingEndpoint(
                    credential,
                    instance,
                    3389,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent),
                new AllowAllRelayPolicy());

            var tokenSource = new CancellationTokenSource();
            listener.ListenAsync(tokenSource.Token);

            return new IapTunnel(instance, listener, tokenSource);
        }

        public void Dispose()
        {
            this.tokenSource.Cancel();
        }
    }
}
