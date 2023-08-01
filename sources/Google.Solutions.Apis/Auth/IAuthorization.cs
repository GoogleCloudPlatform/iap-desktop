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

using Google.Apis.Auth.OAuth2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// OIDC authorization. An authorization is a sequence of one
    /// or more OIDC sessions.
    /// </summary>
    public interface IAuthorization
    {
        /// <summary>
        /// Raised after a successful reauthorization. Might be
        /// triggere on any thread.
        /// </summary>
        event EventHandler Reauthorized;

        /// <summary>
        /// Curent OIDC session.
        /// </summary>
        IOidcSession Session { get; }

        /// <summary>
        /// Credential to use for Google API requests.
        /// </summary>
        ICredential Credential { get; }

        /// <summary>
        /// Terminate the current session and delete any offline
        /// credential.
        /// </summary>
        Task RevokeAsync(); // TODO: Rename to Terminate

        /// <summary>
        /// Reauthorize, only intended to be used by jobs.
        /// </summary>
        Task ReauthorizeAsync(CancellationToken token);

        //---------------------------------------------------------------------
        // TODO: Obsolete methods below?

        string Email { get; }

        /// <summary>
        /// Device. This is non-null, but the enrollment might be
        /// in state "Disabled".
        /// </summary>
        IDeviceEnrollment DeviceEnrollment { get; }
    }
}
