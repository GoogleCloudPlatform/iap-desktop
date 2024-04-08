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
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Gaia
{
    /// <summary>
    /// A Google "1PI" OIDC session.
    /// 
    /// Sessions are subject to the 'Google Cloud Session Length' control,
    /// and end when reauthorization is triggered.
    /// </summary>
    internal class GaiaOidcSession : OidcSessionBase, IGaiaOidcSession
    {
        public GaiaOidcSession(
            UserCredential apiCredential,
            IJsonWebToken idToken)
            : base(apiCredential)
        {
            this.IdToken = idToken.ExpectNotNull(nameof(idToken));

            Debug.Assert(idToken.Payload.Email != null);
        }

        //---------------------------------------------------------------------
        // IGaiaOidcSession.
        //---------------------------------------------------------------------

        public IJsonWebToken IdToken { get; }
        public override string Username => this.IdToken.Payload.Email;
        public string Email => this.IdToken.Payload.Email;
        public string? HostedDomain => this.IdToken.Payload.HostedDomain;

        public override OidcOfflineCredential OfflineCredential
        {
            get
            {
                //
                // Prefer fresh ID token if it's available, otherwise
                // use old.
                //
                var idToken = string.IsNullOrEmpty(this.Credential.Token.IdToken)
                    ? this.IdToken.ToString()
                    : this.Credential.Token.IdToken;

                return new OidcOfflineCredential(
                    OidcIssuer.Gaia,
                    this.Credential.Token.Scope,
                    this.Credential.Token.RefreshToken,
                    idToken);
            }
        }

        public override void Splice(IOidcSession newSession)
        {
            //
            // The "Google Cloud session length" control causes Gaia
            // session to expire and the refresh token to be revoked.
            // Splicing happens after the app has performed "reauth" and
            // has acquired a new set of tokens (including a new refresh
            // token).
            //
            //
            base.Splice(newSession);

            Debug.Assert(((IGaiaOidcSession)newSession).Email == this.Email);
            Debug.Assert(((IGaiaOidcSession)newSession).HostedDomain == this.HostedDomain);
        }

        public override async Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            //
            // Revoke the refresh token. This removes the underlying grant.
            //
            await this.Credential.Flow
                .RevokeTokenAsync(
                    null,
                    this.Credential.Token.RefreshToken,
                    cancellationToken)
                .ConfigureAwait(false);

            Terminate();
        }

        public override Uri CreateDomainSpecificServiceUri(Uri target)
        {
            if (!string.IsNullOrEmpty(this.HostedDomain) &&

                //
                // ServiceLogin can't handle percent-encoded quotes.
                //
                !target.AbsoluteUri.Contains("%22"))
            {
                //
                // Sign in using same domain.
                //
                return new Uri($"https://www.google.com/a/{this.HostedDomain}/ServiceLogin" +
                    $"?continue={WebUtility.UrlEncode(target.ToString())}");
            }
            else
            {
                //
                // Unmanaged user account, just use the normal URL.
                //
                return target;
            }
        }
    }
}
