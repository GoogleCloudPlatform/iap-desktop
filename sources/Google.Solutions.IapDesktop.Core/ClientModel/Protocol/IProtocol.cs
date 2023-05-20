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

using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// A protocol that can be used atop a transport for certain targets.
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// Unique and stable ID for the protocol, such as "ssh".
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name of the profile, suitable for displaying.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Check if this profile is applicable for the given target.
        /// </summary>
        bool IsAvailable(IProtocolTarget target);

        //TODO: Extract to IProtocolSessionContextFactory?!
        ///// <summary>
        ///// Create a context for the given target.
        ///// </summary>
        //Task<IProtocolSessionContext> CreateSessionContextAsync(
        //    IProtocolTarget target,
        //    CancellationToken cancellationToken);
    }

    /// <summary>
    /// Context for parameterizing a protocol before creating a 
    /// transport and session.
    /// </summary>
    public interface IProtocolSessionContext : IDisposable
    {
        /// <summary>
        /// Create a transport, which might involve creating a tunnel.
        /// This might require remote calls.
        /// </summary>
        Task<ITransport> ConnectTransportAsync(CancellationToken cancellationToken);
    }
}
