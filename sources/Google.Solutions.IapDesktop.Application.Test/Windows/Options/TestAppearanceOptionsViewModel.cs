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
using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestAppearanceOptionsViewModel
    {
        private ThemeSettingsRepository CreateSettingsRepository()
        {
            return new ThemeSettingsRepository(RegistryKeyPath
                .ForCurrentTest(RegistryKeyPath.KeyType.Settings)
                .CreateKey());
        }

        //---------------------------------------------------------------------
        // SelectedTheme.
        //---------------------------------------------------------------------

        [Test]
        public void SelectedTheme_WhenChanged_ThenDirtyFlagIsSet()
        {
            var settingsRepository = CreateSettingsRepository();

            Assert.That(
                ApplicationTheme.Dark, Is.Not.EqualTo(ApplicationTheme._Default));

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.SelectedTheme.Value = ApplicationTheme.Dark;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // ScalingMode.
        //---------------------------------------------------------------------

        [Test]
        public void ScalingMode_WhenChanged_ThenDirtyFlagIsSet()
        {
            var settingsRepository = CreateSettingsRepository();

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            Assert.That(viewModel.ScalingMode.Value, Is.Not.EqualTo(ScalingMode.None));

            viewModel.ScalingMode.Value = ScalingMode.SystemDpiAware;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void LoadSettings()
        {
            var settingsRepository = CreateSettingsRepository();

            Assert.That(
                ApplicationTheme.Dark, Is.Not.EqualTo(ApplicationTheme._Default));

            //
            // Persist non-default values.
            //
            var settings = settingsRepository.GetSettings();
            settings.Theme.Value = ApplicationTheme.Dark;
            settings.ScalingMode.Value = ScalingMode.None;
            settingsRepository.SetSettings(settings);

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            Assert.That(viewModel.SelectedTheme.Value, Is.EqualTo(ApplicationTheme.Dark));
            Assert.That(viewModel.ScalingMode.Value, Is.EqualTo(ScalingMode.None));
        }

        [Test]
        public async Task ApplyChanges()
        {
            var settingsRepository = CreateSettingsRepository();

            Assert.That(
                ApplicationTheme.Dark, Is.Not.EqualTo(ApplicationTheme._Default));

            var viewModel = new AppearanceOptionsViewModel(settingsRepository);
            viewModel.SelectedTheme.Value = ApplicationTheme.Dark;
            viewModel.ScalingMode.Value = ScalingMode.None;
            await viewModel.ApplyChangesAsync();

            var settings = settingsRepository.GetSettings();
            Assert.That(settings.Theme.Value, Is.EqualTo(ApplicationTheme.Dark));
            Assert.That(viewModel.ScalingMode.Value, Is.EqualTo(ScalingMode.None));
        }
    }
}
