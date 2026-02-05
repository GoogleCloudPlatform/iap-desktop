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
        public void WhenSettingsSaved()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();
                settings.EnableLocalePropagation.Value = false;
                settings.PublicKeyValidity.Value = 3600;
                settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
                settings.EnableFileAccess.Value = false;
                repository.SetSettings(settings);

                settings = repository.GetSettings();
                Assert.That(settings.EnableLocalePropagation.Value, Is.False);
                Assert.That(settings.PublicKeyValidity.Value, Is.EqualTo(3600));
                Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp256));
                Assert.That(settings.EnableFileAccess.Value, Is.False);
            }
        }

        //---------------------------------------------------------------------
        // EnableLocalePropagation.
        //---------------------------------------------------------------------

        [Test]
        public void EnableLocalePropagation_WhenKeyEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.IsTrue(settings.EnableLocalePropagation.Value);
            }
        }

        //---------------------------------------------------------------------
        // PublicKeyType.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyType_WhenSchemaVersionIsOld()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Initial);

                var settings = repository.GetSettings();

                Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.Rsa3072));
            }
        }

        [Test]
        public void PublicKeyType_WhenSchemaVersionIsNew()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Version229);

                var settings = repository.GetSettings();

                Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp384));
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeInvalid()
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
        public void PublicKeyType_WhenPublicKeyTypeValidAndUserPolicySet()
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

                Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp384));
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeValidAndMachinePolicySet()
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

                Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp521));
            }
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeValidAndUserAndMachinePolicySet()
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

                Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp521));
            }
        }

        //---------------------------------------------------------------------
        // PublicKeyValidity.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyValidity_WhenKeyEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.That(settings.PublicKeyValidity.Value, Is.EqualTo(60 * 60 * 24 * 30));
            }
        }

        [Test]
        public void PublicKeyValidity_WhenPublicKeyValidityInvalid()
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
        public void PublicKeyValidity_WhenPublicKeyValidityValidAndUserPolicySet()
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

                Assert.That(settings.PublicKeyValidity.Value, Is.EqualTo(2 * 60));
            }
        }

        [Test]
        public void PublicKeyValidity_WhenPublicKeyValidityValidAndMachinePolicySet()
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

                Assert.That(settings.PublicKeyValidity.Value, Is.EqualTo(3 * 60));
            }
        }

        [Test]
        public void PublicKeyValidity_WhenPublicKeyValidityValidAndUserAndMachinePolicySet()
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

                Assert.That(settings.PublicKeyValidity.Value, Is.EqualTo(3 * 60));
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
        // EnableFileAccess.
        //---------------------------------------------------------------------

        [Test]
        public void EnableFileAccess_WhenKeyEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SshSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);
                var settings = repository.GetSettings();

                Assert.IsTrue(settings.EnableFileAccess.Value);
            }
        }
    }
}
