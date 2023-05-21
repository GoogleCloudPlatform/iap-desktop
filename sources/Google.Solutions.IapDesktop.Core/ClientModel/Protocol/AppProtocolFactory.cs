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

using Google.Apis.Json;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Google.Solutions.Platform;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    public class AppProtocolFactory
    {
        //---------------------------------------------------------------------
        // Deserialization.
        //---------------------------------------------------------------------

        /// <summary>
        /// Read a protocol configuration from JSON.
        /// 
        /// Example:
        /// 
        /// {
        ///     'name': 'telnet',
        ///     'condition': 'isLinux()',
        ///     'accessPolicy': 'AllowAll',
        ///     'remotePort': 23,
        ///     'command': {
        ///         'executable': '%SystemRoot%\system32\telnet.exe'
        ///     }
        /// }
        /// </summary>
        public virtual AppProtocol FromJson(string json)
        {
            try
            {
                var section = NewtonsoftJsonSerializer
                    .Instance
                    .Deserialize<ConfigurationSection>(json);

                if (section == null)
                {
                    throw new InvalidAppProtocolException(
                        "The protocol configuration is empty");
                }

                return new AppProtocol(
                    section.ParseName(),
                    section.ParseCondition(),
                    section.ParseAccessPolicy(),
                    section.ParseRemotePort(),
                    section.ParseLocalEndpoint(),
                    section.ParseCommand());
            }
            catch (JsonException e)
            {
                throw new InvalidAppProtocolException(
                    "The protocol configuration is malformed", e);
            }
        }

        //---------------------------------------------------------------------
        // De/Serialization classes.
        //---------------------------------------------------------------------

        internal class ConfigurationSection
        {
            /// <summary>
            /// Name of the protocol. The name isn't guaranteed to be unique.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Conditions for this protocol to be available. Can be
            /// an expression of one or more required traits.
            /// 
            ///   trait1() && trait2() && trait3()
            ///   
            /// Currently, only the && operator is available.
            /// </summary>
            [JsonProperty("condition")]
            public string Condition { get; set; }

            /// <summary>
            /// Policy for determining whether access should be allowed.
            /// </summary>
            [JsonProperty("accessPolicy")]
            public string AccessPolicy { get; set; }

            /// <summary>
            /// Remote port to connect to.
            /// </summary>
            [JsonProperty("remotePort")]
            public string RemotePort { get; set; }

            /// <summary>
            /// Optional: Local port.
            /// </summary>
            [JsonProperty("localPort")]
            public string LocalPort { get; set; }

            /// <summary>
            /// Optional: Command to launch. 
            /// </summary>
            [JsonProperty("command")]
            public CommandSection Command { get; set; }

            internal string ParseName()
            {
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    throw new InvalidAppProtocolException("A name is required");
                }

                return this.Name;
            }

            internal IEnumerable<IProtocolTargetTrait> ParseCondition()
            {
                if (this.Condition == null)
                {
                    yield break;
                }

                var clauses = this.Condition
                    .Replace("&&", "\0")
                    .Split('\0')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                foreach (var clause in clauses)
                {
                    if (InstanceTrait.TryParse(clause, out var instanceTrait))
                    {
                        yield return instanceTrait;
                    }
                    else if (WindowsTrait.TryParse(clause, out var windowsTrait))
                    {
                        yield return windowsTrait;
                    }
                    else if (LinuxTrait.TryParse(clause, out var linuxTrait))
                    {
                        yield return linuxTrait;
                    }
                    else
                    {
                        throw new InvalidAppProtocolException(
                            "The condition contains an unrecognized clause: " + clause);
                    }
                }
            }

            internal ITransportPolicy ParseAccessPolicy()
            {
                if ("AllowAll".Equals(this.AccessPolicy?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new AllowAllPolicy();
                }
                else
                {
                    throw new InvalidAppProtocolException(
                        $"The access policy {this.AccessPolicy} is invalid");

                }
            }

            internal ushort ParseRemotePort()
            {
                if (ushort.TryParse(this.RemotePort, out var port))
                {
                    return port;
                }
                else
                {
                    throw new InvalidAppProtocolException("A remote port is required");
                }
            }

            internal IPEndPoint ParseLocalEndpoint()
            {
                var localPort = this.LocalPort?.Trim();
                if (string.IsNullOrEmpty(localPort))
                {
                    return null;
                }

                var parts = localPort.Split(':');
                if (parts.Length == 1)
                {
                    //
                    // Port only.
                    //
                    if (ushort.TryParse(parts[0], out var port))
                    {
                        return new IPEndPoint(IPAddress.Loopback, port);
                    }
                }
                else if (parts.Length == 2)
                {
                    //
                    // IP:port.
                    //
                    if (IPAddress.TryParse(parts[0], out var ip) &&
                        ushort.TryParse(parts[1], out var port))
                    {
                        return new IPEndPoint(ip, port);
                    }
                }

                throw new InvalidAppProtocolException(
                    "The local port must be a number or a IPv4/port tuple in the " +
                    "format <ip>:<port>.");
            }

            internal AppProtocol.Command ParseCommand()
            {
                if (this.Command == null || 
                    string.IsNullOrWhiteSpace(this.Command.Executable))
                {
                    return null;
                }

                return new AppProtocol.Command(
                    UserEnvironment.ExpandEnvironmentStrings(this.Command.Executable),
                    UserEnvironment.ExpandEnvironmentStrings(this.Command.Arguments));
            }
        }

        internal class CommandSection
        {
            /// <summary>
            /// Path to executable to launch. The path can contain
            /// environment variables, for example:
            /// 
            ///   %ProgramFiles(x86)%\program.exe
            ///   
            /// </summary>
            [JsonProperty("executable")]
            public string Executable { get; set; }

            /// <summary>
            /// Optional: Arguments to pass to executable. They can contain
            /// environment variables, for example:
            /// 
            ///   %AppData%\.myprofile
            /// 
            /// Arguments can contain the following
            /// placeholders:
            /// 
            ///   %port% - contains the local port to connect to
            ///   %host% - contain the locat IP address to connect to
            ///   
            /// </summary>
            [JsonProperty("arguments")]
            public string Arguments { get; set; }
        }
    }


    public class InvalidAppProtocolException : FormatException
    {
        public InvalidAppProtocolException(string message) : base(message)
        {
        }

        public InvalidAppProtocolException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
