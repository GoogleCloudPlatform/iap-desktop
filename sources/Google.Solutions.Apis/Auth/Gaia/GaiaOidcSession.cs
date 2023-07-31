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
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Gaia
{
    internal class GaiaOidcSession : IGaiaOidcSession
    {
        private readonly UserCredential apiCredential;

        public GaiaOidcSession(
            IDeviceEnrollment deviceEnrollment,
            UserCredential apiCredential,
            IJsonWebToken idToken)
        {
            this.DeviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.apiCredential = apiCredential.ExpectNotNull(nameof(apiCredential));
            this.IdToken = idToken.ExpectNotNull(nameof(idToken));

            Debug.Assert(idToken.Payload.Email != null);
        }

        public event EventHandler Terminated;
        public IJsonWebToken IdToken { get; }
        public ICredential ApiCredential => this.apiCredential;
        public IDeviceEnrollment DeviceEnrollment { get; }
        public string Username => this.IdToken.Payload.Email;
        public string Email => this.IdToken.Payload.Email;
        public string HostedDomain => this.IdToken.Payload.HostedDomain;

        public OidcOfflineCredential OfflineCredential
        {
            get
            {
                //
                // Prefer fresh ID token if it's available, otherwise
                // use old.
                //
                var idToken = string.IsNullOrEmpty(this.apiCredential.Token.IdToken)
                    ? this.IdToken.ToString()
                    : this.apiCredential.Token.IdToken;

                return new OidcOfflineCredential(
                    OidcOfflineCredentialIssuer.Gaia,
                    this.apiCredential.Token.RefreshToken,
                    idToken);
            }
        }

        public void Splice(IOidcSession newSession)
        {
            //
            // Replace the current tokens (which might be invalid)
            // with the new session's tokens.
            //
            // By retaining the UserCredential object, we ensure that
            // any existing API client (which has the UserCredential installed
            // as interceptor) continues to work, and immediately starts
            // using the new tokens.
            //
            // NB. A more obviuos approach to achiveve the same would be to
            // implement an ICredential facade, and swap out its backing
            // credential with the new session's credential. But UserCredential
            // hands out `this` pointers in multiple places, and that makes the
            // facade approach brittle in practice.
            //
            if (newSession is GaiaOidcSession gaiaSession && gaiaSession != null)
            {
                this.apiCredential.Token = gaiaSession.apiCredential.Token;

                //
                // NB. Leave IdToken and other properties as is as their
                // values shouldn't have changed.
                //

                Debug.Assert(gaiaSession.Email == this.Email);
                Debug.Assert(gaiaSession.HostedDomain == this.HostedDomain);
            }
            else
            {
                throw new ArgumentException(nameof(newSession));
            }
        }

        public void Terminate()
        {
            this.Terminated?.Invoke(this, EventArgs.Empty);
        }

        public async Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            //
            // Revoke the refresh token. This removes the underlying grant.
            //
            await this.apiCredential.Flow
                .RevokeTokenAsync(
                    null, 
                    this.apiCredential.Token.RefreshToken, 
                    cancellationToken)
                .ConfigureAwait(false);

            Terminate();
        }
    }
}
