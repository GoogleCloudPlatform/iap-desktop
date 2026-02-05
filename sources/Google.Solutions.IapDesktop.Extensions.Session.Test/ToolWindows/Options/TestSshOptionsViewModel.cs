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
        public void IsPropagateLocaleEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.EnableLocalePropagation.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsPropagateLocaleEnabled.Value, Is.True);
        }

        [Test]
        public void IsPropagateLocaleEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.EnableLocalePropagation.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsPropagateLocaleEnabled.Value, Is.False);
        }

        [Test]
        public async Task IsPropagateLocaleEnabled_WhenDisablingIsPropagateLocaleEnabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.EnableLocalePropagation.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.IsPropagateLocaleEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.EnableLocalePropagation.Value, Is.False);
        }

        [Test]
        public void IsPropagateLocaleEnabled_WhenIsPropagateLocaleEnabledChanged()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsPropagateLocaleEnabled.Value = !viewModel.IsPropagateLocaleEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // UsePersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void UsePersistentKey_WhenSettingEnabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.UsePersistentKey.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.UsePersistentKey.Value, Is.True);
            Assert.That(viewModel.IsUsePersistentKeyEditable.Value, Is.True);
            Assert.That(viewModel.IsPublicKeyValidityInDaysEditable.Value, Is.True);
        }

        [Test]
        public void UsePersistentKey_WhenSettingDisabled()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.UsePersistentKey.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.UsePersistentKey.Value, Is.False);
            Assert.That(viewModel.IsUsePersistentKeyEditable.Value, Is.True);
            Assert.That(viewModel.IsPublicKeyValidityInDaysEditable.Value, Is.False);
        }

        [Test]
        public void UsePersistentKey_WhenSettingDisabledByPolicy()
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

            Assert.That(viewModel.UsePersistentKey.Value, Is.False);
            Assert.That(viewModel.IsUsePersistentKeyEditable.Value, Is.False);
            Assert.That(viewModel.IsPublicKeyValidityInDaysEditable.Value, Is.False);
        }

        [Test]
        public async Task UsePersistentKey_WhenDisablingUsePersistentKey()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.UsePersistentKey.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.UsePersistentKey.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.UsePersistentKey.Value, Is.False);
        }

        [Test]
        public void UsePersistentKey_WhenUsePersistentKeyChanged()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.UsePersistentKey.Value = !viewModel.UsePersistentKey.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // PublicKeyValidityInDays.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyValidityInDays_WhenSettingPopulated()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.Value = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That((int)viewModel.PublicKeyValidityInDays.Value, Is.EqualTo(1));
            Assert.That(viewModel.IsPublicKeyValidityInDaysEditable.Value, Is.True);
        }

        [Test]
        public void PublicKeyValidityInDays_WhenSettingPopulatedByPolicy()
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

            Assert.That(viewModel.PublicKeyValidityInDays.Value, Is.EqualTo(2));
            Assert.That(viewModel.IsPublicKeyValidityInDaysEditable.Value, Is.False);
        }

        [Test]
        public async Task PublicKeyValidityInDays_WhenChangingPublicKeyValidityInDays()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.Value = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.PublicKeyValidityInDays.Value = 365 * 2;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.PublicKeyValidity.Value, Is.EqualTo(365 * 2 * 24 * 60 * 60));
        }

        [Test]
        public void PublicKeyValidityInDays_WhenPublicKeyValidityInDaysChanged()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.PublicKeyValidityInDays.Value++;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // PublicKeyType.
        //---------------------------------------------------------------------

        [Test]
        public void PublicKeyType_WhenSettingPopulated()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp256));
            Assert.That(viewModel.IsPublicKeyTypeEditable, Is.True);
        }

        [Test]
        public void PublicKeyType_WhenSettingPopulatedByPolicy()
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

            Assert.That(viewModel.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp384));
            Assert.That(viewModel.IsPublicKeyTypeEditable, Is.False);
        }

        [Test]
        public async Task PublicKeyType_WhenChangingPublicKeyType()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.Value = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);
            viewModel.PublicKeyType.Value = SshKeyType.EcdsaNistp384;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.PublicKeyType.Value, Is.EqualTo(SshKeyType.EcdsaNistp384));
        }

        [Test]
        public void PublicKeyType_WhenPublicKeyTypeChanged()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.PublicKeyType.Value++;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }
    }
}
