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

using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Persistence
{
    [TestFixture]
    public class TestApplicationSettingsRepository
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

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

            Assert.AreEqual(false, settings.IsMainWindowMaximized);
            Assert.AreEqual(0, settings.MainWindowHeight);
            Assert.AreEqual(0, settings.MainWindowWidth);
            Assert.AreEqual(true, settings.IsUpdateCheckEnabled);
            Assert.AreEqual(0, settings.LastUpdateCheck);
        }

        [Test]
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new ApplicationSettingsRepository(baseKey);

            repository.SetSettings(new ApplicationSettings()
            {
                IsMainWindowMaximized = true,
                MainWindowHeight = 480,
                MainWindowWidth = 640,
                IsUpdateCheckEnabled = false,
                LastUpdateCheck = 123
            });

            var settings = repository.GetSettings();

            Assert.IsTrue(settings.IsMainWindowMaximized);
            Assert.AreEqual(480, settings.MainWindowHeight);
            Assert.AreEqual(640, settings.MainWindowWidth);
            Assert.AreEqual(false, settings.IsUpdateCheckEnabled);
            Assert.AreEqual(123, settings.LastUpdateCheck);
        }
    }
}
