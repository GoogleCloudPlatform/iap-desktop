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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Auth.Gaia
{
    [TestFixture]
    public class TestGaiaCodeFlow
    {
        [Test]
        public void Initializer_WhenNotEnrolled_ThenFlowUsesTls(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var initializer = new GaiaCodeFlow.Initializer(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                TestProject.UserAgent);

            Assert.That(initializer.AuthorizationServerUrl, Is.EqualTo("https://accounts.google.com/o/oauth2/v2/auth"));
            Assert.That(initializer.TokenServerUrl, Is.EqualTo("https://oauth2.googleapis.com/token"));
            Assert.That(initializer.RevokeTokenUrl, Is.EqualTo("https://oauth2.googleapis.com/revoke"));
        }

        [Test]
        public void Initializer_WhenEnrolled_ThenFlowUsesMtls()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var initializer = new GaiaCodeFlow.Initializer(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                TestProject.UserAgent);

            Assert.That(initializer.AuthorizationServerUrl, Is.EqualTo("https://accounts.google.com/o/oauth2/v2/auth"));
            Assert.That(initializer.TokenServerUrl, Is.EqualTo("https://oauth2.mtls.googleapis.com/token"));
            Assert.That(initializer.RevokeTokenUrl, Is.EqualTo("https://oauth2.mtls.googleapis.com/revoke"));
        }
    }
}
