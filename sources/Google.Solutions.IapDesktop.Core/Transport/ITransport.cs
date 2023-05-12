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

using System;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport
{
    public interface ITransport : IDisposable 
    {
        /// <summary>
        /// Flags, informative.
        /// </summary>
        TransportFlags Flags { get; }

        /// <summary>
        /// Traffic statistics.
        /// </summary>
        TransportStatistics Statistics { get; }

        /// <summary>
        /// Probe if this transport works. Throws an exception
        /// in case of a negative probe.
        /// </summary>
        Task Probe(TimeSpan timeout);

        /// <summary>
        /// Endpoint that clients can connect to. This might 
        /// be a localhost endpoint.
        /// </summary>
        IPEndPoint LocalEndpoint { get; }
    }

    [Flags]
    public enum TransportFlags
    {
        None,

        /// <summary>
        /// Transport is using mTLS.
        /// </summary>
        Mtls
    }

    public struct TransportStatistics
    {
        public ulong BytesReceived;
        public ulong BytesTransmitted;
    }
}
