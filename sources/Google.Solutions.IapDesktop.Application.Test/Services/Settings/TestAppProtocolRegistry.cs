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

using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Settings
{
    [TestFixture]
    public class TestAppProtocolRegistry : ApplicationFixtureBase
    {
        private const string TestScheme = "iapdesktop-test";

        [SetUp]
        public void SetUp()
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"SOFTWARE\Classes\{TestScheme}", false);
        }

        [Test]
        public void WhenProtocolNotRegistered_ThenIsRegisteredReturnsFalse()
        {
            var registry = new AppProtocolRegistry();
            Assert.IsFalse(registry.IsRegistered("unknown-scheme", "app.exe"));
        }

        [Test]
        public void WhenProtocolNotRegistered_ThenUnregisterDoesNothing()
        {
            var registry = new AppProtocolRegistry();
            registry.Unregister("unknown-scheme");
        }

        [Test]
        public void WhenProtocolRegisteredByDifferentApp_ThenIsRegisteredReturnsFalse()
        {
            var registry = new AppProtocolRegistry();
            registry.Register(TestScheme, "Test", "app.exe");
            registry.Unregister(TestScheme);

            Assert.IsFalse(registry.IsRegistered(TestScheme, "app.exe"));
        }

        [Test]
        public void WhenProtocolRegisteredByDifferentApp_TheRegisterOverridesRegistration()
        {
            var registry = new AppProtocolRegistry();
            registry.Register(TestScheme, "Test", "someotherapp.exe");
            registry.Register(TestScheme, "Test", "app.exe");

            Assert.IsTrue(registry.IsRegistered(TestScheme, "app.exe"));
        }

        [Test]
        public void WhenProtocolRegistered_ThenIsRegisteredReturnsTrue()
        {
            var registry = new AppProtocolRegistry();
            registry.Register(TestScheme, "Test", "app.exe");

            Assert.IsTrue(registry.IsRegistered(TestScheme, "app.exe"));
            Assert.IsFalse(registry.IsRegistered(TestScheme, "someotherapp.exe"));
        }
    }
}
