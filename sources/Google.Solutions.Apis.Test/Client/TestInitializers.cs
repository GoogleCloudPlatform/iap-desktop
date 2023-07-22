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

using Google.Apis.Compute.v1;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Apis.Mocks;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestInitializers
    {
        private class SampleClient : IClient
        {
            public SampleClient(IServiceEndpoint endpoint)
            {
                this.Endpoint = endpoint;
            }

            public IServiceEndpoint Endpoint { get; }
        }

        private const string SampleEndpoint = "https://sample.googleapis.com/";

        //---------------------------------------------------------------------
        // CreateServiceInitializer.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnrolled_ThenCreateServiceInitializerUsesTls(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            var endpoint = new ServiceEndpoint<SampleClient>(
                PrivateServiceConnectDirections.None,
                SampleEndpoint);

            var initializer = Initializers.CreateServiceInitializer(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.AreEqual(SampleEndpoint, initializer.BaseUri);

            var client = new ComputeService(initializer);
            Assert.IsFalse(client.IsDeviceCertificateAuthenticationEnabled());
        }

        [Test]
        public void WhenEnrolled_ThenCreateServiceInitializerUsesTlsUsesMtls()
        {
            var endpoint = new ServiceEndpoint<SampleClient>(
                PrivateServiceConnectDirections.None, 
                SampleEndpoint);

            var initializer = Initializers.CreateServiceInitializer(
                endpoint,
                AuthorizationMocks.ForSecureConnectUser(),
                TestProject.UserAgent);

            Assert.AreEqual("https://sample.mtls.googleapis.com/", initializer.BaseUri);

            var client = new ComputeService(initializer);
            Assert.IsTrue(client.IsDeviceCertificateAuthenticationEnabled());
        }

        [Test]
        public void WhenPscOverrideFound_ThenCreateServiceInitializerUsesPsc()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            var endpoint = new ServiceEndpoint<SampleClient>(
                new PrivateServiceConnectDirections("crm.googleapis.com"),
                SampleEndpoint);

            var initializer = Initializers.CreateServiceInitializer(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.AreEqual("https://crm.googleapis.com/", initializer.BaseUri);

            var client = new ComputeService(initializer);
            Assert.IsFalse(client.IsDeviceCertificateAuthenticationEnabled());
        }


        //---------------------------------------------------------------------
        // CreateOpenIdInitializer.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnrolled_ThenCreateOpenIdInitializerUsesTls(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var initializer = Initializers.CreateOpenIdInitializer(
                SignInClient.OAuthClient.CreateEndpoint(),
                SignInClient.OpenIdClient.CreateEndpoint(),
                enrollment.Object);

            Assert.AreEqual("https://accounts.google.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth2.googleapis.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth2.googleapis.com/revoke", initializer.RevokeTokenUrl);
            Assert.AreEqual("https://openidconnect.googleapis.com/v1/userinfo", initializer.UserInfoUrl.ToString());
        }

        [Test]
        public void WhenEnrolled_ThenCreateOpenIdInitializerUsesTlsUsesMtls()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var initializer = Initializers.CreateOpenIdInitializer(
                SignInClient.OAuthClient.CreateEndpoint(),
                SignInClient.OpenIdClient.CreateEndpoint(),
                enrollment.Object);

            Assert.AreEqual("https://accounts.google.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth2.mtls.googleapis.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth2.mtls.googleapis.com/revoke", initializer.RevokeTokenUrl);
            Assert.AreEqual("https://openidconnect.mtls.googleapis.com/v1/userinfo", initializer.UserInfoUrl.ToString());
        }
    }
}
