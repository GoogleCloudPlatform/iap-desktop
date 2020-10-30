﻿//
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
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.SecureConnect
{
    [TestFixture]
    public class TestSecureConnectNativeHelper
    {
        [SetUp]
        public void SetUp()
        {
            SecureConnectNativeHelper.Disabled = false;
        }

        [Test]
        public void WhenHelperDisabled_ThenPingThrowsSecureConnectException()
        {
            SecureConnectNativeHelper.Disabled = true;
            Assert.Throws<SecureConnectException>(() => new SecureConnectNativeHelper().Ping());
        }

        [Test]
        public void WhenHelperInstalled_ThenPingSucceeds()
        {
            new SecureConnectNativeHelper().Ping();
        }

        [Test]
        public void WhenUserIdUnknown_ThenShouldEnrollDeviceReturnsTrue()
        {
            Assert.IsTrue(new SecureConnectNativeHelper().ShouldEnrollDevice("1"));
        }

        [Test]
        public void WhenUserIdKnown_ThenShouldEnrollDeviceReturnsFalse()
        {
            // TODO: use enrolled ID
            Assert.IsFalse(new SecureConnectNativeHelper().ShouldEnrollDevice("TODO"));
        }

        [Test]
        public void WhenDeviceEnrolled_ThenGetDeviceInfoReturnsCerts()
        {
            var info = new SecureConnectNativeHelper().GetDeviceInfo();

            Assert.IsNotNull(info.SerialNumber);
            CollectionAssert.IsNotEmpty(info.CertificateFingerprints);
        }
    }
}
