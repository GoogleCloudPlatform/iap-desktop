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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Options;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Options
{
    [TestFixture]
    public class TestSshOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey
            .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private SshSettingsRepository settingsRepository;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new SshSettingsRepository(baseKey);
        }

        //---------------------------------------------------------------------
        // IsPropagateLocaleEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsPropagateLocaleEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsPropagateLocaleEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsPropagateLocaleEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsPropagateLocaleEnabled);
        }

        [Test]
        public void WhenDisablingIsPropagateLocaleEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsPropagateLocaleEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new SshOptionsViewModel(this.settingsRepository)
            {
                IsPropagateLocaleEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsPropagateLocaleEnabled.BoolValue);
        }

        [Test]
        public void WhenIsPropagateLocaleEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new SshOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsPropagateLocaleEnabled = !viewModel.IsPropagateLocaleEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }
    }
}
