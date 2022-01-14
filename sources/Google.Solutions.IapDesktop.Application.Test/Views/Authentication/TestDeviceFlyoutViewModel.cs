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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Views.Authentication;
using Moq;
using NUnit.Framework;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Authentication
{
    [TestFixture]
    public class TestDeviceFlyoutViewModel : ApplicationFixtureBase
    {
        [Test]
        public void WhenNotInstalled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);

            var viewModel = new DeviceFlyoutViewModel(
                new Mock<IWin32Window>().Object,
                enrollment.Object);

            Assert.IsNotEmpty(viewModel.EnrollmentStateDescription);
            Assert.IsFalse(viewModel.IsDeviceEnrolledIconVisible);
            Assert.IsTrue(viewModel.IsDeviceNotEnrolledIconVisible);
            Assert.IsTrue(viewModel.IsDetailsLinkVisible);
            Assert.IsNotEmpty(viewModel.DetailsLinkCaption);
        }

        [Test]
        public void WhenNotEnrolled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            var viewModel = new DeviceFlyoutViewModel(
                new Mock<IWin32Window>().Object,
                enrollment.Object);

            Assert.IsNotEmpty(viewModel.EnrollmentStateDescription);
            Assert.IsFalse(viewModel.IsDeviceEnrolledIconVisible);
            Assert.IsTrue(viewModel.IsDeviceNotEnrolledIconVisible);
            Assert.IsTrue(viewModel.IsDetailsLinkVisible);
            Assert.IsNotEmpty(viewModel.DetailsLinkCaption);
        }

        [Test]
        public void WhenEnrolled_ThenPropertiesAreSetAccordingly()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var viewModel = new DeviceFlyoutViewModel(
                new Mock<IWin32Window>().Object,
                enrollment.Object);

            Assert.IsNotEmpty(viewModel.EnrollmentStateDescription);
            Assert.IsTrue(viewModel.IsDeviceEnrolledIconVisible);
            Assert.IsFalse(viewModel.IsDeviceNotEnrolledIconVisible);
            Assert.IsTrue(viewModel.IsDetailsLinkVisible);
            Assert.IsNotEmpty(viewModel.DetailsLinkCaption);
        }
    }
}
