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
using Google.Solutions.Common.Util;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
{
    public struct TransportParameters
    {
        /// <summary>
        /// Connection target.
        /// </summary>
        public InstanceLocator Instance { get; }

        /// <summary>
        /// Type of transport.
        /// </summary>
        public TransportType Type { get; }

        /// <summary>
        /// Endpoint to connect to. This might be a localhost endpoint.
        /// </summary>
        public IPEndPoint Endpoint { get; }

        public TransportParameters(
            TransportType type,
            InstanceLocator instance,
            IPEndPoint endpoint)
        {
            this.Type = type;
            this.Instance = instance.ExpectNotNull(nameof(instance));
            this.Endpoint = endpoint.ExpectNotNull(nameof(endpoint));
        }

        public enum TransportType
        {
            IapTunnel
        }
    }

    public struct ConnectionTemplate<TSessionParameters>
    {
        /// <summary>
        /// Transport to use.
        /// </summary>
        public TransportParameters Transport { get; }

        /// <summary>
        /// Parameters for sessions.
        /// </summary>
        public TSessionParameters Session { get; }

        public ConnectionTemplate(
            TransportParameters transport,
            TSessionParameters session)
        {
            this.Transport = transport.ExpectNotNull(nameof(transport));
            this.Session = session.ExpectNotNull(nameof(session));
        }
    }
}
