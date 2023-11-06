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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Testing.Apis.Auth;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Integration
{
    public sealed class CredentialAttribute : NUnitAttribute, IParameterDataSource
    {
        public string[] Roles { get; set; } = Array.Empty<string>();

        public string Role
        {
            set => this.Roles = new[] { value };
            get => this.Roles.First();
        }

        private string CreateSpecificationFingerprint()
        {
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                // Create a hash of the image specification.
                var specificationRaw = Encoding.UTF8.GetBytes(
                    string.Join(",", this.Roles));
                return "s" + BitConverter
                    .ToString(sha.ComputeHash(specificationRaw))
                    .Replace("-", string.Empty)
                    .Substring(0, 14)
                    .ToLower();
            }
        }

        private static async Task<IAuthorization> CreateAuthorizationWithRolesAsync(
            string fingerprint,
            string[] roles)
        {
            if (roles == null || !roles.Any())
            {
                //
                // Return the credentials of the (admin) account the
                // tests are run as.
                //
                return TestProject.AdminAuthorization;
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
                            fingerprint)
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

        public IEnumerable GetData(IParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(ResourceTask<IAuthorization>))
            {
                var fingerprint = CreateSpecificationFingerprint();
                return new[] {
                    ResourceTask<IAuthorization>.ProvisionOnce(
                        parameter.Method,
                        fingerprint,
                        () => CreateAuthorizationWithRolesAsync(
                            fingerprint,
                            this.Roles))
                };
            }
            else if (parameter.ParameterType == typeof(ResourceTask<ICredential>))
            {
                var fingerprint = CreateSpecificationFingerprint();
                return new[] {
                    ResourceTask<ICredential>.ProvisionOnce(
                        parameter.Method,
                        fingerprint,
                        async () => (await CreateAuthorizationWithRolesAsync(
                            fingerprint, 
                            this.Roles))
                            .Session
                            .ApiCredential)
                };
            }
            else
            {
                throw new ArgumentException(
                    $"Parameter must be of type {typeof(ICredential).Name}");
            }
        }

        public override string ToString()
        {
            return CreateSpecificationFingerprint();
        }
    }
}
