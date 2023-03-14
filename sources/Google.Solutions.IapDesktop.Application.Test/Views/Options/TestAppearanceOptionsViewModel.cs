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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestAppearanceOptionsViewModel
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private ThemeSettingsRepository settingsRepository;

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new ThemeSettingsRepository(baseKey);
        }

        //---------------------------------------------------------------------
        // SelectedTheme.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSelectedThemeChanged_ThenDirtyFlagIsSet()
        {
            Assert.AreNotEqual(
                ThemeSettings.ApplicationTheme._Default,
                ThemeSettings.ApplicationTheme.Dark);

            var viewModel = new AppearanceOptionsViewModel(this.settingsRepository);
            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.SelectedTheme.Value = ThemeSettings.ApplicationTheme.Dark;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void LoadReadsSettings()
        {
            Assert.AreNotEqual(
                ThemeSettings.ApplicationTheme._Default,
                ThemeSettings.ApplicationTheme.Dark);

            //
            // Persist non-default values.
            //
            var settings = this.settingsRepository.GetSettings();
            settings.Theme.Value = ThemeSettings.ApplicationTheme.Dark;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new AppearanceOptionsViewModel(this.settingsRepository);
            Assert.AreEqual(ThemeSettings.ApplicationTheme.Dark, viewModel.SelectedTheme.Value);
        }

        [Test]
        public async Task SaveUpdatesSettings()
        {
            Assert.AreNotEqual(
                ThemeSettings.ApplicationTheme._Default,
                ThemeSettings.ApplicationTheme.Dark);

            var viewModel = new AppearanceOptionsViewModel(this.settingsRepository);
            viewModel.SelectedTheme.Value = ThemeSettings.ApplicationTheme.Dark;
            await viewModel.ApplyChangesAsync();

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual(ThemeSettings.ApplicationTheme.Dark, settings.Theme.Value);
        }
    }
}
