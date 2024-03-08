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
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Ssh.Cryptography;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]

    public class TestSshSettingsRepository
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
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.IsPropagateLocaleEnabled.Value = false;
                settings.PublicKeyValidity.Value = 3600;
                settings.PublicKeyType.EnumValue = SshKeyType.EcdsaNistp256;
                repository.SetSettings(settings);

                settings = repository.GetSettings();
                Assert.IsFalse(settings.IsPropagateLocaleEnabled.Value);
                Assert.AreEqual(3600, settings.PublicKeyValidity.Value);
                Assert.AreEqual(SshKeyType.EcdsaNistp256, settings.PublicKeyType.EnumValue);
            }
        }

        //---------------------------------------------------------------------
        // IsPropagateLocaleEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyEmpty_ThenIsPropagateLocaleEnabledIsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.IsTrue(settings.IsPropagateLocaleEnabled.Value);
            }
        }

        //---------------------------------------------------------------------
        // PublicKeyType.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSchemaVersionIsOld_ThenPublicKeyTypeIsRsa()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Initial);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.Rsa3072, settings.PublicKeyType.Value);
            }
        }

        [Test]
        public void WhenSchemaVersionIsNew_ThenPublicKeyTypeIsEcdsa()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Version229);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp384, settings.PublicKeyType.Value);
            }
        }

        [Test]
        public void WhenPublicKeyTypeInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.PublicKeyType.Reset();

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.PublicKeyType.EnumValue = (SshKeyType)0xFF);
            }
        }

        [Test]
        public void WhenPublicKeyTypeValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp256, RegistryValueKind.DWord);
                userPolicyKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp384, RegistryValueKind.DWord);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp384, settings.PublicKeyType.EnumValue);
            }
        }

        [Test]
        public void WhenPublicKeyTypeValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp256, RegistryValueKind.DWord);
                machinePolicyKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp521, RegistryValueKind.DWord);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp521, settings.PublicKeyType.EnumValue);
            }
        }

        [Test]
        public void WhenPublicKeyTypeValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp256, RegistryValueKind.DWord);
                userPolicyKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp384, RegistryValueKind.DWord);
                machinePolicyKey.SetValue("PublicKeyType", SshKeyType.EcdsaNistp521, RegistryValueKind.DWord);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp521, settings.PublicKeyType.EnumValue);
            }
        }

        //---------------------------------------------------------------------
        // PublicKeyValidity.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyEmpty_ThenPublicKeyValidityIs30days()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.AreEqual(60 * 60 * 24 * 30, settings.PublicKeyValidity.Value);
            }
        }

        [Test]
        public void WhenPublicKeyValidityInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.PublicKeyValidity.Reset();

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.PublicKeyValidity.Value = 5);
            }
        }

        [Test]
        public void WhenPublicKeyValidityValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("PublicKeyValidity", 60);
                userPolicyKey.SetValue("PublicKeyValidity", 2 * 60);

                var settings = repository.GetSettings();

                Assert.AreEqual(2 * 60, settings.PublicKeyValidity.Value);
            }
        }

        [Test]
        public void WhenPublicKeyValidityValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("PublicKeyValidity", 60);
                machinePolicyKey.SetValue("PublicKeyValidity", 3 * 60);

                var settings = repository.GetSettings();

                Assert.AreEqual(3 * 60, settings.PublicKeyValidity.Value);
            }
        }

        [Test]
        public void WhenPublicKeyValidityValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey,
                    UserProfile.SchemaVersion.Current);

                settingsKey.SetValue("PublicKeyValidity", 60);
                userPolicyKey.SetValue("PublicKeyValidity", 2 * 60);
                machinePolicyKey.SetValue("PublicKeyValidity", 3 * 60);

                var settings = repository.GetSettings();

                Assert.AreEqual(3 * 60, settings.PublicKeyValidity.Value);
            }
        }

        //---------------------------------------------------------------------
        // UsePersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyEmpty_ThenUsePersistentKeyIsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new SshSettingsRepository(
                    settingsKey,
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.IsTrue(settings.UsePersistentKey.Value);
            }
        }
    }
}
