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
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// Custom protocol, to be used with a locally installed app.
    /// </summary>
    public class AppProtocol : IProtocol
    {
        public AppProtocol(
            string name,
            IEnumerable<ITrait> requiredTraits,
            ushort remotePort,
            IPEndPoint? localEndpoint,
            IAppProtocolClient? client)
        {
            this.Name = name.ExpectNotNull(nameof(name));
            this.RequiredTraits = requiredTraits.ExpectNotNull(nameof(requiredTraits));
            this.RemotePort = remotePort;
            this.LocalEndpoint = localEndpoint;
            this.Client = client;
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Traits that a target has to have to use this protocol.
        /// </summary>
        public IEnumerable<ITrait> RequiredTraits { get; }

        /// <summary>
        /// Port to connect transport to.
        /// </summary>
        public ushort RemotePort { get; }

        /// <summary>
        /// Local (loopback) address and port to bind to. If null, a port is
        /// selected dynamically.
        /// </summary>
        public IPEndPoint? LocalEndpoint { get; }

        /// <summary>
        /// Optional: Client app to launch after connecting transport.
        /// </summary>
        public IAppProtocolClient? Client { get; }

        //---------------------------------------------------------------------
        // IProtocol.
        //---------------------------------------------------------------------

        public string Name { get; }

        public bool IsAvailable(IProtocolTarget target)
        {
            if (this.Client != null && !this.Client.IsAvailable)
            {
                return false;
            }
            else
            {
                return this.RequiredTraits
                    .EnsureNotNull()
                    .All(target.Traits.EnsureNotNull().Contains);
            }
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override int GetHashCode()
        {
            return
                this.Name.GetHashCode() ^
                this.RemotePort ^
                (this.LocalEndpoint?.GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AppProtocol);
        }

        public bool Equals(IProtocol? other)
        {
            //
            // NB. Ignore if the client is the same or not as the client
            // doesn't "define" the protocol, and different clients
            // should share the same transport if their protocol is
            // equivalent otherwise.
            //
            return other is AppProtocol protocol &&
                Equals(protocol.Name, this.Name) &&
                Enumerable.SequenceEqual(protocol.RequiredTraits, this.RequiredTraits) &&
                Equals(protocol.RemotePort, this.RemotePort) &&
                Equals(protocol.LocalEndpoint, this.LocalEndpoint);
        }

        public static bool operator ==(AppProtocol obj1, AppProtocol obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(AppProtocol obj1, AppProtocol obj2)
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
    }
}
