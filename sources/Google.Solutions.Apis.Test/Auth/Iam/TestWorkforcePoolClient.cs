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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestWorkforcePoolClient
    {
        private static Mock<IDeviceEnrollment> CreateDisabledEnrollment()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);
            return enrollment;
        }

        //---------------------------------------------------------------------
        // Code flow.
        //---------------------------------------------------------------------

        [Test]
        public void AuthorizationUrl()
        {
            var provider = new WorkforcePoolProviderLocator(
                "global",
                "pool",
                "provider");

            var flow = new WorkforcePoolClient.StsCodeFlow(
                new WorkforcePoolClient.StsCodeFlowInitializer(
                    WorkforcePoolClient.CreateEndpoint(),
                    CreateDisabledEnrollment().Object,
                    provider,
                    new ClientSecrets()
                    {
                        ClientId = "client-id",
                        ClientSecret = "client-secret"
                    },
                    TestProject.UserAgent)
                {
                    Scopes = new[] { "scope-1", "scope-2" }
                });

            var url = flow.CreateAuthorizationCodeRequest("http://localhost/").Build();
            var parameters = HttpUtility.ParseQueryString(url.Query);

            Assert.AreEqual("auth.cloud.google", url.Host);
            Assert.AreEqual(provider.ToString(), parameters.Get("provider_name"));
            Assert.AreEqual("client-id", parameters.Get("client_id"));
            Assert.AreEqual("http://localhost/", parameters.Get("redirect_uri"));
            Assert.AreEqual("none", parameters.Get("state"));
            Assert.AreEqual("scope-1 scope-2", parameters.Get("scope"));
        }

        [Test]
        [InteractiveTest] 
        public async Task __TestAuth() // TODO: Remove this test
        {
            var store = new Mock<IOidcOfflineCredentialStore>();

            var secret = Environment.GetEnvironmentVariable("WWAUTH_CLIENT_SECRET").Split(':');
            var registration = new OidcClientRegistration(
                OidcIssuer.Iam,
                secret[0],
                secret[1],
                "/");

            var provider = new WorkforcePoolProviderLocator(
                "global",
                "ntdev-azuread",
                "ntdev-azuread-saml");

            var client = new WorkforcePoolClient(
                WorkforcePoolClient.CreateEndpoint(),
                CreateDisabledEnrollment().Object,
                store.Object,
                provider,
                registration,
                TestProject.UserAgent);

            var session = await client
                .AuthorizeAsync(
                    new LoopbackCodeReceiver(
                        "/",
                        "done!!1!"),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
