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
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Iam.v1.Data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Integration
{
    public class CredentialRequest
    {
        private string[] roles;
        private readonly string name;

        private async Task<ServiceAccount> CreateOrGetServiceAccountAsync()
        {
            var service = TestProject.CreateIamService();
            var email = $"{this.name}@{TestProject.ProjectId}.iam.gserviceaccount.com";
            try
            {
                return await service.Projects.ServiceAccounts
                    .Get($"projects/{TestProject.ProjectId}/serviceAccounts/{email}")
                    .ExecuteAsync();
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

        private async Task GrantRolesToServiceAccountAsync(ServiceAccount member)
        {
            var service = TestProject.CreateCloudResourceManagerService();

            var policy = await service.Projects
                .GetIamPolicy(
                    new GetIamPolicyRequest(),
                    TestProject.ProjectId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            foreach (var role in this.roles)
            {
                policy.Bindings.Add(
                    new Apis.CloudResourceManager.v1.Data.Binding()
                    {
                        Role = role,
                        Members = new string[] { $"serviceAccount:{member.Email}" }
                    });
            }

            await service.Projects.SetIamPolicy(
                    new Apis.CloudResourceManager.v1.Data.SetIamPolicyRequest()
                    {
                        Policy = policy
                    },
                    TestProject.ProjectId)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private async Task<ICredential> CreateTemporaryCredentialsAsync(
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

            return GoogleCredential.FromAccessToken(response.AccessToken);
        }


        public CredentialRequest(
            string serviceAccountName,
            string[] roles)
        {
            this.name = serviceAccountName;
            this.roles = roles;
        }

        public async Task<ICredential> GetCredentialAsync()
        {
            if (this.roles == null || !this.roles.Any())
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
                    var serviceAccount = await CreateOrGetServiceAccountAsync();

                    // TODO: Assign roles.
                    await GrantRolesToServiceAccountAsync(serviceAccount);

                    // Create a token.
                    return await CreateTemporaryCredentialsAsync(serviceAccount.Email);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
        public override string ToString()
        {
            return this.name;
        }
    }
}
