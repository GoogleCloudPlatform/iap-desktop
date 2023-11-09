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

using Google.Apis.CloudResourceManager.v1;
using Google.Apis.Iam.v1;
using Google.Apis.Iam.v1.Data;
using Google.Apis.IAMCredentials.v1;
using Google.Apis.Json;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Threading;
using Google.Solutions.Testing.Apis.Integration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Auth
{
    internal class TemporaryServiceAccount : TemporaryPrincipal
    {
        private static readonly AsyncLock serviceAccountLock = new AsyncLock();
        private readonly IAMCredentialsService credentialsService;

        public TemporaryServiceAccount(
            CloudResourceManagerService crmService,
            IAMCredentialsService credentialsService,
            string email)
            : base(crmService, email)
        {
            this.credentialsService = credentialsService;
        }

        internal override string PrincipalId => $"serviceAccount:{this.Username}";

        public async Task<IAuthorization> ImpersonateAsync()
        {
            var response = await this.credentialsService.Projects
                .ServiceAccounts
                .GenerateAccessToken(
                    new Google.Apis.IAMCredentials.v1.Data.GenerateAccessTokenRequest()
                    {
                        Scope = new string[] { TestProject.CloudPlatformScope }
                    },
                    $"projects/-/serviceAccounts/{this.Username}")
                .ExecuteAsync()
                .ConfigureAwait(false);

            return new TemporaryAuthorization(
                new Enrollment(),
                new TemporaryGaiaSession(
                    this.Username,
                    new TemporaryCredential(response.AccessToken)));
        }

        public async Task<string> SignJwtAsync(
            IDictionary<string, object> claims,
            CancellationToken cancellationToken)
        {
            var response = await this.credentialsService.Projects.ServiceAccounts
                .SignJwt(
                    new Google.Apis.IAMCredentials.v1.Data.SignJwtRequest()
                    {
                        Payload = NewtonsoftJsonSerializer.Instance.Serialize(claims)
                    },
                    $"projects/-/serviceAccounts/{this.Username}")
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return response.SignedJwt;
        }

        //---------------------------------------------------------------------
        // Factory.
        //---------------------------------------------------------------------

        internal static async Task<TemporaryServiceAccount> EmplaceAsync(
            IamService iamService,
            IAMCredentialsService credentialsService,
            CloudResourceManagerService crmService,
            string name)
        {
            using (await serviceAccountLock.AcquireAsync(CancellationToken.None))
            {
                var email = $"{name}@{TestProject.ProjectId}.iam.gserviceaccount.com";
                try
                {
                    var account = await iamService.Projects.ServiceAccounts
                        .Get($"projects/{TestProject.ProjectId}/serviceAccounts/{email}")
                        .ExecuteAsync()
                        .ConfigureAwait(true);

                    return new TemporaryServiceAccount(
                        crmService,
                        credentialsService,
                        account.Email);
                }
                catch (Exception)
                {
                    var account = await iamService.Projects.ServiceAccounts
                        .Create(
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

                    return new TemporaryServiceAccount(
                        crmService,
                        credentialsService,
                        account.Email);
                }
            }
        }
    }
}
