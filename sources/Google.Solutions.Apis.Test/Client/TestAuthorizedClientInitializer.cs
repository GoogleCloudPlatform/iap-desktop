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
using Google.Apis.Compute.v1;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Apis.Mocks;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestAuthorizedClientInitializer
    {
        private class SampleAdapter : IClient
        {
            public SampleAdapter(IServiceEndpoint endpoint)
            {
                this.Endpoint = endpoint;
            }

            public IServiceEndpoint Endpoint { get; }
        }

        private const string SampleEndpoint = "https://sample.googleapis.com/";

        [Test]
        public void WhenNotEnrolled_ThenDeviceCertificateAuthenticationIsDisabled(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            var endpoint = new ServiceEndpoint<SampleAdapter>(SampleEndpoint);

            var initializer = new AuthorizedClientInitializer(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.AreEqual(SampleEndpoint, initializer.BaseUri);

            var client = new ComputeService(initializer);
            Assert.IsFalse(client.IsDeviceCertificateAuthenticationEnabled());
        }

        [Test]
        public void WhenEnrolled_ThenDeviceCertificateAuthenticationIsDisabled()
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(SampleEndpoint);

            var initializer = new AuthorizedClientInitializer(
                endpoint,
                AuthorizationMocks.ForSecureConnectUser(),
                TestProject.UserAgent);

            Assert.AreEqual("https://cloudresourcemanager.mtls.googleapis.com/", initializer.BaseUri);

            var client = new ComputeService(initializer);
            Assert.IsTrue(client.IsDeviceCertificateAuthenticationEnabled());
        }

        [Test]
        public void WhenPscOverrideFound_ThenInitializerUsesPscEndpoint()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);


            var endpoint = new ServiceEndpoint<SampleAdapter>(SampleEndpoint)
            {
                PscEndpointOverride = "crm.example.com"
            };

            var initializer = new AuthorizedClientInitializer(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.AreEqual("https://crm.example.com/", initializer.BaseUri);
        }
    }
}
