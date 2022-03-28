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
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Iam.v1.Data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.Common.Test.Integration
{
    public static class CredentialFactory
    {
        private static async Task<ServiceAccount> CreateOrGetServiceAccountAsync(
            string name)
        {
            var service = TestProject.CreateIamService();
            var email = $"{name}@{TestProject.ProjectId}.iam.gserviceaccount.com";
            try
            {
                return await service.Projects.ServiceAccounts
                    .Get($"projects/{TestProject.ProjectId}/serviceAccounts/{email}")
                    .ExecuteAsync()
                    .ConfigureAwait(true);
            }
            catch (Exception)
            {
                return await service.Projects.ServiceAccounts.Create(
                        new CreateServiceAccountRequest()
                        {
                            AccountId = name,
                            ServiceAccount = new ServiceAccount()
                            {
                                DisplayName = "Test account for integration testing"
                            }
                        },
                    $"projects/{TestProject.ProjectId}")
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
        }

        private static async Task GrantRolesToServiceAccountAsync(
            ServiceAccount member,
            string[] roles)
        {
            var service = TestProject.CreateCloudResourceManagerService();

            for (int attempt = 0; attempt < 6; attempt++)
            {
                var policy = await service.Projects
                    .GetIamPolicy(
                        new GetIamPolicyRequest(),
                        TestProject.ProjectId)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                foreach (var role in roles)
                {
                    policy.Bindings.Add(
                        new Apis.CloudResourceManager.v1.Data.Binding()
                        {
                            Role = role,
                            Members = new string[] { $"serviceAccount:{member.Email}" }
                        });
                }

                try
                {
                    await service.Projects.SetIamPolicy(
                            new Apis.CloudResourceManager.v1.Data.SetIamPolicyRequest()
                            {
                                Policy = policy
                            },
                            TestProject.ProjectId)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    break;
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 409)
                {
                    // Concurrent modification - back off and retry. 
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
        }

        private static async Task<ICredential> CreateTemporaryCredentialsAsync(
            string serviceAccountEmail)
        {
            var service = TestProject.CreateIamCredentialsService();
            var response = await service.Projects.ServiceAccounts.GenerateAccessToken(
                    new Apis.IAMCredentials.v1.Data.GenerateAccessTokenRequest()
                    {
                        Scope = new string[] { TestProject.CloudPlatformScope }
                    },
                    $"projects/-/serviceAccounts/{serviceAccountEmail}")
                .ExecuteAsync()
                .ConfigureAwait(false);

            return new TemporaryServiceCredential(
                serviceAccountEmail, 
                response.AccessToken);
        }

        public static async Task<ICredential> CreateServiceAccountCredentialAsync(
            string name,
            string[] roles)
        {
            if (roles == null || !roles.Any())
            {
                // Return the credentials of the (admin) account the
                // tests are run as.
                return TestProject.GetAdminCredential();
            }
            else
            {
                // Create a service account with exactly these
                // roles and return temporary credentials.
                try
                {
                    // Create a service account.
                    var serviceAccount = await CreateOrGetServiceAccountAsync(name)
                        .ConfigureAwait(true);

                    // Assign roles.
                    await GrantRolesToServiceAccountAsync(serviceAccount, roles)
                        .ConfigureAwait(true);

                    // Create a token.
                    return await CreateTemporaryCredentialsAsync(serviceAccount.Email)
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }

    public class TemporaryServiceCredential : ServiceCredential
    {
        public string AccessToken { get; }
        public string Email { get; }

        internal TemporaryServiceCredential(
            string email,
            string accessToken)
            : base(new ServiceCredential.Initializer(GoogleAuthConsts.TokenUrl))
        {
            this.Email = email;
            this.AccessToken = accessToken;
        }

        public override Task<bool> RequestAccessTokenAsync(
            CancellationToken taskCancellationToken)
        {
            this.Token = new TokenResponse()
            {
                AccessToken = this.AccessToken,
                ExpiresInSeconds = 3600
            };

            return Task.FromResult(true);
        }
    }
}
