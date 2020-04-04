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
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Auth
{
    public interface IOAuthAdapter : IDisposable
    {
        Task<TokenResponse> GetStoredRefreshTokenAsync(CancellationToken token);
        
        bool IsRefreshTokenValid(TokenResponse tokenResponse);

        Task DeleteStoredRefreshToken();
        
        ICredential AuthorizeUsingRefreshToken(TokenResponse tokenResponse);
        
        Task<ICredential> AuthorizeUsingBrowserAsync(CancellationToken token);
    }

    public class GoogleOAuthAdapter : IOAuthAdapter
    {
        public const string StoreUserId = "oauth";

        private readonly GoogleAuthorizationCodeFlow.Initializer initializer;
        private readonly GoogleAuthorizationCodeFlow flow;
        private readonly AuthorizationCodeInstalledApp installedApp;

        public GoogleOAuthAdapter(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            this.initializer = initializer;
            this.flow = new GoogleAuthorizationCodeFlow(initializer);
            this.installedApp = new AuthorizationCodeInstalledApp(
                this.flow,
                new LocalServerCodeReceiver(closePageReponse));
        }

        public Task<TokenResponse> GetStoredRefreshTokenAsync(CancellationToken token)
        {
            return this.flow.LoadTokenAsync(
                StoreUserId,
                token);
        }

        public Task DeleteStoredRefreshToken()
        {
            return this.initializer.DataStore.DeleteAsync<TokenResponse>(StoreUserId);
        }

        public ICredential AuthorizeUsingRefreshToken(TokenResponse tokenResponse)
        {
            return new UserCredential(
                this.flow,
                StoreUserId,
                tokenResponse);
        }

        public async Task<ICredential> AuthorizeUsingBrowserAsync(CancellationToken token)
        {
            return await this.installedApp.AuthorizeAsync(
                StoreUserId,
                token);
        }

        public bool IsRefreshTokenValid(TokenResponse tokenResponse)
        {
            return !this.installedApp.ShouldRequestAuthorizationCode(tokenResponse);
        }

        public void Dispose()
        {
            this.flow.Dispose();
        }
    }
}
