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

using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    [Category("DCA")]
    public class TestSecureConnectNativeHelper : FixtureBase
    {
        private string userIdWithEnrolledDevice = "";

        [Test]
        public async Task WhenHelperInstalled_ThenIsInstalledReturnsTrue()
        {
            var adapter = new SecureConnectAdapter();

            Assert.IsTrue(await adapter.IsInstalledAsync());
        }

        [Test]
        public async Task WhenUserIdDoesNotHaveDeviceEnrolled_ThenIsDeviceEnrolledForUserReturnsFalse()
        {
            var adapter = new SecureConnectAdapter();

            Assert.IsFalse(await adapter.IsDeviceEnrolledForUserAsync("111"));
        }

        [Test]
        public async Task WhenUserIdHasDeviceEnrolled_ThenIsDeviceEnrolledForUserReturnsTrue()
        {
            var adapter = new SecureConnectAdapter();

            Assert.IsTrue(await adapter.IsDeviceEnrolledForUserAsync(userIdWithEnrolledDevice));
        }

        [Test]
        public async Task WhenDeviceEnrolled_ThenGetDeviceInfoReturnsCerts()
        {
            var adapter = new SecureConnectAdapter();
            var info = await adapter.GetDeviceInfoAsync();

            Assert.IsNotNull(info.SerialNumber);
            CollectionAssert.IsNotEmpty(info.CertificateThumbprints);
        }
    }
}
