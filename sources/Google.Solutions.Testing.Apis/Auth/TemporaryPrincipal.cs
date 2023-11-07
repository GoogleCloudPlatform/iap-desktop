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
using Google.Apis.Util;
using Google.Solutions.Testing.Apis.Integration;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Auth
{
    internal abstract class TemporaryPrincipal
    {
        private readonly CloudResourceManagerService crmService;

        protected TemporaryPrincipal(
            CloudResourceManagerService crmService,
            string username)
        {
            this.crmService = crmService;
            this.Username = username;
        }

        public string Username { get; }

        internal abstract string PrincipalId { get; }

        public async Task GrantRolesAsync(string[] roles)
        {
            GoogleApiException lastException = null;

            var backoff = new ExponentialBackOff();

            for (var attempt = 0; attempt < backoff.MaxNumOfRetries; attempt++)
            {
                var policy = await this.crmService.Projects
                    .GetIamPolicy(
                        new Google.Apis.CloudResourceManager.v1.Data.GetIamPolicyRequest(),
                        TestProject.ProjectId)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                foreach (var role in roles)
                {
                    policy.Bindings.Add(
                        new Google.Apis.CloudResourceManager.v1.Data.Binding()
                        {
                            Role = role,
                            Members = new string[] { this.PrincipalId }
                        });
                }

                try
                {
                    await this.crmService.Projects
                        .SetIamPolicy(
                            new Google.Apis.CloudResourceManager.v1.Data.SetIamPolicyRequest()
                            {
                                Policy = policy
                            },
                            TestProject.ProjectId)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    return;
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 409)
                {
                    lastException = e;

                    //
                    // Concurrent modification - back off and retry. 
                    //
                    await Task
                        .Delay(backoff.DeltaBackOff)
                        .ConfigureAwait(false);
                }
            }

            throw lastException;
        }
    }
}
