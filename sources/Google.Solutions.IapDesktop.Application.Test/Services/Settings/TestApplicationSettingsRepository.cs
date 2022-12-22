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

using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Settings
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
        public void WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, null, null);

                var settings = repository.GetSettings();

                Assert.AreEqual(false, settings.IsMainWindowMaximized.Value);
                Assert.AreEqual(0, settings.MainWindowHeight.Value);
                Assert.AreEqual(0, settings.MainWindowWidth.Value);
                Assert.AreEqual(true, settings.IsUpdateCheckEnabled.Value);
                Assert.AreEqual(0, settings.LastUpdateCheck.Value);
            }
        }

        [Test]
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, null, null);

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
        }

        //---------------------------------------------------------------------
        // ProxyUrl.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProxyUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, null, null);

                var settings = repository.GetSettings();
                settings.ProxyUrl.Value = null;

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.ProxyUrl.Value = "thisisnotanurl");
            }
        }

        [Test]
        public void WhenProxyUrlValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ProxyUrl", "http://setting");
                userPolicyKey.SetValue("ProxyUrl", "http://userpolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://userpolicy", settings.ProxyUrl.StringValue);
            }
        }

        [Test]
        public void WhenProxyUrlValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ProxyUrl", "http://setting");
                machinePolicyKey.SetValue("ProxyUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyUrl.StringValue);
            }
        }

        [Test]
        public void WhenProxyUrlValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ProxyUrl", "http://setting");
                userPolicyKey.SetValue("ProxyUrl", "http://userpolicy");
                machinePolicyKey.SetValue("ProxyUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyUrl.StringValue);
            }
        }

        //---------------------------------------------------------------------
        // ProxyPacUrl.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProxyPacUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, null, null);

                var settings = repository.GetSettings();
                settings.ProxyPacUrl.Value = null;

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.ProxyPacUrl.Value = "thisisnotanurl");
            }
        }

        [Test]
        public void WhenProxyPacUrlValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ProxyPacUrl", "http://setting");
                userPolicyKey.SetValue("ProxyPacUrl", "http://userpolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://userpolicy", settings.ProxyPacUrl.StringValue);
            }
        }

        [Test]
        public void WhenProxyPacUrlValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ProxyPacUrl", "http://setting");
                machinePolicyKey.SetValue("ProxyPacUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyPacUrl.StringValue);
            }
        }

        [Test]
        public void WhenProxyPacUrlValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ProxyPacUrl", "http://setting");
                userPolicyKey.SetValue("ProxyPacUrl", "http://userpolicy");
                machinePolicyKey.SetValue("ProxyPacUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.AreEqual("http://machinepolicy", settings.ProxyPacUrl.StringValue);
            }
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenIsUpdateCheckEnabledValid_ThenSettingWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null);

                settingsKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.BoolValue);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        [Test]
        public void WhenIsUpdateCheckEnabledValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("IsUpdateCheckEnabled", 1);
                userPolicyKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.BoolValue);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        [Test]
        public void WhenIsUpdateCheckEnabledValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("IsUpdateCheckEnabled", 1);
                machinePolicyKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.BoolValue);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        [Test]
        public void WhenIsUpdateCheckEnabledValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("IsUpdateCheckEnabled", 1);
                userPolicyKey.SetValue("IsUpdateCheckEnabled", 1);
                machinePolicyKey.SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.IsFalse(settings.IsUpdateCheckEnabled.BoolValue);
                Assert.IsFalse(settings.IsUpdateCheckEnabled.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // IsDeviceCertificateAuthenticationEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenIsDeviceCertificateAuthenticationEnabledValid_ThenSettingWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null);

                settingsKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        [Test]
        public void WhenIsDeviceCertificateAuthenticationEnabledValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                userPolicyKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        [Test]
        public void WhenIsDeviceCertificateAuthenticationEnabledValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                machinePolicyKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        [Test]
        public void WhenIsDeviceCertificateAuthenticationEnabledValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                userPolicyKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                machinePolicyKey.SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }


        //---------------------------------------------------------------------
        // ConnectionLimit.
        //---------------------------------------------------------------------

        [Test]
        public void WhenConnectionLimitValid_ThenSettingWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    null,
                    null);

                settingsKey.SetValue("ConnectionLimit", 8);

                var settings = repository.GetSettings();
                Assert.AreEqual(8, settings.ConnectionLimit.IntValue);
                Assert.IsFalse(settings.ConnectionLimit.IsDefault);
            }
        }

        [Test]
        public void WhenConnectionLimitValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ConnectionLimit", 8);
                userPolicyKey.SetValue("ConnectionLimit", 9);

                var settings = repository.GetSettings();
                Assert.AreEqual(9, settings.ConnectionLimit.IntValue);
                Assert.IsFalse(settings.ConnectionLimit.IsDefault);
            }
        }

        [Test]
        public void WhenConnectionLimitValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ConnectionLimit", 8);
                machinePolicyKey.SetValue("ConnectionLimit", 9);

                var settings = repository.GetSettings();
                Assert.AreEqual(9, settings.ConnectionLimit.IntValue);
                Assert.IsFalse(settings.ConnectionLimit.IsDefault);
            }
        }

        [Test]
        public void WhenConnectionLimitValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("ConnectionLimit", 8);
                userPolicyKey.SetValue("ConnectionLimit", 9);
                machinePolicyKey.SetValue("ConnectionLimit", 10);

                var settings = repository.GetSettings();
                Assert.AreEqual(10, settings.ConnectionLimit.IntValue);
                Assert.IsFalse(settings.ConnectionLimit.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // IsPolicyPresent.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMachineAndUserPolicyKeysAreNull_ThenIsPolicyPresentReturnsFalse()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, null, null);

                Assert.IsFalse(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void WhenMachinePolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, policyKey, null);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void WhenUserPolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new ApplicationSettingsRepository(settingsKey, null, policyKey);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }
    }
}
