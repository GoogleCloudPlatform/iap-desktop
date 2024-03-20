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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Microsoft.Win32;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestAppearanceOptionsViewModel
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private ThemeSettingsRepository CreateSettingsRepository()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);

            return new ThemeSettingsRepository(baseKey);
        }

        //---------------------------------------------------------------------
        // SelectedTheme.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSelectedThemeChanged_ThenDirtyFlagIsSet()
        {
            var settingsRepository = CreateSettingsRepository();

            Assert.AreNotEqual(
                ApplicationTheme._Default,
                ApplicationTheme.Dark);

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.SelectedTheme.Value = ApplicationTheme.Dark;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // IsGdiScalingEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenGdiScalingChanged_ThenDirtyFlagIsSet()
        {
            var settingsRepository = CreateSettingsRepository();

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            Assert.IsTrue(viewModel.IsGdiScalingEnabled.Value);

            viewModel.IsGdiScalingEnabled.Value = false;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void LoadReadsSettings()
        {
            var settingsRepository = CreateSettingsRepository();

            Assert.AreNotEqual(
                ApplicationTheme._Default,
                ApplicationTheme.Dark);

            //
            // Persist non-default values.
            //
            var settings = settingsRepository.GetSettings();
            settings.Theme.Value = ApplicationTheme.Dark;
            settings.IsGdiScalingEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            Assert.AreEqual(ApplicationTheme.Dark, viewModel.SelectedTheme.Value);
            Assert.IsFalse(viewModel.IsGdiScalingEnabled.Value);
        }

        [Test]
        public async Task SaveUpdatesSettings()
        {
            var settingsRepository = CreateSettingsRepository();

            Assert.AreNotEqual(
                ApplicationTheme._Default,
                ApplicationTheme.Dark);

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            viewModel.SelectedTheme.Value = ApplicationTheme.Dark;
            viewModel.IsGdiScalingEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            var settings = settingsRepository.GetSettings();
            Assert.AreEqual(ApplicationTheme.Dark, settings.Theme.Value);
            Assert.IsFalse(settings.IsGdiScalingEnabled.Value);
        }
    }
}
