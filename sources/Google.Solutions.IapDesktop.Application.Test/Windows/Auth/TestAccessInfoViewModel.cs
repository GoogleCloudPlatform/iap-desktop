//
// Copyright 2020 Google LLC
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
using Google.Solutions.Apis.Client;
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using Google.Solutions.IapDesktop.Application.Host.Diagnostics;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Auth
{
    [TestFixture]
    public class TestAccessInfoViewModel : ApplicationFixtureBase
    {
        private static IAuthorization CreateAuthorization(
            IDeviceEnrollment enrollment)
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.DeviceEnrollment)
                .Returns(enrollment);

            return authorization.Object;
        }

        [Test]
        public void WhenPscEnabled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);

            var viewModel = new AccessInfoViewModel(
                CreateAuthorization(enrollment.Object),
                new ServiceRoute("endpoint"),
                new HelpClient());

            Assert.AreEqual("Enabled", viewModel.PrivateServiceConnectText);
            Assert.AreEqual("Disabled", viewModel.DeviceCertificateLinkText);
        }

        [Test]
        public void WhenNotInstalled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);

            var viewModel = new AccessInfoViewModel(
                CreateAuthorization(enrollment.Object),
                ServiceRoute.Public,
                new HelpClient());

            Assert.AreEqual("Disabled", viewModel.PrivateServiceConnectText);
            Assert.AreEqual("Disabled", viewModel.DeviceCertificateLinkText);
        }

        [Test]
        public void WhenNotEnrolled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            var viewModel = new AccessInfoViewModel(
                CreateAuthorization(enrollment.Object),
                ServiceRoute.Public,
                new HelpClient());

            Assert.AreEqual("Disabled", viewModel.PrivateServiceConnectText);
            Assert.AreEqual("Error", viewModel.DeviceCertificateLinkText);
        }

        [Test]
        public void WhenEnrolled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var viewModel = new AccessInfoViewModel(
                CreateAuthorization(enrollment.Object),
                ServiceRoute.Public,
                new HelpClient());

            Assert.AreEqual("Disabled", viewModel.PrivateServiceConnectText);
            Assert.AreEqual("Enabled", viewModel.DeviceCertificateLinkText);
        }
    }
}
