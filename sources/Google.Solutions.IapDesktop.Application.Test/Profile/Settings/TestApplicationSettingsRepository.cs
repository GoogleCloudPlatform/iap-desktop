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

using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestApplicationSettingsRepository : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestMachinePolicyKeyPath = @"Software\Google\__TestMachinePolicy";
        private const string TestUserPolicyKeyPath = @"Software\Google\__TestUserPolicy";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.hkcu.DeleteSubKeyTree(TestMachinePolicyKeyPath, false);
            this.hkcu.DeleteSubKeyTree(TestUserPolicyKeyPath, false);
        }

        [Test]
        public void GetSettings_WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();

                Assert.AreEqual(false, settings.IsMainWindowMaximized.Value);
                Assert.AreEqual(0, settings.MainWindowHeight.Value);
                Assert.AreEqual(0, settings.MainWindowWidth.Value);
                Assert.AreEqual(true, settings.IsUpdateCheckEnabled.Value);
                Assert.AreEqual(0, settings.LastUpdateCheck.Value);
            }
        }

        [Test]
        public void GetSettings_WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.IsMainWindowMaximized.Value = true;
                settings.MainWindowHeight.Value = 480;
                settings.MainWindowWidth.Value = 640;
                settings.IsUpdateCheckEnabled.Value = false;
                settings.LastUpdateCheck.Value = 123L;
                repository.SetSettings(settings);

                settings = repository.GetSettings();

                Assert.AreEqual(true, settings.IsMainWindowMaximized.Value);
                Assert.AreEqual(480, settings.MainWindowHeight.Value);
                Assert.AreEqual(640, settings.MainWindowWidth.Value);
                Assert.AreEqual(false, settings.IsUpdateCheckEnabled.Value);
                Assert.AreEqual(123, settings.LastUpdateCheck.Value);
            }
        }

        //---------------------------------------------------------------------
        // ProxyUrl.
        //---------------------------------------------------------------------

        [Test]
        public void ProxyUrl_WhenProxyUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.ProxyUrl.Value = null;

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.ProxyUrl.Value = "thisisnotanurl");
            }
        }

        [Test]
        public void ProxyUrl_WhenProxyUrlValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("ProxyUrl", "http://setting");
                userPolicyKey.SetValue("ProxyUrl", "http://userpolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://userpolicy", settings.ProxyUrl.Value);
            }
        }

        [Test]
        public void ProxyUrl_WhenProxyUrlValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("ProxyUrl", "http://setting");
                machinePolicyKey.SetValue("ProxyUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyUrl.Value);
            }
        }

        [Test]
        public void ProxyUrl_WhenProxyUrlValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("ProxyUrl", "http://setting");
                userPolicyKey.SetValue("ProxyUrl", "http://userpolicy");
                machinePolicyKey.SetValue("ProxyUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyUrl.Value);
            }
        }

        //---------------------------------------------------------------------
        // ProxyPacUrl.
        //---------------------------------------------------------------------

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.ProxyPacUrl.Value = null;

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.ProxyPacUrl.Value = "thisisnotanurl");
            }
        }

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("ProxyPacUrl", "http://setting");
                userPolicyKey.SetValue("ProxyPacUrl", "http://userpolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://userpolicy", settings.ProxyPacUrl.Value);
            }
        }

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("ProxyPacUrl", "http://setting");
                machinePolicyKey.SetValue("ProxyPacUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyPacUrl.Value);
            }
        }

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("ProxyPacUrl", "http://setting");
                userPolicyKey.SetValue("ProxyPacUrl", "http://userpolicy");
                machinePolicyKey.SetValue("ProxyPacUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyPacUrl.Value);
            }
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValid_ThenSettingWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.Value);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("IsUpdateCheckEnabled", 1);
                userPolicyKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.Value);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("IsUpdateCheckEnabled", 1);
                machinePolicyKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.Value);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("IsUpdateCheckEnabled", 1);
                userPolicyKey.SetValue("IsUpdateCheckEnabled", 1);
                machinePolicyKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.Value);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // IsPolicyPresent.
        //---------------------------------------------------------------------

        [Test]
        public void IsPolicyPresent_WhenMachineAndUserPolicyKeysAreNull_ThenIsPolicyPresentReturnsFalse()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                Assert.IsFalse(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void IsPolicyPresent_WhenMachinePolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    policyKey,
                    null,
                    UserProfile.SchemaVersion.Current);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void IsPolicyPresent_WhenUserPolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    policyKey,
                    UserProfile.SchemaVersion.Current);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }

        //---------------------------------------------------------------------
        // IsTelemetryEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsTelemetryEnabled_WhenSchemaVersion240_ThenIsTelemetryEnabledDefaultsToTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    policyKey,
                    UserProfile.SchemaVersion.Version240);

                Assert.IsTrue(repository.GetSettings().IsTelemetryEnabled.Value);
            }
        }

        [Test]
        public void IsTelemetryEnabled_WhenSchemaVersion229OrBelow_ThenIsTelemetryEnabledDefaultsToTrue(
            [Values(UserProfile.SchemaVersion.Initial, UserProfile.SchemaVersion.Version229)]
            UserProfile.SchemaVersion schemaVersion)
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    policyKey,
                    schemaVersion);

                Assert.IsFalse(repository.GetSettings().IsTelemetryEnabled.Value);
            }
        }
    }
}
