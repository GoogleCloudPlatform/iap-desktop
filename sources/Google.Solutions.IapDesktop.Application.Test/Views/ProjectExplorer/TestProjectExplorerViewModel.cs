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
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Views.ProjectExplorer
{
    [TestFixture]
    public class TestProjectExplorerViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ApplicationSettingsRepository settingsRepository;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            this.settingsRepository = new ApplicationSettingsRepository(baseKey);
        }

        //---------------------------------------------------------------------
        // Include OS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsingDefaultSettings_ThenOnlyWindowsIsIncluded()
        {
            var viewModel = new ProjectExplorerViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsWindowsIncluded);
            Assert.IsFalse(viewModel.IsLinuxIncluded);
        }

        [Test]
        public void WhenAllOsEnabledInSettings_ThenAllOsAreIncluded()
        {
            // Write settings.
            var viewModel = new ProjectExplorerViewModel(this.settingsRepository);
            viewModel.IsWindowsIncluded = true;
            viewModel.IsLinuxIncluded = true;

            // Read again.
            viewModel = new ProjectExplorerViewModel(this.settingsRepository);
            Assert.IsTrue(viewModel.IsWindowsIncluded);
            Assert.IsTrue(viewModel.IsLinuxIncluded);
        }

        [Test]
        public void WhenAllOsDisabledInSettings_ThenNoOsAreIncluded()
        {
            // Write settings.
            var viewModel = new ProjectExplorerViewModel(this.settingsRepository);
            viewModel.IsWindowsIncluded = false;
            viewModel.IsLinuxIncluded = false;

            // Read again.
            viewModel = new ProjectExplorerViewModel(this.settingsRepository);
            Assert.IsFalse(viewModel.IsWindowsIncluded);
            Assert.IsFalse(viewModel.IsLinuxIncluded);
        }
    }
}
