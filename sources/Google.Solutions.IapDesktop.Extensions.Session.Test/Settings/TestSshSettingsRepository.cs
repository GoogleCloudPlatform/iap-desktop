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
using Google.Solutions.Testing.Apis.Platform;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]

    public class TestSshSettingsRepository
    {
        [Test]
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.IsPropagateLocaleEnabled.Value = false;
                settings.PublicKeyValidity.Value = 3600;
                settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
                settings.IsFileAccessEnabled.Value = false;
                repository.SetSettings(settings);

                settings = repository.GetSettings();
                Assert.IsFalse(settings.IsPropagateLocaleEnabled.Value);
                Assert.AreEqual(3600, settings.PublicKeyValidity.Value);
                Assert.AreEqual(SshKeyType.EcdsaNistp256, settings.PublicKeyType.Value);
                Assert.IsFalse(settings.IsFileAccessEnabled.Value);
            }
        }

        //---------------------------------------------------------------------
        // IsPropagateLocaleEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsPropagateLocaleEnabled_WhenKeyEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
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
        public void PublicKeyType_WhenSchemaVersionIsOld_ThenPublicKeyTypeIsRsa()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Initial);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.Rsa3072, settings.PublicKeyType.Value);
            }
        }

        [Test]
        public void PublicKeyType_WhenSchemaVersionIsNew_ThenPublicKeyTypeIsEcdsa()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Version229);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp384, settings.PublicKeyType.Value);
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.PublicKeyType.Reset();

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => settings.PublicKeyType.Value = (SshKeyType)0xFF);
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp256, RegistryValueKind.DWord);
                userPolicyPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp384, RegistryValueKind.DWord);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp384, settings.PublicKeyType.Value);
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp256, RegistryValueKind.DWord);
                machinePolicyPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp521, RegistryValueKind.DWord);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp521, settings.PublicKeyType.Value);
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp256, RegistryValueKind.DWord);
                userPolicyPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp384, RegistryValueKind.DWord);
                machinePolicyPath.CreateKey().SetValue("PublicKeyType", SshKeyType.EcdsaNistp521, RegistryValueKind.DWord);

                var settings = repository.GetSettings();

                Assert.AreEqual(SshKeyType.EcdsaNistp521, settings.PublicKeyType.Value);
            }
        }

        //---------------------------------------------------------------------
        // PublicKeyValidity.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyValidity_WhenKeyEmpty_ThenPublicKeyValidityIs30days()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.AreEqual(60 * 60 * 24 * 30, settings.PublicKeyValidity.Value);
            }
        }

        [Test]
        public void PublicKeyValidity_WhenPublicKeyValidityInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
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
        public void PublicKeyValidity_WhenPublicKeyValidityValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("PublicKeyValidity", 60);
                userPolicyPath.CreateKey().SetValue("PublicKeyValidity", 2 * 60);

                var settings = repository.GetSettings();

                Assert.AreEqual(2 * 60, settings.PublicKeyValidity.Value);
            }
        }

        [Test]
        public void PublicKeyValidity_WhenPublicKeyValidityValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("PublicKeyValidity", 60);
                machinePolicyPath.CreateKey().SetValue("PublicKeyValidity", 3 * 60);

                var settings = repository.GetSettings();

                Assert.AreEqual(3 * 60, settings.PublicKeyValidity.Value);
            }
        }

        [Test]
        public void PublicKeyValidity_WhenPublicKeyValidityValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("PublicKeyValidity", 60);
                userPolicyPath.CreateKey().SetValue("PublicKeyValidity", 2 * 60);
                machinePolicyPath.CreateKey().SetValue("PublicKeyValidity", 3 * 60);

                var settings = repository.GetSettings();

                Assert.AreEqual(3 * 60, settings.PublicKeyValidity.Value);
            }
        }

        //---------------------------------------------------------------------
        // UsePersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void UsePersistentKey_WhenKeyEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.IsTrue(settings.UsePersistentKey.Value);
            }
        }

        //---------------------------------------------------------------------
        // IsFileAccessEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsFileAccessEnabled_WhenKeyEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.IsTrue(settings.IsFileAccessEnabled.Value);
            }
        }
    }
}
