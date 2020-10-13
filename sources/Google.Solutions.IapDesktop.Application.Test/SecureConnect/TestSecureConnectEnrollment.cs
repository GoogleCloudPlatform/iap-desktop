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

using Google.Solutions.IapDesktop.Application.SecureConnect;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.SecureConnect
{
    [TestFixture]
    public class TestSecureConnectEnrollment
    {
        [SetUp]
        public void SetUp()
        {
            SecureConnectNativeHelper.Disabled = false;
        }

        [Test]
        public async Task WhenDisabled_ThenStatusIsNotInstalled()
        {
            SecureConnectNativeHelper.Disabled = true;

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync("1");
            Assert.AreEqual(DeviceEnrollmentState.NotInstalled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUserIdUnknown_ThenDeviceIsNotEnrolled()
        {
            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync("1");
            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUserIdKnown_ThenDeviceIsEnrolled()
        {
            // TODO: use known user ID

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync("1");
            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);
            Assert.AreEqual("Google Endpoint Verification", enrollment.Certificate.Subject);
        }

        [Test]
        public async Task WhenSwitchingUserIdsFromKnownToUnknown_ThenRefreshUpdatesStateToNotEnrolled()
        {
            // TODO: use enrolled ID

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync("1");
            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);

            await enrollment.RefreshAsync("1");
            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }
    }
}
