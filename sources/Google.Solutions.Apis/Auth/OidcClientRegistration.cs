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
using Google.Apis.Util;
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Security;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// An OIDC client registration.
    /// </summary>
    public class OidcClientRegistration
    {
        private readonly SecureString clientSecret;

        public OidcClientRegistration(
            OidcIssuer issuer,
            string clientId,
            string clientSecret,
            string redirectPath)
        {
            this.Issuer = issuer;
            this.ClientId = clientId.ThrowIfNullOrEmpty(nameof(clientId));
            this.clientSecret = SecureStringExtensions.FromClearText(clientSecret);
            this.RedirectPath = redirectPath.ExpectNotEmpty(nameof(redirectPath));

            Debug.Assert(this.RedirectPath.StartsWith("/"));
        }

        /// <summary>
        /// Issuer for which this registration applies.
        /// </summary>
        public OidcIssuer Issuer { get; }

        /// <summary>
        /// Client ID.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Path to use in loopback redirect URL:
        /// http://localhost/PATH/
        /// </summary>
        public string RedirectPath { get; }

        public ClientSecrets ToClientSecrets()
        {
            return new ClientSecrets
            {
                ClientId = this.ClientId,
                ClientSecret = this.clientSecret.ToClearText()
            };
        }

        public override string ToString()
        {
            return this.ClientId;
        }
    }

    public enum OidcIssuer
    {
        /// <summary>
        /// Google Sign-in.
        /// </summary>
        Gaia,

        /// <summary>
        /// Workforce/workload identity federation.
        /// </summary>
        Sts
    }
}
