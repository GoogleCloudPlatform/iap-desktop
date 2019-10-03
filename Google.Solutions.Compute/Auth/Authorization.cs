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
using Google.Apis.Util.Store;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Google.Solutions.Compute.Auth
{
    public interface IAuthorization
    {
        ICredential Credential { get; }

        string UserId { get; }

        Task RevokeAsync();
    }

    public class OAuthAuthorization : IAuthorization
    {
        public const string StoreUserId = "oauth";

        private readonly UserCredential credential;
        private readonly IDataStore credentialStore;

        public OAuthAuthorization(
            UserCredential credential,
            IDataStore credentialStore)
        {
            this.credential = credential;
            this.credentialStore = credentialStore;
        }

        public ICredential Credential => this.credential;

        public string UserId => this.credential.UserId;

        public Task RevokeAsync()
        {
            return this.credentialStore.DeleteAsync<object>(StoreUserId);
        }

        public static async Task<OAuthAuthorization> TryLoadExistingAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            using (var flow = new GoogleAuthorizationCodeFlow(initializer))
            {
                var installedApp = new AuthorizationCodeInstalledApp(
                    flow,
                    new LocalServerCodeReceiver(closePageReponse));

                var existingTokenResponse = await flow.LoadTokenAsync(
                    OAuthAuthorization.StoreUserId,
                    CancellationToken.None);

                if (!installedApp.ShouldRequestAuthorizationCode(existingTokenResponse))
                {
                    return new OAuthAuthorization(
                        new UserCredential(
                            new GoogleAuthorizationCodeFlow(initializer),
                            OAuthAuthorization.StoreUserId,
                            existingTokenResponse),
                        initializer.DataStore);
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<OAuthAuthorization> CreateAuthorizationAsync(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            using (var flow = new GoogleAuthorizationCodeFlow(initializer))
            {
                var installedApp = new AuthorizationCodeInstalledApp(
                    flow,
                    new LocalServerCodeReceiver(closePageReponse));

                return new OAuthAuthorization(
                    await installedApp.AuthorizeAsync(
                        OAuthAuthorization.StoreUserId,
                        CancellationToken.None),
                    initializer.DataStore);
            }
        }
    }

    public class GcloudAuthorization : IAuthorization
    {
        private const string StoreUserId = "gcloud";

        private readonly GoogleCredential credential;
        private readonly IDataStore credentialStore;

        private GcloudAuthorization(
            string userId,
            GoogleCredential credential,
            IDataStore credentialStore)
        {
            this.UserId = userId;
            this.credential = credential;
            this.credentialStore = credentialStore;
        }

        public ICredential Credential => this.credential;

        public string UserId { get; }

        public Task RevokeAsync()
        {
            return this.credentialStore.DeleteAsync<object>(StoreUserId);
        }

        public static bool CanAuthorize(GcloudAccount account)
        {
            return account != null && File.Exists(account.CredentialFile);
        }

        public static async Task<GcloudAuthorization> TryLoadExistingAuthorizationAsync(
            GcloudAccount account,
            IDataStore credentialStore)
        {
            // Check if there is a marker in the credential store indicating
            // that the user authorized us to use the GCloud credentials.
            if (CanAuthorize(account) &&
                account.Name == await credentialStore.GetAsync<string>(StoreUserId))
            {
                return new GcloudAuthorization(
                    account.Name,
                    account.Credential,
                    credentialStore);
            }
            else
            {
                return null;
            }
        }

        public static async Task<GcloudAuthorization> CreateAuthorizationAsync(
            GcloudAccount account,
            IDataStore credentialStore)
        {
            var authorization = new GcloudAuthorization(
                account.Name,
                account.Credential,
                credentialStore);

            if (credentialStore != null)
            {
                // Save a marker in the credential store indicating
                // that the user authorized us to use the GCloud credentials.
                await credentialStore.StoreAsync<string>(StoreUserId, account.Name);
            }

            return authorization;
        }
    }
}
