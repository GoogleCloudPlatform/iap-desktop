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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.Platform.Dispatch;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    public class AppProtocolContext : IProtocolContext
    {
        private readonly AppProtocol protocol;
        private readonly InstanceLocator target;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;

        /// <summary>
        /// Network credential to use for launching the client.
        /// </summary>
        public NetworkCredential NetworkCredential { get; set;  }

        /// <summary>
        /// Timeout for connecting transport.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public AppProtocolContext(
            AppProtocol protocol,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            InstanceLocator target)
        {
            this.protocol = protocol.ExpectNotNull(nameof(protocol));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.target = target.ExpectNotNull(nameof(target));
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public bool CanLaunchClient
        {
            get => this.protocol.Client != null && this.protocol.Client.IsAvailable; // TODO: Add test
        }

        public IWin32Process LaunchClient(ITransport transport)
        {
            var client = this.protocol.Client;

            if (this.NetworkCredential != null)
            {
                return this.processFactory.CreateProcessAsUser(
                    client.Executable,
                    client.FormatArguments(transport),
                    LogonFlags.NetCredentialsOnly,
                    this.NetworkCredential);
            }
            else
            {
                return this.processFactory.CreateProcess(
                    client.Executable,
                    client.FormatArguments(transport));
            }
        }

        //---------------------------------------------------------------------
        // IProtocolContext.
        //---------------------------------------------------------------------

        public Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken)
        {
            return this.transportFactory.CreateTransportAsync(
                this.protocol,
                this.protocol.Policy,
                this.target,
                this.protocol.RemotePort,
                this.protocol.LocalEndpoint,
                this.ConnectionTimeout,
                cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}
