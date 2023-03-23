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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Globalization;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
{
    /// <summary>
    /// Contains all parameters to connect to a VM instance and establish
    /// one or more sessions.
    /// </summary>
    public abstract class ConnectionTemplateBase
    {
        /// <summary>
        /// Connection target.
        /// </summary>
        public InstanceLocator Instance { get; protected set; }

        /// <summary>
        /// Indicates whether a IAP-TCP tunnel is used.
        /// </summary>
        public bool IsTunnelled { get; protected set; }
    }

    public class RdpConnectionTemplate : ConnectionTemplateBase
    { 
        /// <summary>
        /// Windows credentials, might be null.
        /// </summary>

        public InstanceConnectionSettings Settings { get; }

        /// <summary>
        /// Endpoint to connect to. This might be a localhost endpoint.
        /// </summary>
        public string Endpoint { get; protected set; }

        /// <summary>
        /// Endpoint to connect to. This might be a localhost endpoint.
        /// </summary>
        public ushort EndpointPort { get; protected set; }

        public RdpConnectionTemplate(
            InstanceLocator instance,
            bool isTunnelled,
            string endpoint,
            ushort endpointPort,
            InstanceConnectionSettings settings)
        {
            this.Instance = instance;
            this.IsTunnelled = isTunnelled;
            this.Endpoint = endpoint;
            this.EndpointPort = endpointPort;

            this.Settings = settings;
        }
    }

    public class SshConnectionTemplate : ConnectionTemplateBase
    {
        /// <summary>
        /// Key to authenticate with.
        /// </summary>
        public AuthorizedKeyPair AuthorizedKey { get; }

        /// <summary>
        /// Terminal locale.
        /// </summary>
        public CultureInfo Language { get; }

        public IPEndPoint Endpoint { get; }

        /// <summary>
        /// Timeout to use for the initial connection attempt.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; protected set; }

        public SshConnectionTemplate(
            InstanceLocator instance,
            bool isTunnelled,
            IPEndPoint endpoint,
            AuthorizedKeyPair authorizedKey,
            CultureInfo language,
            TimeSpan connectionTimeout)
        {
            this.Instance = instance;
            this.IsTunnelled = isTunnelled;
            this.Endpoint = endpoint;
            this.ConnectionTimeout = connectionTimeout;

            this.AuthorizedKey = authorizedKey;
            this.Language = language;
        }
    }
}
