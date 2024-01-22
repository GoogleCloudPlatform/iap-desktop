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

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Persistent storage for offline credentials.
    /// </summary>
    public interface IOidcOfflineCredentialStore
    {
        /// <summary>
        /// Try to load an offline credential.
        /// </summary>
        bool TryRead(out OidcOfflineCredential? credential);

        /// <summary>
        /// Store offline credential.
        /// </summary>
        void Write(OidcOfflineCredential credential);

        /// <summary>
        /// Delete all offline credentials.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Offline credential that permits silent extension of an existing
    /// OAuth session.
    /// </summary>
    public class OidcOfflineCredential
    {
        /// <summary>
        /// Source of the credential.
        /// </summary>
        public OidcIssuer Issuer { get; }

        /// <summary>
        /// Refresh token, not null.
        /// </summary>
        public string RefreshToken { get; }

        /// <summary>
        /// ID token, optional.
        /// </summary>
        public string? IdToken { get; }

        /// <summary>
        /// List of authorized scopes (space separated).
        /// </summary>
        public string Scope { get; }

        public OidcOfflineCredential(
            OidcIssuer issuer,
            string scope,
            string refreshToken,
            string? idToken)
        {
            this.Issuer = issuer;
            this.Scope = scope;
            this.RefreshToken = refreshToken.ExpectNotEmpty(nameof(refreshToken));
            this.IdToken = idToken;
        }
    }
}
