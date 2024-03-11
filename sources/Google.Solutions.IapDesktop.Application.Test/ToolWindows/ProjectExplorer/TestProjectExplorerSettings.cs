//
// Copyright 2022 Google LLC
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows.ProjectExplorer;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.ProjectExplorer
{
    [TestFixture]
    public class TestProjectExplorerSettings
    {
        private const string TestKeyPath = @"Software\Google\__Test";

        private ApplicationSettingsRepository settingsRepository;

        [SetUp]
        public void SetUp()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.settingsRepository = new ApplicationSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath),
                null,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // CollapsedProjects.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoValueSaved_ThenCollapsedProjectsReturnsEmptySet()
        {
            using (var explorerSettings = new ProjectExplorerSettings(this.settingsRepository, true))
            {
                Assert.IsNotNull(explorerSettings.CollapsedProjects);
                CollectionAssert.IsEmpty(explorerSettings.CollapsedProjects);
            }
        }

        [Test]
        public void WhenValueSaved_ThenCollapsedProjectsReturnsSavedValue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.CollapsedProjects.Value = "  project-1,, project-2,";
            this.settingsRepository.SetSettings(settings);

            using (var explorerSettings = new ProjectExplorerSettings(this.settingsRepository, true))
            {
                Assert.IsNotNull(explorerSettings.CollapsedProjects);
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        new ProjectLocator("project-1"),
                        new ProjectLocator("project-2")
                    },
                    explorerSettings.CollapsedProjects);
            }
        }

        [Test]
        public void WhenDisposed_ThenCollapsedProjectsAreSaved()
        {
            using (var explorerSettings = new ProjectExplorerSettings(this.settingsRepository, false))
            {
                explorerSettings.CollapsedProjects.Add(new ProjectLocator("project-1"));
                explorerSettings.CollapsedProjects.Add(new ProjectLocator("project-2"));
            }

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual("project-1,project-2", settings.CollapsedProjects.Value);
        }
    }
}
