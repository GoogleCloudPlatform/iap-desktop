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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol
{
    /// <summary>
    /// Protocol context that can be used for initiating a (terminal)
    /// session.
    /// </summary>
    public interface ISessionContext<TCredential, TParameters> : IProtocolContext
    {
        /// <summary>
        /// Target instance of this session.
        /// </summary>
        InstanceLocator Instance { get; }

        /// <summary>
        /// Parameters for the session.
        /// </summary>
        TParameters Parameters { get; }

        /// <summary>
        /// Authorize the credential, if necessary. This might require
        /// remote calls, so the method should be called in a job.
        /// </summary>
        Task<TCredential> AuthorizeCredentialAsync(CancellationToken cancellationToken);
    }
}
