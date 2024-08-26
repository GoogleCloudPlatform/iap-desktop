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
using System.Web;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestAuthPortalCodeFlow
    {
        private static readonly WorkforcePoolProviderLocator SampleProvider
            = new WorkforcePoolProviderLocator(
                "global",
                "pool",
                "provider");

        private static readonly ClientSecrets SampleClientCredentials
            = new ClientSecrets()
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };

        private static Mock<IDeviceEnrollment> CreateDisabledEnrollment()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);
            return enrollment;
        }

        [Test]
        public void AuthorizationUrl()
        {
            var flow = new AuthPortalCodeFlow(
                new AuthPortalCodeFlow.Initializer(
                    WorkforcePoolClient.CreateEndpoint(),
                    CreateDisabledEnrollment().Object,
                    SampleProvider,
                    SampleClientCredentials,
                    TestProject.UserAgent)
                {
                    Scopes = new[] { "scope-1", "scope-2" }
                });

            var url = flow.CreateAuthorizationCodeRequest("http://localhost/").Build();
            var parameters = HttpUtility.ParseQueryString(url.Query);

            Assert.AreEqual("auth.cloud.google", url.Host);
            Assert.AreEqual(SampleProvider.ToString(), parameters.Get("provider_name"));
            Assert.AreEqual("client-id", parameters.Get("client_id"));
            Assert.AreEqual("http://localhost/", parameters.Get("redirect_uri"));
            Assert.AreEqual("scope-1 scope-2", parameters.Get("scope"));
        }

        [Test]
        public void AuthorizationUrl_WhenNotEnrolled_ThenUsesTls(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var initializer = new AuthPortalCodeFlow.Initializer(
                WorkforcePoolClient.CreateEndpoint(),
                enrollment.Object,
                SampleProvider,
                SampleClientCredentials,
                TestProject.UserAgent);

            Assert.AreEqual("https://auth.cloud.google/authorize", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://sts.googleapis.com/v1/oauthtoken", initializer.TokenServerUrl);
        }

        [Test]
        public void AuthorizationUrl_WhenEnrolled_ThenUsesMtls()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var initializer = new AuthPortalCodeFlow.Initializer(
                WorkforcePoolClient.CreateEndpoint(),
                enrollment.Object,
                SampleProvider,
                SampleClientCredentials,
                TestProject.UserAgent);

            Assert.AreEqual("https://auth.cloud.google/authorize", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://sts.mtls.googleapis.com/v1/oauthtoken", initializer.TokenServerUrl);
        }
    }
}
