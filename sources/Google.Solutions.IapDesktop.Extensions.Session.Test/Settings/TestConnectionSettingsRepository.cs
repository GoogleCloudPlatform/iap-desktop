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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]
    public class TestConnectionSettingsRepository
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private static readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private static ProjectRepository CreateProjectRepository()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            return new ProjectRepository(hkcu.CreateSubKey(TestKeyPath));
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_ThenGetProjectSettingsThrowsKeyNotFoundException()
        {
            var repository = new ConnectionSettingsRepository(CreateProjectRepository());

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings(new ProjectLocator("project-1"));
            });
        }

        [Test]
        public void WhenProjectIdExists_ThenGetProjectSettingsReturnsDefaults()
        {
            var project = new ProjectLocator("project-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var settings = repository.GetProjectSettings(project);

            Assert.AreEqual(project, settings.Resource);
            Assert.IsTrue(settings.RdpUsername.IsDefault);
            Assert.IsTrue(settings.RdpPassword.IsDefault);
            Assert.IsTrue(settings.RdpDomain.IsDefault);
            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
            Assert.IsTrue(settings.RdpAudioMode.IsDefault);
            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
            Assert.IsTrue(settings.RdpUserAuthenticationBehavior.IsDefault);
            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
            Assert.IsTrue(settings.RdpPort.IsDefault);
            Assert.IsTrue(settings.RdpTransport.IsDefault);
            Assert.IsTrue(settings.RdpRedirectWebAuthn.IsDefault);
            Assert.IsTrue(settings.RdpRestrictedAdminMode.IsDefault);
            Assert.IsTrue(settings.RdpSessionType.IsDefault);
        }

        [Test]
        public void WhenProjectSettingsSaved_ThenGetProjectSettingsReturnsData()
        {
            var project = new ProjectLocator("project-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetProjectSettings(project);
            originalSettings.RdpUsername.Value = "user";

            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(project);

            Assert.AreEqual(originalSettings.Resource, settings.Resource);
            Assert.AreEqual(originalSettings.RdpUsername.Value, settings.RdpUsername.Value);
        }

        [Test]
        public void WhenProjectSettingsSavedTwice_ThenGetProjectSettingsReturnsLatestData()
        {
            var project = new ProjectLocator("project-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetProjectSettings(project);
            originalSettings.RdpUsername.Value = "user";

            repository.SetProjectSettings(originalSettings);

            originalSettings.RdpUsername.Value = "new-user";
            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(project);

            Assert.AreEqual(originalSettings.Resource, settings.Resource);
            Assert.AreEqual(originalSettings.RdpUsername.Value, settings.RdpUsername.Value);
        }

        [Test]
        public void WhenProjectSettingsDeleted_ThenGetProjectSettingsThrowsKeyNotFoundException()
        {
            var project = new ProjectLocator("project-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetProjectSettings(project);
            originalSettings.RdpUsername.Value = "user";
            repository.SetProjectSettings(originalSettings);

            projectRepository.RemoveProject(project);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings(project);
            });
        }

        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_ThenGetZoneSettingsThrowsKeyNotFoundException()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var repository = new ConnectionSettingsRepository(CreateProjectRepository());
            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings(zone);
            });
        }

        [Test]
        public void WhenZoneIdDoesNotExist_ThenGetZoneSettingsReturnsDefaults()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(zone.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var settings = repository.GetZoneSettings(zone);

            Assert.AreEqual(zone, settings.Resource);
            Assert.IsTrue(settings.RdpUsername.IsDefault);
            Assert.IsTrue(settings.RdpPassword.IsDefault);
            Assert.IsTrue(settings.RdpDomain.IsDefault);
            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
            Assert.IsTrue(settings.RdpAudioMode.IsDefault);
            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
            Assert.IsTrue(settings.RdpUserAuthenticationBehavior.IsDefault);
            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
            Assert.IsTrue(settings.RdpPort.IsDefault);
            Assert.IsTrue(settings.RdpTransport.IsDefault);
            Assert.IsTrue(settings.RdpRedirectWebAuthn.IsDefault);
            Assert.IsTrue(settings.RdpRestrictedAdminMode.IsDefault);
            Assert.IsTrue(settings.RdpSessionType.IsDefault);
        }

        [Test]
        public void WhenSetValidZoneSettings_ThenGetZoneSettingsReturnSameValues()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(zone.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetZoneSettings(zone);
            originalSettings.RdpUsername.Value = "user-1";
            repository.SetZoneSettings(originalSettings);

            var settings = repository.GetZoneSettings(zone);

            Assert.AreEqual("user-1", settings.RdpUsername.Value);
        }

        [Test]
        public void WhenProjectSettingsDeleted_ThenGetZoneSettingsAreDeletedToo()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(zone.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetZoneSettings(zone);
            originalSettings.RdpUsername.Value = "user-1";
            repository.SetZoneSettings(originalSettings);

            projectRepository.RemoveProject(zone.Project);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings(zone);
            });
        }

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_ThenGetInstanceSettingsThrowsKeyNotFoundException()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var repository = new ConnectionSettingsRepository(CreateProjectRepository());
            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetInstanceSettings(instance);
            });
        }

        [Test]
        public void WhenInstanceIdDoesNotExist_ThenGetZoneSettingsReturnsDefaults()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(instance.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var settings = repository.GetInstanceSettings(instance);

            Assert.AreEqual(instance, settings.Resource);
            Assert.IsTrue(settings.RdpUsername.IsDefault);
            Assert.IsTrue(settings.RdpPassword.IsDefault);
            Assert.IsTrue(settings.RdpDomain.IsDefault);
            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
            Assert.IsTrue(settings.RdpAudioMode.IsDefault);
            Assert.IsTrue(settings.RdpUserAuthenticationBehavior.IsDefault);
            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
            Assert.IsTrue(settings.RdpRedirectPrinter.IsDefault);
            Assert.IsTrue(settings.RdpRedirectSmartCard.IsDefault);
            Assert.IsTrue(settings.RdpRedirectPort.IsDefault);
            Assert.IsTrue(settings.RdpRedirectDrive.IsDefault);
            Assert.IsTrue(settings.RdpRedirectDevice.IsDefault);
            Assert.IsTrue(settings.RdpPort.IsDefault);
            Assert.IsTrue(settings.RdpTransport.IsDefault);
            Assert.IsTrue(settings.RdpRedirectWebAuthn.IsDefault);
            Assert.IsTrue(settings.RdpRestrictedAdminMode.IsDefault);
            Assert.IsTrue(settings.RdpSessionType.IsDefault);
            Assert.IsTrue(settings.RdpDpiScaling.IsDefault);
        }

        [Test]
        public void WhenSetValidInstanceSettings_ThenGetInstanceSettingsReturnSameValues()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(instance.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetInstanceSettings(instance);
            originalSettings.RdpUsername.Value = "user-1";
            originalSettings.RdpConnectionBar.Value = RdpConnectionBarState.Pinned;
            originalSettings.RdpAuthenticationLevel.Value = RdpAuthenticationLevel.RequireServerAuthentication;
            originalSettings.RdpColorDepth.Value = RdpColorDepth.DeepColor;
            originalSettings.RdpAudioMode.Value = RdpAudioMode.DoNotPlay;
            originalSettings.RdpRedirectClipboard.Value = RdpRedirectClipboard.Enabled;
            originalSettings.RdpRedirectPrinter.Value = RdpRedirectPrinter.Enabled;
            originalSettings.RdpRedirectSmartCard.Value = RdpRedirectSmartCard.Enabled;
            originalSettings.RdpRedirectPort.Value = RdpRedirectPort.Enabled;
            originalSettings.RdpRedirectDrive.Value = RdpRedirectDrive.Enabled;
            originalSettings.RdpRedirectDevice.Value = RdpRedirectDevice.Enabled;
            originalSettings.RdpPort.Value = 13389;
            originalSettings.RdpTransport.Value = SessionTransportType.Vpc;
            originalSettings.RdpDpiScaling.Value = RdpDpiScaling.Disabled;

            repository.SetInstanceSettings(originalSettings);


            var settings = repository.GetInstanceSettings(instance);

            Assert.AreEqual("user-1", settings.RdpUsername.Value);
            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth.DeepColor, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard.Enabled, settings.RdpRedirectClipboard.Value);
            Assert.AreEqual(RdpRedirectPrinter.Enabled, settings.RdpRedirectPrinter.Value);
            Assert.AreEqual(RdpRedirectSmartCard.Enabled, settings.RdpRedirectSmartCard.Value);
            Assert.AreEqual(RdpRedirectPort.Enabled, settings.RdpRedirectPort.Value);
            Assert.AreEqual(RdpRedirectDrive.Enabled, settings.RdpRedirectDrive.Value);
            Assert.AreEqual(RdpRedirectDevice.Enabled, settings.RdpRedirectDevice.Value);
            Assert.AreEqual(13389, settings.RdpPort.Value);
            Assert.AreEqual(SessionTransportType.Vpc, settings.RdpTransport.Value);
            Assert.AreEqual(RdpDpiScaling.Disabled, settings.RdpDpiScaling.Value);
        }

        [Test]
        public void WhenProjectSettingsDeleted_ThenGetInstanceSettingsAreDeletedToo()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(instance.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetInstanceSettings(instance);
            originalSettings.RdpUsername.Value = "user-1";
            repository.SetInstanceSettings(originalSettings);

            projectRepository.RemoveProject(instance.Project);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetInstanceSettings(instance);
            });
        }

        //---------------------------------------------------------------------
        // Instances - URL.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_ThenGetInstanceSettingsByUrlReturnsUrlSettings()
        {
            var url = IapRdpUrl.FromString(
                "iap-rdp:///project-1/zone-1/instance-1?username=john%20doe&RdpPort=13389");

            var repository = new ConnectionSettingsRepository(CreateProjectRepository());
            var settings = repository.GetInstanceSettings(url, out var foundInInventory);
            Assert.IsNotNull(settings);
            Assert.IsFalse(foundInInventory);

            Assert.AreEqual(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                settings.Resource);

            Assert.AreEqual("john doe", settings.RdpUsername.Value);
            Assert.AreEqual(13389, settings.RdpPort.Value);
            Assert.IsNull(settings.RdpDomain.Value);
        }

        [Test]
        public void WhenProjectFound_ThenGetInstanceSettingsByUrlReturnsMergedSettings()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            //
            // Set project-wide defaults.
            //
            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(instance.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var projectSettings = repository.GetProjectSettings(instance.Project);
            projectSettings.RdpUsername.Value = "user-1";
            projectSettings.RdpDomain.Value = "domain";
            repository.SetProjectSettings(projectSettings);

            //
            // Expect defaults to be applied.
            //
            var url = IapRdpUrl.FromString(
                "iap-rdp:///project-1/zone-1/instance-1?username=john%20doe&RdpPort=13389");

            var settings = repository.GetInstanceSettings(url, out var foundInInventory);
            Assert.IsNotNull(settings);
            Assert.IsTrue(foundInInventory);
            Assert.AreEqual(instance, settings.Resource);

            Assert.AreEqual("john doe", settings.RdpUsername.Value);
            Assert.AreEqual(13389, settings.RdpPort.Value);
            Assert.AreEqual("domain", settings.RdpDomain.Value);
        }
    }
}
