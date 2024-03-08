//
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
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestAccessSettingsRepository
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
                var repository = new AccessSettingsRepository(settingsKey, null, null);

                var settings = repository.GetSettings();

                Assert.IsNull(settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
                StringAssert.Contains(
                    "Google Endpoint Verification",
                    settings.DeviceCertificateSelector.Value);
                Assert.AreEqual(16, settings.ConnectionLimit.IntValue);
            }
        }

        [Test]
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new AccessSettingsRepository(settingsKey, null, null);

                var settings = repository.GetSettings();
                settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
                settings.PrivateServiceConnectEndpoint.Value = "psc.example.com";
                settings.DeviceCertificateSelector.Value = "{}";
                repository.SetSettings(settings);

                settings = repository.GetSettings();

                Assert.AreEqual("psc.example.com", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsTrue(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
                Assert.AreEqual("{}", settings.DeviceCertificateSelector.Value);
            }
        }

        //---------------------------------------------------------------------
        // PrivateServiceConnectEndpoint.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPrivateServiceConnectEndpointValid_ThenSettingWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    null,
                    null);

                settingsKey.SetValue("PrivateServiceConnectEndpoint", "psc");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        [Test]
        public void WhenPrivateServiceConnectEndpointValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("PrivateServiceConnectEndpoint", "psc");
                userPolicyKey.SetValue("PrivateServiceConnectEndpoint", "psc-user");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc-user", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        [Test]
        public void WhenPrivateServiceConnectEndpointValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("PrivateServiceConnectEndpoint", "psc");
                machinePolicyKey.SetValue("PrivateServiceConnectEndpoint", "psc-machine");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc-machine", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
            }
        }

        [Test]
        public void WhenPrivateServiceConnectEndpointValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                settingsKey.SetValue("PrivateServiceConnectEndpoint", "psc");
                userPolicyKey.SetValue("PrivateServiceConnectEndpoint", "psc-user");
                machinePolicyKey.SetValue("PrivateServiceConnectEndpoint", "psc-machine");

                var settings = repository.GetSettings();
                Assert.AreEqual("psc-machine", settings.PrivateServiceConnectEndpoint.Value);
                Assert.IsFalse(settings.PrivateServiceConnectEndpoint.IsDefault);
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
                var repository = new AccessSettingsRepository(
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
                var repository = new AccessSettingsRepository(
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
                var repository = new AccessSettingsRepository(
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
                var repository = new AccessSettingsRepository(
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
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    null,
                    null);

                settingsKey.SetValue("ConnectionLimit", 8);

                var settings = repository.GetSettings();
                Assert.AreEqual(8, settings.ConnectionLimit.IntValue);
                Assert.IsFalse(settings.ConnectionLimit.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // WorkforcePoolProvider.
        //---------------------------------------------------------------------

        [Test]
        public void WhenWorkforcePoolProviderValid_ThenSettingWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var repository = new AccessSettingsRepository(settingsKey, null, null);

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                settingsKey.SetValue("WorkforcePoolProvider", provider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(provider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        [Test]
        public void WhenWorkforcePoolProviderValidAndUserPolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                var userProvider = new WorkforcePoolProviderLocator("global", "pool", "user-provider");
                settingsKey.SetValue("WorkforcePoolProvider", provider.ToString());
                userPolicyKey.SetValue("WorkforcePoolProvider", userProvider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(userProvider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        [Test]
        public void WhenWorkforcePoolProviderValidAndMachinePolicySet_ThenPolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                var machineProvider = new WorkforcePoolProviderLocator("global", "pool", "machine-provider");
                settingsKey.SetValue("WorkforcePoolProvider", provider.ToString());
                machinePolicyKey.SetValue("WorkforcePoolProvider", machineProvider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(machineProvider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
            }
        }

        [Test]
        public void WhenWorkforcePoolProviderValidAndUserAndMachinePolicySet_ThenMachinePolicyWins()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var machinePolicyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            using (var userPolicyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(
                    settingsKey,
                    machinePolicyKey,
                    userPolicyKey);

                var provider = new WorkforcePoolProviderLocator("global", "pool", "provider");
                var userProvider = new WorkforcePoolProviderLocator("global", "pool", "user-provider");
                var machineProvider = new WorkforcePoolProviderLocator("global", "pool", "machine-provider");
                settingsKey.SetValue("WorkforcePoolProvider", provider.ToString());
                userPolicyKey.SetValue("WorkforcePoolProvider", userProvider.ToString());
                machinePolicyKey.SetValue("WorkforcePoolProvider", machineProvider.ToString());

                var settings = repository.GetSettings();
                Assert.AreEqual(machineProvider.ToString(), settings.WorkforcePoolProvider.Value);
                Assert.IsFalse(settings.WorkforcePoolProvider.IsDefault);
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
                var repository = new AccessSettingsRepository(settingsKey, null, null);

                Assert.IsFalse(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void WhenMachinePolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(settingsKey, policyKey, null);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }

        [Test]
        public void WhenUserPolicyKeyExists_ThenIsPolicyPresentReturnsTrue()
        {
            using (var settingsKey = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestUserPolicyKeyPath))
            {
                var repository = new AccessSettingsRepository(settingsKey, null, policyKey);

                Assert.IsTrue(repository.IsPolicyPresent);
            }
        }
    }
}
