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

            var endpoint = new ServiceEndpoint<SampleClient>(SampleEndpoint);

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
            var endpoint = new ServiceEndpoint<SampleClient>(SampleEndpoint);

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

            var endpoint = new ServiceEndpoint<SampleClient>(SampleEndpoint)
            {
                PscHostOverride = "crm.googleapis.com"
            };

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

            var accountsEndpoint = new ServiceEndpoint<SignInClient.AuthorizationClient>(
                new Uri("https://accounts.google.com"),
                new Uri("https://accounts.mtls.google.com"));
            var oauthEndpoint = new ServiceEndpoint<SignInClient.OAuthClient>(
                "https://oauth.googleapis.com");
            var oidcEndpoint = new ServiceEndpoint<SignInClient.OpenIdClient>(
                "https://openidconnect.googleapis.com");

            var initializer = Initializers.CreateOpenIdInitializer(
                accountsEndpoint,
                oauthEndpoint,
                oidcEndpoint,
                enrollment.Object);

            Assert.AreEqual("https://accounts.google.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth.googleapis.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth.googleapis.com/revoke", initializer.RevokeTokenUrl);
            Assert.AreEqual("https://openidconnect.googleapis.com/v1/userinfo", initializer.UserInfoUrl.ToString());
        }

        [Test]
        public void WhenEnrolled_ThenCreateOpenIdInitializerUsesTlsUsesMtls()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var accountsEndpoint = new ServiceEndpoint<SignInClient.AuthorizationClient>(
                new Uri("https://accounts.google.com"),
                new Uri("https://accounts.mtls.google.com"));
            var oauthEndpoint = new ServiceEndpoint<SignInClient.OAuthClient>(
                "https://oauth.googleapis.com");
            var oidcEndpoint = new ServiceEndpoint<SignInClient.OpenIdClient>(
                "https://openidconnect.googleapis.com");

            var initializer = Initializers.CreateOpenIdInitializer(
                accountsEndpoint,
                oauthEndpoint,
                oidcEndpoint,
                enrollment.Object);

            Assert.AreEqual("https://accounts.mtls.google.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth.mtls.googleapis.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth.mtls.googleapis.com/revoke", initializer.RevokeTokenUrl);
            Assert.AreEqual("https://openidconnect.mtls.googleapis.com/v1/userinfo", initializer.UserInfoUrl.ToString());
        }

        [Test]
        public void WhenPscOverrideFound_ThenCreateOpenIdInitializerUsesPsc()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);

            var accountsEndpoint = new ServiceEndpoint<SignInClient.AuthorizationClient>(
                new Uri("https://accounts.google.com"),
                new Uri("https://accounts.mtls.google.com"))
            {
                PscHostOverride = "accounts.example.com"
            };
            var oauthEndpoint = new ServiceEndpoint<SignInClient.OAuthClient>(
                "https://oauth.googleapis.com")
            {
                PscHostOverride = "oauth.example.com"
            };
            var oidcEndpoint = new ServiceEndpoint<SignInClient.OpenIdClient>(
                "https://openidconnect.googleapis.com")
            {
                PscHostOverride = "openidconnect.example.com"
            };

            var initializer = Initializers.CreateOpenIdInitializer(
                accountsEndpoint,
                oauthEndpoint,
                oidcEndpoint,
                enrollment.Object);

            Assert.AreEqual("https://accounts.example.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth.example.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth.example.com/revoke", initializer.RevokeTokenUrl);
            Assert.AreEqual("https://openidconnect.example.com/v1/userinfo", initializer.UserInfoUrl.ToString());
        }
    }
}
