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
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Settings
{
    [TestFixture]
    public class TestApplicationSettingsRepository : FixtureBase
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
            var repository = new ApplicationSettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.AreEqual(false, settings.IsMainWindowMaximized.Value);
            Assert.AreEqual(0, settings.MainWindowHeight.Value);
            Assert.AreEqual(0, settings.MainWindowWidth.Value);
            Assert.AreEqual(true, settings.IsUpdateCheckEnabled.Value);
            Assert.AreEqual(0, settings.LastUpdateCheck.Value);
        }

        [Test]
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new ApplicationSettingsRepository(baseKey);

            var settings = repository.GetSettings();
            settings.IsMainWindowMaximized.BoolValue = true;
            settings.MainWindowHeight.IntValue = 480;
            settings.MainWindowWidth.IntValue = 640;
            settings.IsUpdateCheckEnabled.BoolValue = false;
            settings.LastUpdateCheck.LongValue = 123L;
            repository.SetSettings(settings);

            settings = repository.GetSettings();

            Assert.AreEqual(true, settings.IsMainWindowMaximized.BoolValue);
            Assert.AreEqual(480, settings.MainWindowHeight.IntValue);
            Assert.AreEqual(640, settings.MainWindowWidth.IntValue);
            Assert.AreEqual(false, settings.IsUpdateCheckEnabled.BoolValue);
            Assert.AreEqual(123, settings.LastUpdateCheck.LongValue);
        }

        [Test]
        public void WhenProxyUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new ApplicationSettingsRepository(baseKey);

            var settings = repository.GetSettings();
            settings.ProxyUrl.Value = null;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => settings.ProxyUrl.Value = "thisisnotanurl");
        }

        [Test]
        public void WhenProxyPacUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new ApplicationSettingsRepository(baseKey);

            var settings = repository.GetSettings();
            settings.ProxyPacUrl.Value = null;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => settings.ProxyPacUrl.Value = "thisisnotanurl");
        }
    }
}
