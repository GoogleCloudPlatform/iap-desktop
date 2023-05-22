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
using Google.Apis.Compute.v1;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    public class TestAuthorizedClientInitializer : ApplicationFixtureBase
    {
        [Test]
        public void WhenNotEnrolled_ThenDeviceCertificateAuthenticationIsDisabled(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var initializer = new AuthorizedClientInitializer(
                new Mock<ICredential>().Object,
                enrollment.Object,
                ComputeEngineAdapter.MtlsBaseUri);

            var client = new ComputeService(initializer);
            Assert.IsFalse(client.IsDeviceCertificateAuthenticationEnabled());
        }

        [Test]
        public void WhenNotEnrolled_ThenDeviceCertificateAuthenticationIsDisabled()
        {
            var initializer = new AuthorizedClientInitializer(
                AuthorizationMocks.ForSecureConnectUser(),
                ComputeEngineAdapter.MtlsBaseUri);

            var client = new ComputeService(initializer);
            Assert.IsTrue(client.IsDeviceCertificateAuthenticationEnabled());
        }
    }
}
