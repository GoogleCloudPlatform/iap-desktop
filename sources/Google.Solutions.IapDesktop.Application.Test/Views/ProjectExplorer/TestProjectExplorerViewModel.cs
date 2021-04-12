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

        // TODO: Re-enable VM tests
        // TODO: Add new VM tests

        //[Test]
        //public void WhenUsingDefaultSettings_ThenWindowsAndLinuxIsIncluded()
        //{
        //    var viewModel = new ProjectExplorerViewModel(this.settingsRepository);

        //    Assert.IsTrue(viewModel.IsWindowsIncluded);
        //    Assert.IsTrue(viewModel.IsLinuxIncluded);
        //}

        //[Test]
        //public void WhenAllOsEnabledInSettings_ThenAllOsAreIncluded()
        //{
        //    // Write settings.
        //    new ProjectExplorerViewModel(this.settingsRepository)
        //    {
        //        IsWindowsIncluded = true,
        //        IsLinuxIncluded = true
        //    };

        //    // Read again.
        //    var viewModel = new ProjectExplorerViewModel(this.settingsRepository);
        //    Assert.IsTrue(viewModel.IsWindowsIncluded);
        //    Assert.IsTrue(viewModel.IsLinuxIncluded);
        //}

        //[Test]
        //public void WhenAllOsDisabledInSettings_ThenNoOsAreIncluded()
        //{
        //    // Write settings.
        //    new ProjectExplorerViewModel(this.settingsRepository)
        //    {
        //        IsWindowsIncluded = false,
        //        IsLinuxIncluded = false
        //    };

        //    // Read again.
        //    var viewModel = new ProjectExplorerViewModel(this.settingsRepository);
        //    Assert.IsFalse(viewModel.IsWindowsIncluded);
        //    Assert.IsFalse(viewModel.IsLinuxIncluded);
        //}
    }
}
