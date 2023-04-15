//
// Copyright 2019 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Authorization
{
    /// <summary>
    /// OAuth authorization for this app.
    /// 
    /// The Authorization object is initialized during startup and
    /// a single instance is kept throughout the lifetime of the
    /// process, even in the case of re-auth.
    /// </summary>
    public class AppAuthorization : IAuthorization
    {
        private readonly ISignInAdapter adapter;

        private readonly UserCredential credential;

        private AppAuthorization(
            ISignInAdapter adapter,
            IDeviceEnrollment deviceEnrollment,
            UserCredential credential,
            UserInfo userInfo)
        {
            //
            // NB. We must use the same UserCredential object throghout the
            // lifetime of the app because existing clients maintain a 
            // reference to the object.
            //
            // In case of re-auth, we therefore don't swap the credential
            // object, but merely replace its embedded refresh token.
            //
            this.adapter = adapter.ExpectNotNull(nameof(adapter));
            this.DeviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.credential = credential.ExpectNotNull(nameof(credential));
            this.UserInfo = userInfo.ExpectNotNull(nameof(userInfo));
        }

        public event EventHandler Reauthorized;

        public ICredential Credential => this.credential;

        public string Email => this.UserInfo.Email;

        public UserInfo UserInfo { get; private set; }

        public IDeviceEnrollment DeviceEnrollment { get; }

        public Task RevokeAsync()
        {
            return this.adapter.DeleteStoredRefreshToken();
        }

        public async Task ReauthorizeAsync(CancellationToken token)
        {
            //
            // As this is a 3p OAuth app, we do not support Gnubby/Password-based
            // reauth. Instead, we simply trigger a new authorization (code flow).
            //
            // Use the current user as login hint to simplify the browser flow
            // a little.
            //
            var newCredential = await this.adapter
                .SignInWithBrowserAsync(this.Email, token)
                .ConfigureAwait(false);

            //
            // Keep the credential object, but swap out its refresh token.
            //
            this.credential.Token = newCredential.Token;

            //
            // The user might have changed to a different user account,
            // so we have to re-fetch user information.
            //
            this.UserInfo = await this.adapter
                .QueryUserInfoAsync(newCredential, token)
                .ConfigureAwait(false);

            this.Reauthorized?.Invoke(this, EventArgs.Empty);
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        public static async Task<AppAuthorization> TryLoadExistingAuthorizationAsync(
            ISignInAdapter oauthAdapter,
            IDeviceEnrollment deviceEnrollment,
            CancellationToken token)
        {
            var credential = await oauthAdapter
                .TrySignInWithRefreshTokenAsync(token)
                .ConfigureAwait(false);
            if (credential != null)
            {
                //
                // Authorize worked, so the token was still valid.
                //
                var userInfo = await oauthAdapter
                    .QueryUserInfoAsync(credential, token)
                    .ConfigureAwait(false);

                return new AppAuthorization(
                    oauthAdapter,
                    deviceEnrollment,
                    credential,
                    userInfo);
            }
            else
            {
                //
                // No token found, or it was invalid.
                //
                return null;
            }
        }

        public static async Task<AppAuthorization> CreateAuthorizationAsync(
            ISignInAdapter oauthAdapter,
            IDeviceEnrollment deviceEnrollment,
            CancellationToken token)
        {
            var credential = await oauthAdapter
                .SignInWithBrowserAsync(null, token)
                .ConfigureAwait(false);

            var userInfo = await oauthAdapter
                .QueryUserInfoAsync(credential, token)
                .ConfigureAwait(false);

            return new AppAuthorization(
                oauthAdapter,
                deviceEnrollment,
                credential,
                userInfo);
        }
    }
}
