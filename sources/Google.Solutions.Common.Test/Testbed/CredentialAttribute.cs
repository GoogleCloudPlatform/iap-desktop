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
using Google.Apis.Iam.v1;
using Google.Apis.Iam.v1.Data;
using Google.Apis.IAMCredentials.v1;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Testbed
{
    public class CredentialAttribute : NUnitAttribute, IParameterDataSource
    {
        public string[] Roles { get; set; } = Array.Empty<string>();

        private string CreateSpecificationFingerprint()
        {
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                // Create a hash of the image specification.
                var specificationRaw = Encoding.UTF8.GetBytes(
                    string.Join(",", this.Roles));
                return "s" + BitConverter
                    .ToString(sha.ComputeHash(specificationRaw))
                    .Replace("-", String.Empty)
                    .Substring(0, 14)
                    .ToLower();
            }
        }

        private async Task<ServiceAccount> CreateOrGetServiceAccountAsync()
        {
            var service = Defaults.CreateIamService();
            var name = CreateSpecificationFingerprint();
            var email = $"{name}@{Defaults.ProjectId}.iam.gserviceaccount.com";
            try
            {
                return await service.Projects.ServiceAccounts
                    .Get($"projects/{Defaults.ProjectId}/serviceAccounts/{email}")
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
                    $"projects/{Defaults.ProjectId}")
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
        }

        private async Task GrantRolesToServiceAccountAsync(ServiceAccount member)
        {
            var service = Defaults.CreateCloudResourceManagerService();
            
            var policy = await service.Projects
                .GetIamPolicy(
                    new GetIamPolicyRequest(),
                    Defaults.ProjectId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            foreach (var role in this.Roles)
            {
                policy.Bindings.Add(
                    new Apis.CloudResourceManager.v1.Data.Binding()
                    {
                        Role = role,
                        Members = new string[] { $"serviceAccount:{member.Email}"}
                    });
            }

            await service.Projects.SetIamPolicy(
                    new Apis.CloudResourceManager.v1.Data.SetIamPolicyRequest()
                    {
                        Policy = policy
                    },
                    Defaults.ProjectId)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private async Task<ICredential> CreateTemporaryCredentialsAsync(
            string serviceAccountEmail)
        {
            var service = Defaults.CreateIamCredentialsService();
            var response = await service.Projects.ServiceAccounts.GenerateAccessToken(
                    new Apis.IAMCredentials.v1.Data.GenerateAccessTokenRequest()
                    {
                        Scope = new string[] { Defaults.CloudPlatformScope }
                    },
                    $"projects/-/serviceAccounts/{serviceAccountEmail}")
                .ExecuteAsync()
                .ConfigureAwait(false);

            return GoogleCredential.FromAccessToken(response.AccessToken);
        }

        private async Task<ICredential> CreateAccessToken()
        {
            // Create a service account.
            var serviceAccount = await CreateOrGetServiceAccountAsync();

            // TODO: Assign roles.
            await GrantRolesToServiceAccountAsync(serviceAccount);

            // Create a token.
            return await CreateTemporaryCredentialsAsync(serviceAccount.Email);
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(ICredential))
            {
                return new ICredential[] { CreateAccessToken().Result };
            }
            else
            {
                throw new ArgumentException(
                    $"Parameter must be of type {typeof(ICredential).Name}");
            }
        }

        public override string ToString()
        {
            return this.CreateSpecificationFingerprint();
        }
    }
}
