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

using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.Settings.Collection;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestAccessOptionsViewModel
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestMachinePolicyKeyPath = @"Software\Google\__TestMachinePolicy";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        //
        // Pseudo-PSC endpoint that passes validation.
        //
        private const string SamplePscEndpoint = "www.googleapis.com";

        private IRepository<IAccessSettings> CreateSettingsRepository(
            IDictionary<string, object>? policies = null)
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.hkcu.DeleteSubKeyTree(TestMachinePolicyKeyPath, false);

            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var policyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath);
            foreach (var policy in policies.EnsureNotNull())
            {
                policyKey.SetValue(policy.Key, policy.Value);
            }

            return new AccessSettingsRepository(baseKey, policyKey, null);
        }

        //---------------------------------------------------------------------
        // DCA.
        //---------------------------------------------------------------------

        [Test]
        public void DeviceCertificateAuthentication_WhenSettingEnabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEnabled.Value);
            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEditable.Value);
        }

        [Test]
        public void DeviceCertificateAuthentication_WhenSettingDisabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.IsFalse(viewModel.IsDeviceCertificateAuthenticationEnabled.Value);
            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEditable.Value);
        }

        [Test]
        public void DeviceCertificateAuthentication_WhenSettingEnabledByPolicy()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "IsDeviceCertificateAuthenticationEnabled", 1 }
                });

            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEnabled.Value);
            Assert.IsFalse(viewModel.IsDeviceCertificateAuthenticationEditable.Value);
        }

        [Test]
        public async Task DeviceCertificateAuthentication_ApplyChanges()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());
            viewModel.IsDeviceCertificateAuthenticationEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.Value);
        }

        [Test]
        public void DeviceCertificateAuthentication_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsDeviceCertificateAuthenticationEnabled.Value =
                !viewModel.IsDeviceCertificateAuthenticationEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // PrivateServiceConnectEndpoint.
        //---------------------------------------------------------------------

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointConfigured()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PrivateServiceConnectEndpoint.Value = "psc";
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.AreEqual("psc", viewModel.PrivateServiceConnectEndpoint.Value);
            Assert.IsTrue(viewModel.IsPrivateServiceConnectEnabled.Value);
            Assert.IsTrue(viewModel.IsPrivateServiceConnectEditable.Value);
        }

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointNotConfigured()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PrivateServiceConnectEndpoint.Value = null;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.IsNull(viewModel.PrivateServiceConnectEndpoint.Value);
            Assert.IsFalse(viewModel.IsPrivateServiceConnectEnabled.Value);
            Assert.IsTrue(viewModel.IsPrivateServiceConnectEditable.Value);
        }

        [Test]
        public void PrivateServiceConnectEndpoint_WhenPrivateServiceConnectEndpointConfiguredByPolicy()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "PrivateServiceConnectEndpoint", "psc-policy" }
                });

            var settings = settingsRepository.GetSettings();
            settings.PrivateServiceConnectEndpoint.Value = null;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.AreEqual("psc-policy", viewModel.PrivateServiceConnectEndpoint.Value);
            Assert.IsTrue(viewModel.IsPrivateServiceConnectEnabled.Value);
            Assert.IsFalse(viewModel.IsPrivateServiceConnectEditable.Value);
        }

        [Test]
        public async Task PrivateServiceConnectEndpoint_WhenChangingPrivateServiceConnectEndpoint_ApplyChanges()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient())
            {
                ProbePrivateServiceConnectEndpoint = false
            };
            viewModel.IsPrivateServiceConnectEnabled.Value = true;
            viewModel.PrivateServiceConnectEndpoint.Value = SamplePscEndpoint;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual(SamplePscEndpoint, settings.PrivateServiceConnectEndpoint.Value);
        }

        [Test]
        public void PrivateServiceConnectEndpoint_WhenChangingPrivateServiceConnectEndpoint_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient())
            {
                ProbePrivateServiceConnectEndpoint = false
            };

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsPrivateServiceConnectEnabled.Value = true;
            viewModel.PrivateServiceConnectEndpoint.Value = SamplePscEndpoint;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // ConnectionPoolLimit.
        //---------------------------------------------------------------------

        [Test]
        public void ConnectionPoolLimit_WhenNotConfigured()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.AreEqual(16, viewModel.ConnectionPoolLimit.Value);
        }

        [Test]
        public void ConnectionPoolLimit_WhenConfigured()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.ConnectionLimit.Value = 5;
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.AreEqual(5, viewModel.ConnectionPoolLimit.Value);
        }

        [Test]
        public async Task ConnectionPoolLimit_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());

            Assert.IsFalse(viewModel.IsDirty.Value);
            viewModel.ConnectionPoolLimit.Value = 4;

            Assert.IsTrue(viewModel.IsDirty.Value);

            await viewModel.ApplyChangesAsync();

            Assert.IsFalse(viewModel.IsDirty.Value);
            Assert.AreEqual(4, settingsRepository.GetSettings().ConnectionLimit.Value);
        }

        //---------------------------------------------------------------------
        // ApplyChanges.
        //---------------------------------------------------------------------

        [Test]
        public async Task ApplyChanges_WhenDisablingPsc_ThenApplyChangesClearsPscEndpoint()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PrivateServiceConnectEndpoint.Value = "psc";
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());
            viewModel.IsPrivateServiceConnectEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsNull(settings.PrivateServiceConnectEndpoint.Value);
        }

        [Test]
        public void ApplyChanges_WhenPscAndDcaEnabled_ThenApplyChangesThrowsException()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());
            viewModel.IsDeviceCertificateAuthenticationEnabled.Value = true;
            viewModel.IsPrivateServiceConnectEnabled.Value = true;
            viewModel.PrivateServiceConnectEndpoint.Value = "new-psc";

            Assert.Throws<InvalidOptionsException>(
                () => viewModel.ApplyChangesAsync().Wait());
        }

        [Test]
        public void ApplyChanges_WhenPscEndpointInvalid_ThenApplyChangesThrowsException()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settingsRepository.SetSettings(settings);

            var viewModel = new AccessOptionsViewModel(
                settingsRepository,
                new HelpClient());
            viewModel.IsPrivateServiceConnectEnabled.Value = true;
            viewModel.PrivateServiceConnectEndpoint.Value = "invalid-endpoint";

            Assert.Throws<InvalidOptionsException>(
                () => viewModel.ApplyChangesAsync().Wait());
        }
    }
}
