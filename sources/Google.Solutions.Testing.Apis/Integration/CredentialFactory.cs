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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Testing.Apis.Auth;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.Testing.Apis.Integration
{
    public static class CredentialFactory
    {
        public static async Task<IAuthorization> CreateServiceAccountAuthorizationAsync(
            string name,
            string[] roles)
        {
            if (roles == null || !roles.Any())
            {
                //
                // Return the credentials of the (admin) account the
                // tests are run as.
                //
                return new TemporaryAuthorization(
                    new Enrollment(),
                    new TemporaryGaiaSession(
                        "admin@gserviceaccount.com",
                        TestProject.GetAdminCredential()));
            }
            else
            {
                //
                // Create a service account with exactly these
                // roles and return temporary credentials.
                //
                try
                {
                    //
                    // Create a service account.
                    //
                    var serviceAccount = await TemporaryServiceAccount
                        .EmplaceAsync(
                            TestProject.CreateIamService(),
                            TestProject.CreateIamCredentialsService(),
                            TestProject.CreateCloudResourceManagerService(),
                            name)
                        .ConfigureAwait(true);

                    //
                    // Assign roles.
                    //
                    await serviceAccount
                        .GrantRolesAsync(roles)
                        .ConfigureAwait(true);

                    //
                    // Impersonate.
                    //
                    return await serviceAccount
                        .ImpersonateAsync()
                        .ConfigureAwait(false);
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
}
