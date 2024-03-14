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
using Google.Solutions.Platform;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// Parser for protocol configuration files.
    /// 
    /// Example:
    /// 
    /// {
    ///     'version': 1,
    ///     'name': 'telnet',
    ///     'condition': 'isLinux()',
    ///     'remotePort': 23,
    ///     'client': {
    ///         'executable': '%SystemRoot%\system32\telnet.exe',
    ///         'arguments': '{host} {port}'
    ///     }
    /// }
    /// 
    /// The executable can be either the name of a registered app 
    /// (for ex, "chrome.exe"), or a path to an executable file.
    /// 
    /// paths Executable and arguments can contain environment variables, 
    /// for example:
    /// 
    ///   %AppData%\.myprofile
    /// 
    /// Arguments can contain the following placeholders:
    /// 
    ///   {port}:     the local port to connect to
    ///   {host}:     the locat IP address to connect to
    ///   {username}: the username to authenticate with (can be empty)
    ///   
    /// </summary>
    /// <see cref="https://learn.microsoft.com/en-us/windows/win32/shell/app-registration"/>
    public static class AppProtocolConfigurationFile
    {
        /// <summary>
        /// File extension for protocol configuration files
        /// (IAPC = IAP App Protocol Configuration).
        /// </summary>
        public const string FileExtension = ".iapc";

        //---------------------------------------------------------------------
        // Deserialization.
        //---------------------------------------------------------------------

        private static AppProtocol ReadSection(MainSection section)
        {
            if (section == null)
            {
                throw new InvalidAppProtocolException(
                    "The protocol configuration is empty");
            }
            else if (
                section.SchemaVersion < MainSection.MinSchemaVersion ||
                section.SchemaVersion > MainSection.CurrentSchemaVersion)
            {
                throw new InvalidAppProtocolException(
                    "The protocol configuration uses an unsupported schema version");
            }

            return new AppProtocol(
                section.ParseName(),
                section.ParseCondition(),
                section.ParseRemotePort(),
                section.ParseLocalEndpoint(),
                section.ParseClientSection());
        }

        public static AppProtocol ReadJson(string json)
        {
            try
            {
                return ReadSection(NewtonsoftJsonSerializer
                    .Instance
                    .Deserialize<MainSection>(json));
            }
            catch (JsonException e)
            {
                throw new InvalidAppProtocolException(
                    "The protocol configuration contains format errors", e);
            }
        }

        public static Task<AppProtocol> ReadStreamAsync(Stream stream)
        {
            return Task.Run(() =>
            {
                try
                {
                    return ReadSection(NewtonsoftJsonSerializer
                        .Instance
                        .Deserialize<MainSection>(stream));
                }
                catch (InvalidAppProtocolException e)
                {
                    throw new InvalidAppProtocolException(
                        $"The protocol configuration contains format errors", e);
                }
                catch (JsonException e)
                {
                    throw new InvalidAppProtocolException(
                        $"The protocol configuration contains format errors", e);
                }
            });
        }

        public static Task<AppProtocol> ReadFileAsync(string path)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var stream = File.OpenRead(path))
                    {
                        return ReadSection(NewtonsoftJsonSerializer
                            .Instance
                            .Deserialize<MainSection>(stream));
                    }
                }
                catch (InvalidAppProtocolException e)
                {
                    throw new InvalidAppProtocolException(
                        $"The protocol configuration file {path} contains format errors", e);
                }
                catch (JsonException e)
                {
                    throw new InvalidAppProtocolException(
                        $"The protocol configuration file {path} contains format errors", e);
                }
            });
        }

        //---------------------------------------------------------------------
        // De/Serialization classes.
        //---------------------------------------------------------------------

        internal class MainSection
        {
            internal const ushort MinSchemaVersion = 1;
            internal const ushort CurrentSchemaVersion = 1;

            /// <summary>
            /// Schema version.
            /// </summary>
            [JsonProperty("version")]
            public ushort SchemaVersion { get; set; }

            /// <summary>
            /// Name of the protocol. The name isn't guaranteed to be unique.
            /// </summary>
            [JsonProperty("name")]
            public string? Name { get; set; }

            /// <summary>
            /// Conditions for this protocol to be available. Can be
            /// an expression of one or more required traits.
            /// 
            ///   trait1() && trait2() && trait3()
            ///   
            /// Currently, only the && operator is available.
            /// </summary>
            [JsonProperty("condition")]
            public string? Condition { get; set; }

            /// <summary>
            /// Remote port to connect to.
            /// </summary>
            [JsonProperty("remotePort")]
            public string? RemotePort { get; set; }

            /// <summary>
            /// Optional: Local port.
            /// </summary>
            [JsonProperty("localPort")]
            public string? LocalPort { get; set; }

            /// <summary>
            /// Optional: Client application to launch. 
            /// </summary>
            [JsonProperty("client")]
            public ClientSection? Client { get; set; }

            internal string ParseName()
            {
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    throw new InvalidAppProtocolException("A name is required");
                }

                Debug.Assert(this.Name != null);
                return this.Name!;
            }

            internal IEnumerable<ITrait> ParseCondition()
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
                        Debug.Assert(instanceTrait != null);
                        yield return instanceTrait!;
                    }
                    else if (WindowsTrait.TryParse(clause, out var windowsTrait))
                    {
                        Debug.Assert(windowsTrait != null);
                        yield return windowsTrait!;
                    }
                    else if (LinuxTrait.TryParse(clause, out var linuxTrait))
                    {
                        Debug.Assert(linuxTrait != null);
                        yield return linuxTrait!;
                    }
                    else
                    {
                        throw new InvalidAppProtocolException(
                            "The condition contains an unrecognized clause: " + clause);
                    }
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

            internal IPEndPoint? ParseLocalEndpoint()
            {
                var localPort = this.LocalPort?.Trim();
                if (string.IsNullOrEmpty(localPort))
                {
                    return null;
                }

                Debug.Assert(localPort != null);

                var parts = localPort!.Split(':');
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

            internal IAppProtocolClient? ParseClientSection()
            {
                if (this.Client == null ||
                    string.IsNullOrWhiteSpace(this.Client.Executable))
                {
                    return null;
                }

                Debug.Assert(this.Client.Executable != null);

                if (!UserEnvironment.TryResolveAppPath(
                    this.Client.Executable!,
                    out var executablePath))
                {
                    executablePath = UserEnvironment.ExpandEnvironmentStrings(
                        this.Client.Executable!);
                }

                Debug.Assert(executablePath != null);
                return new AppProtocolClient(
                    executablePath!,
                    UserEnvironment.ExpandEnvironmentStrings(this.Client.Arguments));
            }
        }

        internal class ClientSection
        {
            /// <summary>
            /// Path to executable to launch. The path can contain
            /// environment variables, for example:
            /// 
            ///   %ProgramFiles(x86)%\program.exe
            ///   
            /// </summary>
            [JsonProperty("executable")]
            public string? Executable { get; set; }

            /// <summary>
            /// Optional: Arguments to pass to executable.
            /// </summary>
            [JsonProperty("arguments")]
            public string? Arguments { get; set; }
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
