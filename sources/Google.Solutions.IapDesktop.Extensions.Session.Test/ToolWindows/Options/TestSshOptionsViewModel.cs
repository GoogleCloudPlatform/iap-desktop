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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options;
using Google.Solutions.Ssh.Cryptography;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Options
{
    [TestFixture]
    public class TestSshOptionsViewModel
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestMachinePolicyKeyPath = @"Software\Google\__TestMachinePolicy";

        private readonly RegistryKey hkcu = RegistryKey
            .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private SshSettingsRepository CreateSettingsRepository(
            IDictionary<string, object> policies = null)
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.hkcu.DeleteSubKeyTree(TestMachinePolicyKeyPath, false);

            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var policyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath);
            foreach (var policy in policies.EnsureNotNull())
            {
                policyKey.SetValue(policy.Key, policy.Value);
            }

            return new SshSettingsRepository(
                baseKey,
                policyKey,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // IsPropagateLocaleEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsPropagateLocaleEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.IsPropagateLocaleEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsPropagateLocaleEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsPropagateLocaleEnabled);
        }

        [Test]
        public async Task WhenDisablingIsPropagateLocaleEnabled_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository)
            {
                IsPropagateLocaleEnabled = false
            };

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsPropagateLocaleEnabled.BoolValue);
        }

        [Test]
        public void WhenIsPropagateLocaleEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsPropagateLocaleEnabled = !viewModel.IsPropagateLocaleEnabled;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // PublicKeyValidityInDays.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingPopulated_ThenPublicKeyValidityInDaysHasCorrectValue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.IntValue = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(1, viewModel.PublicKeyValidityInDays);
            Assert.IsTrue(viewModel.IsPublicKeyValidityInDaysEditable);
        }

        [Test]
        public void WhenSettingPopulatedByPolicy_ThenIsPublicKeyValidityInDaysEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "PublicKeyValidity", 60 * 60 * 24 * 2 }
                });

            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.IntValue = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(2, viewModel.PublicKeyValidityInDays);
            Assert.IsFalse(viewModel.IsPublicKeyValidityInDaysEditable);
        }

        [Test]
        public async Task WhenChangingPublicKeyValidityInDays_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyValidity.IntValue = 60 * 60 * 26; // 1.5 days
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository)
            {
                PublicKeyValidityInDays = 365 * 2
            };

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual(365 * 2 * 24 * 60 * 60, settings.PublicKeyValidity.IntValue);
        }

        [Test]
        public void WhenPublicKeyValidityInDaysChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.PublicKeyValidityInDays++;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // PublicKeyType.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingPopulated_ThenPublicKeyTypeHasCorrectValue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.EnumValue = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(SshKeyType.EcdsaNistp256, viewModel.PublicKeyType);
            Assert.IsTrue(viewModel.IsPublicKeyTypeEditable);
        }

        [Test]
        public void WhenSettingPopulatedByPolicy_ThenIsPublicKeyTypeEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "PublicKeyType", (int)SshKeyType.EcdsaNistp384 }
                });

            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.EnumValue = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.AreEqual(SshKeyType.EcdsaNistp384, viewModel.PublicKeyType);
            Assert.IsFalse(viewModel.IsPublicKeyTypeEditable);
        }

        [Test]
        public async Task WhenChangingPublicKeyType_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.PublicKeyType.EnumValue = SshKeyType.EcdsaNistp256;
            settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(settingsRepository)
            {
                PublicKeyType = SshKeyType.EcdsaNistp384
            };

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual(SshKeyType.EcdsaNistp384, settings.PublicKeyType.EnumValue);
        }

        [Test]
        public void WhenPublicKeyTypeChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.PublicKeyType++;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        [Test]
        public void WhenPublicKeyTypeNotChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.PublicKeyType = viewModel.PublicKeyType;

            Assert.IsFalse(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // PublicKeyTypeIndex.
        //---------------------------------------------------------------------

        [Test]
        public void WhenIndexSet_ThenPublicKeyTypeIsUpdated()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            viewModel.PublicKeyType = viewModel.AllPublicKeyTypes[0];
            viewModel.PublicKeyTypeIndex++;

            Assert.AreEqual(1, viewModel.PublicKeyTypeIndex);
            Assert.AreEqual(viewModel.AllPublicKeyTypes[1], viewModel.PublicKeyType);
        }

        //---------------------------------------------------------------------
        // AllPublicKeyTypes.
        //---------------------------------------------------------------------

        [Test]
        public void AllPublicKeyTypesReturnsList()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new SshOptionsViewModel(settingsRepository);

            var keyTypes = viewModel.AllPublicKeyTypes.ToList();

            Assert.Greater(keyTypes.Count, 1);
            Assert.AreEqual(keyTypes.Count, Enum.GetValues(typeof(SshKeyType)).Length);
        }
    }
}
