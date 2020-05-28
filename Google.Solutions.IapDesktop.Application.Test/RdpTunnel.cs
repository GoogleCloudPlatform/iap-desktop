﻿//
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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.IapTunneling.Iap;
using System;
using System.Threading;

namespace Google.Solutions.IapDesktop.Application.Test
{
    internal class RdpTunnel : IDisposable
    {
        private readonly SshRelayListener listener;
        private readonly CancellationTokenSource tokenSource;

        public int LocalPort => listener.LocalPort;

        public void Dispose()
        {
            this.tokenSource.Cancel();
        }

        private RdpTunnel(SshRelayListener listener, CancellationTokenSource tokenSource)
        {
            this.listener = listener;
            this.tokenSource = tokenSource;
        }

        public static RdpTunnel Create(InstanceLocator vmRef)
        {
            var listener = SshRelayListener.CreateLocalListener(
                new IapTunnelingEndpoint(
                    Defaults.GetCredential(),
                    vmRef,
                    3389,
                    IapTunnelingEndpoint.DefaultNetworkInterface));

            var tokenSource = new CancellationTokenSource();
            listener.ListenAsync(tokenSource.Token);

            return new RdpTunnel(listener, tokenSource);
        }
    }
}
