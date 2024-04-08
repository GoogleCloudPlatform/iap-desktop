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

namespace Google.Solutions.Apis.Auth.Gaia
{
    /// <summary>
    /// A Google "1PI" OIDC session.
    /// 
    /// Sessions are subject to the 'Google Cloud Session Length' control,
    /// and end when reauthorization is triggered.
    /// </summary>
    public interface IGaiaOidcSession : IOidcSession
    {
        /// <summary>
        /// ID token for the signed-in user.
        /// Not null.
        /// </summary>
        IJsonWebToken IdToken { get; }

        /// <summary>
        /// Primary email address of user.
        /// </summary>
        string Email { get; }

        /// <summary>
        /// Primary domain of the user's Cloud Identity/Workspace
        /// account. Null if it's a consumer user account.
        /// </summary>
        string? HostedDomain { get; }
    }
}
