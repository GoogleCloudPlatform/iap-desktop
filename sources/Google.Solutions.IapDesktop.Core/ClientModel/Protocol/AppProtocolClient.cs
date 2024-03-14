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
using System.IO;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// A thick client application.
    /// </summary>
    public interface IAppProtocolClient
    {
        /// <summary>
        /// Indicates if the application is available on this system.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Path to the executable to be launched. 
        /// </summary>
        string Executable { get; }

        /// <summary>
        /// Create command line arguments, incorporating the information
        /// from the transport if necessary.
        /// </summary>
        string? FormatArguments(
            ITransport transport,
            AppProtocolParameters parameters);

        /// <summary>
        /// Check if the app supports NLA.
        /// </summary>
        bool IsNetworkLevelAuthenticationSupported { get; }

        /// <summary>
        /// Check if a the client requires a username in its command line.
        /// Only applies if NLA is not supported.
        /// </summary>
        bool IsUsernameRequired { get; }
    }

    public class AppProtocolClient : IAppProtocolClient
    {
        /// <summary>
        /// Path to the executable to be launched. 
        /// </summary>
        public string Executable { get; }

        /// <summary>
        /// Optional: Arguments to be passed. Arguments can contain the
        /// following placeholders:
        /// 
        ///   {port}:     the local port to connect to
        ///   {host}:     the locat IP address to connect to
        ///   {username}: the username to authenticate with (can be empty)
        ///   
        /// </summary>
        internal string? ArgumentsTemplate { get; }

        protected internal AppProtocolClient(
            string executable,
            string? argumentsTemplate)
        {
            this.Executable = executable.ExpectNotEmpty(nameof(executable));
            this.ArgumentsTemplate = argumentsTemplate;
        }

        public override string ToString()
        {
            return this.Executable +
                (this.ArgumentsTemplate == null ? string.Empty : " " + this.ArgumentsTemplate);
        }

        //-----------------------------------------------------------------
        // IAppProtocolClient.
        //-----------------------------------------------------------------

        public bool IsAvailable
        {
            get => File.Exists(this.Executable);
        }

        public string? FormatArguments(
            ITransport transport,
            AppProtocolParameters parameters)
        {
            var arguments = this.ArgumentsTemplate;
            if (arguments != null)
            {
                //
                // Don't permit quotes in username as they could interfere
                // with argument quoting.
                //
                if (!string.IsNullOrWhiteSpace(parameters.PreferredUsername) &&
                    (parameters.PreferredUsername!.Contains("\"") ||
                     parameters.PreferredUsername.Contains("'")))
                {
                    throw new ArgumentException("The username contains invalid characters");
                }

                //
                // NB. We use {x} instead of %x% here to prevent
                // clashes with envirnment variables.
                //
                arguments = arguments
                    .Replace("{port}", transport.Endpoint.Port.ToString())
                    .Replace("{host}", transport.Endpoint.Address.ToString())
                    .Replace("{username}", parameters.PreferredUsername ?? string.Empty);
            }

            return arguments;
        }

        public bool IsNetworkLevelAuthenticationSupported
        {
            get => false;
        }

        public bool IsUsernameRequired
        {
            get => this.ArgumentsTemplate != null &&
                this.ArgumentsTemplate.Contains("{username}");
        }
    }
}
