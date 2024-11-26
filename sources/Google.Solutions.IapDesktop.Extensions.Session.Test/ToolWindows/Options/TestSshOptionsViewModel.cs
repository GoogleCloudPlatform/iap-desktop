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

using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Options
{
    [TestFixture]
    public class TestSshOptionsViewModel
    {
        private SshSettingsRepository CreateSettingsRepository(
            IDictionary<string, object>? policies = null)
        {
            var settingsKey = RegistryKeyPath
                .ForCurrentTest(RegistryKeyPath.KeyType.Settings)
                .CreateKey();

            var policyKey = RegistryKeyPath
                .ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy)
                .CreateKey();

            foreach (var policy in policies.EnsureNotNull())
            {
                policyKey.SetValue(policy.Key, policy.Value);
            }

            return new SshSettingsRepository(
                settingsKey,
                policyKey,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // IsPropagateLocaleEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsPropagateLocaleEnabled_WhenSettingEnabled_ThenIsPropagateLocaleEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.IsPropagateLocaleEnabled.Value);
        }

        [Test]
        public void IsPropagateLocaleEnabled_WhenSettingDisabled_ThenIsPropagateLocaleEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsPropagateLocaleEnabled.Value);
        }

        [Test]
        public async Task IsPropagateLocaleEnabled_WhenDisablingIsPropagateLocaleEnabled_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.IsPropagateLocaleEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsPropagateLocaleEnabled.Value);
        }

        [Test]
        public void IsPropagateLocaleEnabled_WhenIsPropagateLocaleEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsPropagateLocaleEnabled.Value = !viewModel.IsPropagateLocaleEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // UsePersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void UsePersistentKey_WhenSettingEnabled_ThenUsePersistentKeyIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.UsePersistentKey.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.UsePersistentKey.Value);
            Assert.IsTrue(viewModel.IsUsePersistentKeyEditable.Value);
            Assert.IsTrue(viewModel.IsPublicKeyValidityInDaysEditable.Value);
        }

        [Test]
        public void UsePersistentKey_WhenSettingDisabled_ThenUsePersistentKeyIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.UsePersistentKey.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.UsePersistentKey.Value);
            Assert.IsTrue(viewModel.IsUsePersistentKeyEditable.Value);
            Assert.IsFalse(viewModel.IsPublicKeyValidityInDaysEditable.Value);
        }

        [Test]
        public void UsePersistentKey_WhenSettingDisabledByPolicy_ThenIsUsePersistentKeyEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "UsePersistentKey", 0 }
                });

            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.Value = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.UsePersistentKey.Value);
            Assert.IsFalse(viewModel.IsUsePersistentKeyEditable.Value);
            Assert.IsFalse(viewModel.IsPublicKeyValidityInDaysEditable.Value);
        }

        [Test]
        public async Task UsePersistentKey_WhenDisablingUsePersistentKey_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.UsePersistentKey.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.UsePersistentKey.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.UsePersistentKey.Value);
        }

        [Test]
        public void UsePersistentKey_WhenUsePersistentKeyChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.UsePersistentKey.Value = !viewModel.UsePersistentKey.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // PublicKeyValidityInDays.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyValidityInDays_WhenSettingPopulated_ThenPublicKeyValidityInDaysHasCorrectValue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.Value = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(1, (int)viewModel.PublicKeyValidityInDays.Value);
            Assert.IsTrue(viewModel.IsPublicKeyValidityInDaysEditable.Value);
        }

        [Test]
        public void PublicKeyValidityInDays_WhenSettingPopulatedByPolicy_ThenIsPublicKeyValidityInDaysEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "PublicKeyValidity", 60 * 60 * 24 * 2 }
                });

            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.Value = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(2, viewModel.PublicKeyValidityInDays.Value);
            Assert.IsFalse(viewModel.IsPublicKeyValidityInDaysEditable.Value);
        }

        [Test]
        public async Task PublicKeyValidityInDays_WhenChangingPublicKeyValidityInDays_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.Value = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.PublicKeyValidityInDays.Value = 365 * 2;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual(365 * 2 * 24 * 60 * 60, settings.PublicKeyValidity.Value);
        }

        [Test]
        public void PublicKeyValidityInDays_WhenPublicKeyValidityInDaysChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.PublicKeyValidityInDays.Value++;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // PublicKeyType.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyType_WhenSettingPopulated_ThenPublicKeyTypeHasCorrectValue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(SshKeyType.EcdsaNistp256, viewModel.PublicKeyType.Value);
            Assert.IsTrue(viewModel.IsPublicKeyTypeEditable);
        }

        [Test]
        public void PublicKeyType_WhenSettingPopulatedByPolicy_ThenIsPublicKeyTypeEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "PublicKeyType", (int)SshKeyType.EcdsaNistp384 }
                });

            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(SshKeyType.EcdsaNistp384, viewModel.PublicKeyType.Value);
            Assert.IsFalse(viewModel.IsPublicKeyTypeEditable);
        }

        [Test]
        public async Task PublicKeyType_WhenChangingPublicKeyType_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.PublicKeyType.Value = SshKeyType.EcdsaNistp384;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual(SshKeyType.EcdsaNistp384, settings.PublicKeyType.Value);
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.PublicKeyType.Value++;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }
    }
}
