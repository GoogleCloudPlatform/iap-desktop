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
        string FormatArguments(
            ITransport transport,
            AppProtocolParameters parameters);
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
        ///   %port% - contains the local port to connect to
        ///   %host% - contain the locat IP address to connect to
        ///   
        /// </summary>
        internal string ArgumentsTemplate { get; }

        protected internal AppProtocolClient(
            string executable,
            string argumentsTemplate)
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
            get => true;
        }

        public string FormatArguments(
            ITransport transport,
            AppProtocolParameters parameters)
        {
            string arguments = this.ArgumentsTemplate;
            if (arguments != null)
            {
                arguments = arguments
                    .Replace("%port%", transport.Endpoint.Port.ToString())
                    .Replace("%host%", transport.Endpoint.Address.ToString());
            }

            return arguments;
        }
    }
}
