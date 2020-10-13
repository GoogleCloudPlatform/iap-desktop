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
    public class TestSecureConnectAdapter
    {
        [Test]
        public async Task WhenUserIdUnknown_ThenDeviceIsNotEnrolled()
        {
            if (!SecureConnectNativeHelper.IsInstalled)
            {
                Assert.Inconclusive("Not installed");
                return;
            }

            var adapter = new SecureConnectAdapter();
            var info = await adapter.GetEnrollmentInfoAsync("1");
            Assert.IsNotNull(info);
            Assert.IsFalse(info.IsEnrolled);
            Assert.IsNull(info.DeviceCertificate);
        }

        [Test]
        public async Task WhenUserIdKnown_ThenDeviceIsEnrolled()
        {
            if (!SecureConnectNativeHelper.IsInstalled)
            {
                Assert.Inconclusive("Not installed");
                return;
            }

            var adapter = new SecureConnectAdapter();
            var info = await adapter.GetEnrollmentInfoAsync("1");
            Assert.IsNotNull(info);
            Assert.IsTrue(info.IsEnrolled);
            Assert.IsNotNull(info.DeviceCertificate);
            Assert.AreEqual("Google Endpoint Verification", info.DeviceCertificate.Subject);
        }
    }
}
