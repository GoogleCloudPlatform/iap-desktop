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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Solutions.Apis.Auth.Iam;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestStsService
    {
        //---------------------------------------------------------------------
        // IntrospectToken.
        //---------------------------------------------------------------------

        [Test]
        public async Task IntrospectToken_WhenClientCredentialsMissing_ThenThrowsException()
        {
            using (var service = new StsService(
                new BaseClientService.Initializer()))
            {
                var request = new StsService.IntrospectTokenRequest()
                {
                    Token = "invalid"
                };

                try
                {
                    await service
                        .IntrospectTokenAsync(request, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.Fail("Expected TokenResponseException");
                }
                catch (TokenResponseException e)
                {
                    Assert.That(e.Error.Error, Is.EqualTo("invalid_client"));
                }
            }
        }

        [Test]
        public async Task IntrospectToken_WhenClientCredentialsInvalid_ThenThrowsException()
        {
            using (var service = new StsService(
                new BaseClientService.Initializer()))
            {
                var request = new StsService.IntrospectTokenRequest()
                {
                    Token = "invalid",
                    ClientCredentials = new ClientSecrets()
                    {
                        ClientId = "invalid"
                    }
                };

                try
                {
                    await service
                        .IntrospectTokenAsync(request, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.Fail("Expected TokenResponseException");
                }
                catch (TokenResponseException e)
                {
                    Assert.That(e.Error.Error, Is.EqualTo("invalid_client"));
                }
            }
        }
    }
}
