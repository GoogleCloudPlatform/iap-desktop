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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Integration
{
    public sealed class CredentialAttribute : NUnitAttribute, IParameterDataSource
    {
        /// <summary>
        /// Required roles.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Required role.
        /// </summary>
        public string Role
        {
            set => this.Roles = new[] { value };
            get => this.Roles.First();
        }

        /// <summary>
        /// Type of principal.
        /// </summary>
        public PrincipalType Type { get; set; } = PrincipalType.Gaia;

        private string CreateSpecificationFingerprint()
        {
            using (var sha = SHA256.Create())
            {
                //
                // Create a hash of the account specification.
                //
                var specification = new StringBuilder()
                    .Append(TestProject.ProjectId)
                    .Append(string.Join(",", this.Roles));

                var specificationRaw =
                    Encoding.UTF8.GetBytes(specification.ToString());

                return (this.Type == PrincipalType.Gaia ? "s" : "i") +
                    BitConverter
                    .ToString(sha.ComputeHash(specificationRaw))
                    .Replace("-", string.Empty)
                    .Substring(0, 14)
                    .ToLower();
            }
        }

        private async Task<IAuthorization> CreateAuthorizationWithRolesAsync(
            string fingerprint)
        {
            if (this.Type == PrincipalType.WorkforceIdentity)
            {
                //
                // Create a service account.
                //
                var trustedServiceAccount = await TemporaryServiceAccount
                    .EmplaceAsync(
                        TestProject.CreateIamService(),
                        TestProject.CreateIamCredentialsService(),
                        TestProject.CreateCloudResourceManagerService(),
                        "identity-platform")
                    .ConfigureAwait(true);

                var subject = await TemporaryWorkforcePoolSubject
                    .CreateAsync(
                        TestProject.CreateCloudResourceManagerService(),
                        new TemporaryWorkforcePoolSubject.IdentityPlatformService(
                            TestProject.Configuration.IdentityPlatformApiKey),
                        trustedServiceAccount,
                        TestProject.Configuration.WorkforcePoolId,
                        TestProject.Configuration.WorkforceProviderId,
                        fingerprint,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                //
                // Assign roles.
                //
                await subject
                    .GrantRolesAsync(this.Roles)
                    .ConfigureAwait(true);

                //
                // Impersonate.
                //
                return await subject
                    .ImpersonateAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else if (this.Roles == null || !this.Roles.Any())
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
                        .GrantRolesAsync(this.Roles)
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
                        () => CreateAuthorizationWithRolesAsync(fingerprint))
                };
            }
            else if (parameter.ParameterType == typeof(ResourceTask<ICredential>))
            {
                var fingerprint = CreateSpecificationFingerprint();
                return new[] {
                    ResourceTask<ICredential>.ProvisionOnce(
                        parameter.Method,
                        fingerprint,
                        async () => (await CreateAuthorizationWithRolesAsync(fingerprint))
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

    public enum PrincipalType
    {
        Gaia,
        WorkforceIdentity
    }
}
