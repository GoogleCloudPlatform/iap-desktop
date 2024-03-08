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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestToolWindowStateRepository : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new ToolWindowStateRepository(baseKey);

            var settings = repository.GetSetting(
                "Sample1",
                DockState.Unknown);

            Assert.AreEqual(DockState.Unknown, settings.DockState.Value);
            Assert.AreEqual(DockState.Unknown, settings.DockState.DefaultValue);
        }

        [Test]
        public void WhenSettingsSaved_ThenSettingsCanBeRead()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new ToolWindowStateRepository(baseKey);

            var settings = repository.GetSetting(
                "Sample1",
                DockState.Unknown);

            settings.DockState.Value = DockState.Float;
            repository.SetSetting(settings);

            settings = repository.GetSetting(
                "Sample1",
                DockState.Unknown);

            Assert.AreEqual(DockState.Float, settings.DockState.Value);
        }
    }
}
