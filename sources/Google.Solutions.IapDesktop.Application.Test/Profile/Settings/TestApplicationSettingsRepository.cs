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
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestApplicationSettingsRepository : ApplicationFixtureBase
    {
        [Test]
        public void GetSettings_WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                var settings = repository.GetSettings();

                Assert.That(settings.IsMainWindowMaximized.Value, Is.EqualTo(false));
                Assert.That(settings.MainWindowHeight.Value, Is.EqualTo(0));
                Assert.That(settings.MainWindowWidth.Value, Is.EqualTo(0));
                Assert.That(settings.IsUpdateCheckEnabled.Value, Is.EqualTo(true));
                Assert.That(settings.LastUpdateCheck.Value, Is.EqualTo(0));
            }
        }

        [Test]
        public void GetSettings_WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
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

                Assert.That(settings.IsMainWindowMaximized.Value, Is.EqualTo(true));
                Assert.That(settings.MainWindowHeight.Value, Is.EqualTo(480));
                Assert.That(settings.MainWindowWidth.Value, Is.EqualTo(640));
                Assert.That(settings.IsUpdateCheckEnabled.Value, Is.EqualTo(false));
                Assert.That(settings.LastUpdateCheck.Value, Is.EqualTo(123));
            }
        }

        //---------------------------------------------------------------------
        // ProxyUrl.
        //---------------------------------------------------------------------

        [Test]
        public void ProxyUrl_WhenProxyUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
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
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("ProxyUrl", "http://setting");
                userPolicyPath.CreateKey().SetValue("ProxyUrl", "http://userpolicy");

                var settings = repository.GetSettings();

                Assert.That(settings.ProxyUrl.Value, Is.EqualTo("http://userpolicy"));
            }
        }

        [Test]
        public void ProxyUrl_WhenProxyUrlValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("ProxyUrl", "http://setting");
                machinePolicyPath.CreateKey().SetValue("ProxyUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.That(settings.ProxyUrl.Value, Is.EqualTo("http://machinepolicy"));
            }
        }

        [Test]
        public void ProxyUrl_WhenProxyUrlValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("ProxyUrl", "http://setting");
                userPolicyPath.CreateKey().SetValue("ProxyUrl", "http://userpolicy");
                machinePolicyPath.CreateKey().SetValue("ProxyUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.That(settings.ProxyUrl.Value, Is.EqualTo("http://machinepolicy"));
            }
        }

        //---------------------------------------------------------------------
        // ProxyPacUrl.
        //---------------------------------------------------------------------

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlInvalid_ThenSetValueThrowsArgumentOutOfRangeException()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
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
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("ProxyPacUrl", "http://setting");
                userPolicyPath.CreateKey().SetValue("ProxyPacUrl", "http://userpolicy");

                var settings = repository.GetSettings();

                Assert.That(settings.ProxyPacUrl.Value, Is.EqualTo("http://userpolicy"));
            }
        }

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("ProxyPacUrl", "http://setting");
                machinePolicyPath.CreateKey().SetValue("ProxyPacUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.That(settings.ProxyPacUrl.Value, Is.EqualTo("http://machinepolicy"));
            }
        }

        [Test]
        public void ProxyPacUrl_WhenProxyPacUrlValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("ProxyPacUrl", "http://setting");
                userPolicyPath.CreateKey().SetValue("ProxyPacUrl", "http://userpolicy");
                machinePolicyPath.CreateKey().SetValue("ProxyPacUrl", "http://machinepolicy");

                var settings = repository.GetSettings();

                Assert.That(settings.ProxyPacUrl.Value, Is.EqualTo("http://machinepolicy"));
            }
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValid_ThenSettingWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.That(settings.IsUpdateCheckEnabled.Value, Is.False);
                Assert.That(settings.IsUpdateCheckEnabled.IsDefault, Is.False);
            }
        }

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("IsUpdateCheckEnabled", 1);
                userPolicyPath.CreateKey().SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.That(settings.IsUpdateCheckEnabled.Value, Is.False);
                Assert.That(settings.IsUpdateCheckEnabled.IsDefault, Is.False);
            }
        }

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("IsUpdateCheckEnabled", 1);
                machinePolicyPath.CreateKey().SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.That(settings.IsUpdateCheckEnabled.Value, Is.False);
                Assert.That(settings.IsUpdateCheckEnabled.IsDefault, Is.False);
            }
        }

        [Test]
        public void IsUpdateCheckEnabled_WhenIsUpdateCheckEnabledValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                settingsPath.CreateKey().SetValue("IsUpdateCheckEnabled", 1);
                userPolicyPath.CreateKey().SetValue("IsUpdateCheckEnabled", 1);
                machinePolicyPath.CreateKey().SetValue("IsUpdateCheckEnabled", 0);

                var settings = repository.GetSettings();
                Assert.That(settings.IsUpdateCheckEnabled.Value, Is.False);
                Assert.That(settings.IsUpdateCheckEnabled.IsDefault, Is.False);
            }
        }

        //---------------------------------------------------------------------
        // IsPolicyPresent.
        //---------------------------------------------------------------------

        [Test]
        public void IsPolicyPresent_WhenMachineAnduserPolicyKeysAreNull_ThenIsPolicyPresentReturnsFalse()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current);

                Assert.That(repository.IsPolicyPresent, Is.False);
            }
        }

        [Test]
        public void IsPolicyPresent_WhenMachinePolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    null,
                    UserProfile.SchemaVersion.Current);

                Assert.That(repository.IsPolicyPresent, Is.True);
            }
        }

        [Test]
        public void IsPolicyPresent_WhenuserPolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Current);

                Assert.That(repository.IsPolicyPresent, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // IsTelemetryEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsTelemetryEnabled_WhenSchemaVersion240_ThenIsTelemetryEnabledDefaultsToTrue()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    userPolicyPath.CreateKey(),
                    UserProfile.SchemaVersion.Version240);

                Assert.That(repository.GetSettings().IsTelemetryEnabled.Value, Is.True);
            }
        }

        [Test]
        public void IsTelemetryEnabled_WhenSchemaVersion229OrBelow_ThenIsTelemetryEnabledDefaultsToTrue(
            [Values(UserProfile.SchemaVersion.Initial, UserProfile.SchemaVersion.Version229)]
            UserProfile.SchemaVersion schemaVersion)
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new ApplicationSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    userPolicyPath.CreateKey(),
                    schemaVersion);

                Assert.That(repository.GetSettings().IsTelemetryEnabled.Value, Is.False);
            }
        }
    }
}
