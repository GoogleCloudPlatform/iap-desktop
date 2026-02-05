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
using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.ProjectExplorer
{
    [TestFixture]
    public class TestProjectExplorerSettings
    {
        private static ApplicationSettingsRepository CreateSettingsRepository()
        {
            return new ApplicationSettingsRepository(
                RegistryKeyPath.ForCurrentTest().CreateKey(),
                null,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // CollapsedProjects.
        //---------------------------------------------------------------------

        [Test]
        public void CollapsedProjects_WhenNoValueSaved_ThenCollapsedProjectsReturnsEmptySet()
        {
            var settingsRepository = CreateSettingsRepository();

            using (var explorerSettings = new ProjectExplorerSettings(settingsRepository, true))
            {
                Assert.That(explorerSettings.CollapsedProjects, Is.Not.Null);
                Assert.That(explorerSettings.CollapsedProjects, Is.Empty);
            }
        }

        [Test]
        public void CollapsedProjects_WhenValueSaved_ThenCollapsedProjectsReturnsSavedValue()
        {
            var settingsRepository = CreateSettingsRepository();

            var settings = settingsRepository.GetSettings();
            settings.CollapsedProjects.Value = "  project-1,, project-2,";
            settingsRepository.SetSettings(settings);

            using (var explorerSettings = new ProjectExplorerSettings(settingsRepository, true))
            {
                Assert.That(explorerSettings.CollapsedProjects, Is.Not.Null);
                Assert.That(
                    explorerSettings.CollapsedProjects, Is.EquivalentTo(new[]
                    {
                        new ProjectLocator("project-1"),
                        new ProjectLocator("project-2")
                    }));
            }
        }

        [Test]
        public void CollapsedProjects_WhenDisposed_ThenCollapsedProjectsAreSaved()
        {
            var settingsRepository = CreateSettingsRepository();

            using (var explorerSettings = new ProjectExplorerSettings(settingsRepository, false))
            {
                explorerSettings.CollapsedProjects.Add(new ProjectLocator("project-1"));
                explorerSettings.CollapsedProjects.Add(new ProjectLocator("project-2"));
            }

            var settings = settingsRepository.GetSettings();
            Assert.That(settings.CollapsedProjects.Value, Is.EqualTo("project-1,project-2"));
        }
    }
}
