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
    /// OpenID Connect session. A session is backed by a refresh token,
    /// and it ends when the refresh token is invalidated or the app is 
    /// closed.
    /// </summary>
    public interface IOidcSession
    {
        event EventHandler? Terminated;

        /// <summary>
        /// Username. The syntax differs depending on the implementation.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Credential to use for Google API calls.
        /// Not null.
        /// </summary>
        ICredential ApiCredential { get; }

        /// <summary>
        /// Offline credential for silent reauthentication.
        /// </summary>
        OidcOfflineCredential OfflineCredential { get; }

        /// <summary>
        /// Use a new session to extend the current session so
        /// that the API credential remains valid.
        /// </summary>
        void Splice(IOidcSession newSession);

        /// <summary>
        /// Terminate the session and drop offline credential,
        /// but keep the underling grant.
        /// </summary>
        void Terminate();

        /// <summary>
        /// Revoke the underlying OAuth grant.
        /// </summary>
        Task RevokeGrantAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Create a domain-service service URI that signs in the user
        /// and redirects to the target.
        /// </summary>
        Uri CreateDomainSpecificServiceUri(Uri target);
    }
}
