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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    public class AppProtocol : IProtocol
    {
        public AppProtocol(
            string name,
            IEnumerable<IProtocolTargetTrait> requiredTraits,
            ITransportPolicy policy,
            ushort remotePort,
            IPEndPoint localEndpoint,
            string launchCommand)
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
            return
                this.Name.GetHashCode() ^
                this.RemotePort ^
                this.Policy.GetHashCode() ^
                (this.LocalEndpoint?.GetHashCode() ?? 0)^
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
        // Deserialization.
        //---------------------------------------------------------------------

        //public static AppProtocol Deserialize(string json)
        //{
        //    var definition = NewtonsoftJsonSerializer
        //        .Instance
        //        .Deserialize<Definition>(json);

        //}

        //---------------------------------------------------------------------
        // De/Serialization classes.
        //---------------------------------------------------------------------

        public class Configuration
        {
            /// <summary>
            /// Name of the protocol. The name isn't guaranteed to be unique.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; }

            /// <summary>
            /// Conditions for this protocol to be available. Can be
            /// an expression of one or more required traits.
            /// 
            ///   trait1() && trait2() && trait3()
            ///   
            /// Currently, only the && operator is available.
            /// </summary>
            [JsonProperty("condition")]
            public string Condition { get; }

            /// <summary>
            /// Remote port to connect to.
            /// </summary>
            [JsonProperty("remotePort")]
            public string RemotePort { get; }

            /// <summary>
            /// Optional: Local port.
            /// </summary>
            [JsonProperty("localPort")]
            public string LocalPort { get; }

            /// <summary>
            /// Optional: Command to launch. The command can contain
            /// environment variables, for example:
            /// 
            ///   %ProgramFiles(x86)%\program.exe
            /// 
            /// Additionally, the command con contain the following
            /// placeholders:
            /// 
            ///   %port% - contains the local port to connect to
            ///   %host% - contain the locat IP address to connect to
            ///   
            /// </summary>
            [JsonProperty("command")]
            public string Command { get; }

            internal static IEnumerable<IProtocolTargetTrait> ParseCondition(string condition)
            {
                if (condition == null)
                {
                    yield break;
                }

                var clauses = condition
                    .Replace("&&", "\0")
                    .Split('\0')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                foreach (var clause in clauses)
                {
                    if (InstanceTrait.TryParse(clause, out var trait))
                    {
                        yield return trait;
                    }
                    else
                    {
                        throw new InvalidAppProtocolException(
                            "The condition contains an unrecognized clause: " + clause);
                    }
                }
            }
        }
    }

    public class InvalidAppProtocolException : FormatException
    {
        public InvalidAppProtocolException(string message) : base(message)
        {
        }
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
