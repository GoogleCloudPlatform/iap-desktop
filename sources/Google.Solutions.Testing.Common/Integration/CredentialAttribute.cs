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
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace Google.Solutions.Testing.Common.Integration
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
                    .Replace("-", String.Empty)
                    .Substring(0, 14)
                    .ToLower();
            }
        }


        public IEnumerable GetData(IParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(ResourceTask<ICredential>))
            {
                var fingerprint = CreateSpecificationFingerprint();
                return new[] {
                    ResourceTask<ICredential>.ProvisionOnce(
                        parameter.Method,
                        fingerprint,
                        () => CredentialFactory.CreateServiceAccountCredentialAsync(
                            fingerprint,
                            this.Roles))
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
            return this.CreateSpecificationFingerprint();
        }
    }
}
