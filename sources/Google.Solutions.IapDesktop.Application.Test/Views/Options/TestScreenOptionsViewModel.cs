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

using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestScreenOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private ApplicationSettingsRepository settingsRepository;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new ApplicationSettingsRepository(
                baseKey,
                null,
                null);
        }

        //---------------------------------------------------------------------
        // Full screen devices.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyIsMissing_ThenNoDevicesAreSelected()
        {
            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count(), 1);
            Assert.IsFalse(viewModel.Devices.Any(d => d.IsSelected));
        }

        [Test]
        public void WhenKeyIsEmpty_ThenNoDevicesAreSelected()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.FullScreenDevices.StringValue = "";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count(), 1);
            Assert.IsFalse(viewModel.Devices.Any(d => d.IsSelected));
        }

        [Test]
        public void WhenKeyContainsUnknownDevice_ThenNoDevicesAreSelected()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.FullScreenDevices.StringValue = "unknown\\device,and junk";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count(), 1);
            Assert.IsFalse(viewModel.Devices.Any(d => d.IsSelected));
        }


        [Test]
        public void WhenKeyContainsDevices_ThenDevicesAreSelected()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.FullScreenDevices.StringValue = "unknown," + Screen.PrimaryScreen.DeviceName;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            Assert.GreaterOrEqual(viewModel.Devices.Count(), 1);
            Assert.IsTrue(viewModel.Devices
                .First(d => d.DeviceName == Screen.PrimaryScreen.DeviceName)
                .IsSelected);
        }

        [Test]
        public void WhenDeviceSelected_ThenIsDirtyIsSet()
        {
            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);
            Assert.GreaterOrEqual(viewModel.Devices.Count(), 1);

            viewModel.Devices.First().IsSelected = true;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenAllDevicesDeselected_ThenKeyIsRemoved()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.FullScreenDevices.StringValue = "unknown\\device,and junk";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            viewModel.Devices.First().IsSelected = true;
            viewModel.Devices.First().IsSelected = false;
            viewModel.ApplyChanges();

            Assert.IsNull(this.settingsRepository.GetSettings().FullScreenDevices.StringValue);
        }

        [Test]
        public void WhenDeviceSelected_ThenKeyIsUpdated()
        {
            var settings = this.settingsRepository.GetSettings();
            this.settingsRepository.SetSettings(settings);

            var viewModel = new ScreenOptionsViewModel(
                this.settingsRepository);

            viewModel.Devices.First().IsSelected = true;
            viewModel.ApplyChanges();

            Assert.AreEqual(
                Screen.PrimaryScreen.DeviceName,
                this.settingsRepository.GetSettings().FullScreenDevices.StringValue);
        }
    }
}
