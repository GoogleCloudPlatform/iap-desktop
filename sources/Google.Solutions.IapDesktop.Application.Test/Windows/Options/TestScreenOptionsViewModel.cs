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

using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestScreenOptionsViewModel : ApplicationFixtureBase
    {
        private ApplicationSettingsRepository CreateSettingsRepository()
        {
            var settingsKey = RegistryKeyPath
                .ForCurrentTest(RegistryKeyPath.KeyType.Settings)
                .CreateKey();

            return new ApplicationSettingsRepository(
                settingsKey,
                null,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // Full screen devices.
        //---------------------------------------------------------------------

        [Test]
        public void Devices_WhenKeyIsMissing_ThenNoDevicesAreSelected()
        {
            var settingsRepository = CreateSettingsRepository();

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count, 1);
            Assert.IsFalse(viewModel.Devices.Any(d => d.IsSelected));
        }

        [Test]
        public void Devices_WhenKeyIsEmpty_ThenNoDevicesAreSelected()
        {
            var settingsRepository = CreateSettingsRepository();

            var settings = settingsRepository.GetSettings();
            settings.FullScreenDevices.Value = "";
            settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count, 1);
            Assert.IsFalse(viewModel.Devices.Any(d => d.IsSelected));
        }

        [Test]
        public void Devices_WhenKeyContainsUnknownDevice_ThenNoDevicesAreSelected()
        {
            var settingsRepository = CreateSettingsRepository();

            var settings = settingsRepository.GetSettings();
            settings.FullScreenDevices.Value = "unknown\\device,and junk";
            settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count, 1);
            Assert.IsFalse(viewModel.Devices.Any(d => d.IsSelected));
        }


        [Test]
        public void Devices_WhenKeyContainsDevices_ThenDevicesAreSelected()
        {
            var settingsRepository = CreateSettingsRepository();

            var settings = settingsRepository.GetSettings();
            settings.FullScreenDevices.Value = "unknown," + Screen.PrimaryScreen.DeviceName;
            settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count, 1);
            Assert.IsTrue(viewModel.Devices
                .First(d => d.DeviceName == Screen.PrimaryScreen.DeviceName)
                .IsSelected);
        }

        [Test]
        public void Devices_WhenDeviceSelected_ThenIsDirtyIsSet()
        {
            var settingsRepository = CreateSettingsRepository();

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);
            Assert.GreaterOrEqual(viewModel.Devices.Count, 1);

            viewModel.Devices.First().IsSelected = true;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        [Test]
        public async Task Devices_WhenAllDevicesDeselected_ThenKeyIsRemoved()
        {
            var settingsRepository = CreateSettingsRepository();

            var settings = settingsRepository.GetSettings();
            settings.FullScreenDevices.Value = "unknown\\device,and junk";
            settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            viewModel.Devices.First().IsSelected = true;
            viewModel.Devices.First().IsSelected = false;

            await viewModel.ApplyChangesAsync();

            Assert.IsNull(settingsRepository.GetSettings().FullScreenDevices.Value);
        }

        [Test]
        public async Task Devices_WhenDeviceSelected_ThenKeyIsUpdated()
        {
            var settingsRepository = CreateSettingsRepository();

            var settings = settingsRepository.GetSettings();
            settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                settingsRepository);

            viewModel.Devices.First().IsSelected = true;

            await viewModel.ApplyChangesAsync();

            Assert.AreEqual(
                Screen.PrimaryScreen.DeviceName,
                settingsRepository.GetSettings().FullScreenDevices.Value);
        }
    }
}
