﻿//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Testing.Apis.Platform;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestAccessSettingsRepository
    {
        [Test]
        public void GetSettings_WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                var settings = repository.GetSettings();

                Assert.IsNull(settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.Value);
                StringAssert.Contains(
                    "Google Endpoint Verification",
                    settings.DeviceCertificateSelector.Value);
                Assert.AreEqual(16, settings.ConnectionLimit.Value);
            }
        }

        [Test]
        public void GetSettings_WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                var settings = repository.GetSettings();
                settings.IsDeviceCertificateAuthenticationEnabled.Value = true;
                settings.PrivateServiceConnectEndpoint.Value = "psc.example.com";
                settings.DeviceCertificateSelector.Value = "{}";
                repository.SetSettings(settings);

                settings = repository.GetSettings();

                Assert.AreEqual("psc.example.com", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.Value);
                Assert.AreEqual("{}", settings.DeviceCertificateSelector.Value);
            }
        }

        //---------------------------------------------------------------------
        // PrivateServiceConnectEndpoint.
        //---------------------------------------------------------------------

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointValid_ThenSettingWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                settingsPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                settingsPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc");
                userPolicyPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc-user");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc-user", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                settingsPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc");
                machinePolicyPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc-machine");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc-machine", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                settingsPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc");
                userPolicyPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc-user");
                machinePolicyPath.CreateKey().SetValue("PrivateServiceConnectEndpoint", "psc-machine");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc-machine", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // IsDeviceCertificateAuthenticationEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsDeviceCertificateAuthenticationEnabled_WhenIsDeviceCertificateAuthenticationEnabledValid_ThenSettingWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                settingsPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.Value);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        [Test]
        public void IsDeviceCertificateAuthenticationEnabled_WhenIsDeviceCertificateAuthenticationEnabledValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                settingsPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                userPolicyPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.Value);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        [Test]
        public void IsDeviceCertificateAuthenticationEnabled_WhenIsDeviceCertificateAuthenticationEnabledValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                settingsPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                machinePolicyPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.Value);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        [Test]
        public void IsDeviceCertificateAuthenticationEnabled_WhenIsDeviceCertificateAuthenticationEnabledValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                settingsPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                userPolicyPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 0);
                machinePolicyPath.CreateKey().SetValue("IsDeviceCertificateAuthenticationEnabled", 1);

                var settings = repository.GetSettings();
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.Value);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // ConnectionLimit.
        //---------------------------------------------------------------------

        [Test]
        public void ConnectionLimit_WhenConnectionLimitValid_ThenSettingWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                settingsPath.CreateKey().SetValue("ConnectionLimit", 8);

                var settings = repository.GetSettings();
                Assert.AreEqual(8, settings.ConnectionLimit.Value);
                Assert.IsFalse(settings.ConnectionLimit.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // WorkforcePoolProvider.
        //---------------------------------------------------------------------

        [Test]
        public void WorkforcePoolProvider_WhenWorkforcePoolProviderValid_ThenSettingWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                settingsPath.CreateKey().SetValue("WorkforcePoolProvider", provider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(provider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        [Test]
        public void WorkforcePoolProvider_WhenWorkforcePoolProviderValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                var userProvider = new WorkforcePoolProviderLocator("global", "pool", "user-provider");
                settingsPath.CreateKey().SetValue("WorkforcePoolProvider", provider.ToString());
                userPolicyPath.CreateKey().SetValue("WorkforcePoolProvider", userProvider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(userProvider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        [Test]
        public void WorkforcePoolProvider_WhenWorkforcePoolProviderValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                var machineProvider = new WorkforcePoolProviderLocator("global", "pool", "machine-provider");
                settingsPath.CreateKey().SetValue("WorkforcePoolProvider", provider.ToString());
                machinePolicyPath.CreateKey().SetValue("WorkforcePoolProvider", machineProvider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(machineProvider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        [Test]
        public void WorkforcePoolProvider_WhenWorkforcePoolProviderValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                var userProvider = new WorkforcePoolProviderLocator("global", "pool", "user-provider");
                var machineProvider = new WorkforcePoolProviderLocator("global", "pool", "machine-provider");
                settingsPath.CreateKey().SetValue("WorkforcePoolProvider", provider.ToString());
                userPolicyPath.CreateKey().SetValue("WorkforcePoolProvider", userProvider.ToString());
                machinePolicyPath.CreateKey().SetValue("WorkforcePoolProvider", machineProvider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(machineProvider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // IsPolicyPresent.
        //---------------------------------------------------------------------

        [Test]
        public void IsPolicyPresent_WhenMachineAndUserPolicyKeysAreNull_ThenIsPolicyPresentReturnsFalse()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                Assert.IsFalse(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void IsPolicyPresent_WhenMachinePolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(), 
                    machinePolicyPath.CreateKey(),
                    null);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void IsPolicyPresent_WhenUserPolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    userPolicyPath.CreateKey());

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }
    }
}
