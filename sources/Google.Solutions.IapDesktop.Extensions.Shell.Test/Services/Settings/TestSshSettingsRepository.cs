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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Settings
{
    [TestFixture]
    
    public class TestSshSettingsRepository : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new SshSettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.IsTrue(settings.IsPropagateLocaleEnabled.BoolValue);
            Assert.AreEqual(60 * 60 * 24 * 30, settings.PublicKeyValidity.IntValue);
        }
    }
}
