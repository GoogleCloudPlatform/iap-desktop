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
    /// Context for parameterizing a protocol before creating a 
    /// transport and session.
    /// </summary>
    public interface IProtocolContext : IDisposable
    {
        /// <summary>
        /// Create a transport, which might involve creating a tunnel.
        /// This might require remote calls.
        /// </summary>
        Task<ITransport> ConnectTransportAsync(CancellationToken cancellationToken);
    }

    public interface IProtocolContextFactory
    {
        /// <summary>
        /// Create a protocol context for the given target.
        /// </summary>
        Task<IProtocolContext> CreateContextAsync(
            IProtocolTarget target,
            uint flags,
            CancellationToken cancellationToken);

        /// <summary>
        /// Try to parse a URL as a protocol locator.
        /// </summary>
        bool TryParse(Uri uri, out ProtocolTargetLocator? locator);
    }
}
