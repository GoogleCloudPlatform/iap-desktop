﻿//
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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    public class ClientAppProtocol : IProtocol
    {
        public ClientAppProtocol(
            string name,
            IEnumerable<IProtocolTargetTrait> requiredTraits,
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
        public IEnumerable<IProtocolTargetTrait> RequiredTraits { get; }

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

        public bool IsAvailable(IProtocolTarget target)
        {
            return this.RequiredTraits
                .EnsureNotNull()
                .All(target.Traits.Contains);
        }

        public Task<IProtocolSessionContext> CreateSessionContextAsync(
            IProtocolTarget target,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(this.LaunchCommand))
            {
                // TODO: implement
            }
            else
            {
                // TODO: implement
            }

            throw new NotImplementedException();
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ClientAppProtocol protocol &&
                protocol.Name == this.Name;
        }

        public bool Equals(IProtocol other)
        {
            return Equals(other as ClientAppProtocol);
        }

        public static bool operator ==(ClientAppProtocol obj1, ClientAppProtocol obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ClientAppProtocol obj1, ClientAppProtocol obj2)
        {
            return !(obj1 == obj2);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string ToString()
        {
            return this.Name;
        }

        //---------------------------------------------------------------------
        // Parsing.
        //---------------------------------------------------------------------

        // TODO: implement 
    }

    public class ClientProtocolContext : IProtocolSessionContext
    {
        //---------------------------------------------------------------------
        // IProtocolContext.
        //---------------------------------------------------------------------

        public Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class LaunchableClientProtocolContext : ClientProtocolContext
    {
        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task LaunchAppAsync(
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}