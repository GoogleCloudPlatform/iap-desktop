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

using Google.Solutions.Common.Util;
using Google.Solutions.Iap.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport.Protocols
{
    public class ClientProtocol : IProtocol
    {
        public ClientProtocol(
            string name,
            IEnumerable<ITransportTargetTrait> requiredTraits,
            ISshRelayPolicy relayPolicy,
            ushort remotePort,
            IPAddress localPort,
            string launchCommand)
        {
            this.Name = name.ExpectNotNull(nameof(name));
            this.RequiredTraits = requiredTraits.ExpectNotNull(nameof(requiredTraits));
            this.RelayPolicy = relayPolicy.ExpectNotNull(nameof(relayPolicy));
            this.RemotePort = remotePort.ExpectNotNull(nameof(remotePort));
            this.LocalPort = localPort;
            this.LaunchCommand = launchCommand;
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Traits that a target has to have to use this protocol.
        /// </summary>
        public IEnumerable<ITransportTargetTrait> RequiredTraits { get; }

        /// <summary>
        /// Relay policy that defines who can connect to the local port.
        /// </summary>
        public ISshRelayPolicy RelayPolicy { get; }

        /// <summary>
        /// Port to connect transport to.
        /// </summary>
        public ushort RemotePort { get; }

        /// <summary>
        /// Local (loopback) address and port to bind to. If null, a port is
        /// selected dynamically.
        /// </summary>
        public IPAddress LocalPort { get; }

        /// <summary>
        /// Optional: Command to launch after connecting transport.
        /// </summary>
        public string LaunchCommand { get; }

        //---------------------------------------------------------------------
        // IProtocol.
        //---------------------------------------------------------------------

        public string Name { get; }

        public bool IsAvailable(ITransportTarget target)
        {
            return this.RequiredTraits
                .EnsureNotNull()
                .All(target.Traits.Contains);
        }

        public Task<IProtocolContext> CreateContextAsync(
            ITransportTarget target,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(this.LaunchCommand))
            {
                // TODO
            }
            else
            {
                // TODO
            }

            throw new NotImplementedException();
        }

        //---------------------------------------------------------------------
        // Parsing.
        //---------------------------------------------------------------------

        // TODO: To/from JSON
    }

    public class ClientProtocolContext : IProtocolContext
    {
        //---------------------------------------------------------------------
        // IProtocolContext.
        //---------------------------------------------------------------------

        public Task<ITransport> ConnectTransportAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class LaunchableClientProtocolContext : ClientProtocolContext
    {
        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task LaunchAppAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
