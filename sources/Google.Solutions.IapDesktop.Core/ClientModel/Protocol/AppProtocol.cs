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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// Custom protocol, to be used with a locally installed app.
    /// </summary>
    public class AppProtocol : IProtocol
    {
        public AppProtocol(
            string name,
            IEnumerable<IProtocolTargetTrait> requiredTraits,
            ITransportPolicy policy,
            ushort remotePort,
            IPEndPoint localEndpoint,
            Command launchCommand)
        {
            this.Name = name.ExpectNotNull(nameof(name));
            this.RequiredTraits = requiredTraits.ExpectNotNull(nameof(requiredTraits));
            this.Policy = policy.ExpectNotNull(nameof(policy));
            this.RemotePort = remotePort.ExpectNotNull(nameof(remotePort));
            this.LocalEndpoint = localEndpoint;
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
        public ITransportPolicy Policy { get; }

        /// <summary>
        /// Port to connect transport to.
        /// </summary>
        public ushort RemotePort { get; }

        /// <summary>
        /// Local (loopback) address and port to bind to. If null, a port is
        /// selected dynamically.
        /// </summary>
        public IPEndPoint LocalEndpoint { get; }

        /// <summary>
        /// Optional: Command to launch after connecting transport.
        /// </summary>
        public Command LaunchCommand { get; }

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

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override int GetHashCode()
        {
            return
                this.Name.GetHashCode() ^
                this.RemotePort ^
                this.Policy.GetHashCode() ^
                (this.LocalEndpoint?.GetHashCode() ?? 0) ^
                (this.LaunchCommand?.GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AppProtocol);
        }

        public bool Equals(IProtocol other)
        {
            return other is AppProtocol protocol &&
                Equals(protocol.Name, this.Name) &&
                Enumerable.SequenceEqual(protocol.RequiredTraits, this.RequiredTraits) &&
                Equals(protocol.Policy, this.Policy) &&
                Equals(protocol.RemotePort, this.RemotePort) &&
                Equals(protocol.LocalEndpoint, this.LocalEndpoint) &&
                Equals(protocol.LaunchCommand, this.LaunchCommand);
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

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public class Command : IEquatable<Command>
        {
            /// <summary>
            /// Path to the executable to be launched. 
            /// </summary>
            public string Executable { get; }

            /// <summary>
            /// Optional: Arguments to be passed. Arguments can contain the
            /// following placeholders:
            /// 
            ///   %port% - contains the local port to connect to
            ///   %host% - contain the locat IP address to connect to
            ///   
            /// </summary>
            public string Arguments { get; }

            internal Command(string executable, string arguments)
            {
                this.Executable = executable.ExpectNotEmpty(nameof(executable));
                this.Arguments = arguments;
            }

            public override string ToString()
            {
                return this.Executable +
                    (this.Arguments == null ? string.Empty : " " + this.Arguments);
            }

            //-----------------------------------------------------------------
            // Equality.
            //-----------------------------------------------------------------

            public bool Equals(Command other)
            {
                return other is Command cmd &&
                    Equals(cmd.Executable, this.Executable) &&
                    Equals(cmd.Arguments, this.Arguments);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Command);
            }

            public override int GetHashCode()
            {
                return
                    this.Executable.GetHashCode() ^
                    (this.Arguments?.GetHashCode() ?? 0);
            }

            public static bool operator ==(Command obj1, Command obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(Command obj1, Command obj2)
            {
                return !(obj1 == obj2);
            }
        }
    }
}
