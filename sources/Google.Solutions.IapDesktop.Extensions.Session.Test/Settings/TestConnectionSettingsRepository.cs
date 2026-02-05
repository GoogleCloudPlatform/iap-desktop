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
using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]
    public class TestConnectionSettingsRepository
    {
        private static ProjectRepository CreateProjectRepository()
        {
            return new ProjectRepository(
                RegistryKeyPath.ForCurrentTest().CreateKey());
        }

        //---------------------------------------------------------------------
        // GetProjectSettings.
        //---------------------------------------------------------------------

        [Test]
        public void GetProjectSettings_WhenProjectIdDoesNotExist()
        {
            var repository = new ConnectionSettingsRepository(CreateProjectRepository());

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings(new ProjectLocator("project-1"));
            });
        }

        [Test]
        public void GetProjectSettings_WhenProjectIdExists()
        {
            var project = new ProjectLocator("project-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var settings = repository.GetProjectSettings(project);

            Assert.That(settings.Resource, Is.EqualTo(project));
            Assert.IsTrue(settings.RdpUsername.IsDefault);
            Assert.IsTrue(settings.RdpPassword.IsDefault);
            Assert.IsTrue(settings.RdpDomain.IsDefault);
            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
            Assert.IsTrue(settings.RdpAudioPlayback.IsDefault);
            Assert.IsTrue(settings.RdpAudioInput.IsDefault);
            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
            Assert.IsTrue(settings.RdpAutomaticLogon.IsDefault);
            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
            Assert.IsTrue(settings.RdpPort.IsDefault);
            Assert.IsTrue(settings.RdpTransport.IsDefault);
            Assert.IsTrue(settings.RdpRedirectWebAuthn.IsDefault);
            Assert.IsTrue(settings.RdpRestrictedAdminMode.IsDefault);
            Assert.IsTrue(settings.RdpSessionType.IsDefault);
        }

        [Test]
        public void GetProjectSettings_WhenProjectSettingsSaved()
        {
            var project = new ProjectLocator("project-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetProjectSettings(project);
            originalSettings.RdpUsername.Value = "user";

            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(project);

            Assert.That(settings.Resource, Is.EqualTo(originalSettings.Resource));
            Assert.That(settings.RdpUsername.Value, Is.EqualTo(originalSettings.RdpUsername.Value));
        }

        [Test]
        public void GetProjectSettings_WhenProjectSettingsSavedTwice()
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

            Assert.That(settings.Resource, Is.EqualTo(originalSettings.Resource));
            Assert.That(settings.RdpUsername.Value, Is.EqualTo(originalSettings.RdpUsername.Value));
        }

        [Test]
        public void GetProjectSettings_WhenProjectSettingsDeleted()
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
        // GetZoneSettings.
        //---------------------------------------------------------------------

        [Test]
        public void GetZoneSettings_WhenProjectIdDoesNotExist()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var repository = new ConnectionSettingsRepository(CreateProjectRepository());
            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings(zone);
            });
        }

        [Test]
        public void GetZoneSettings_WhenZoneIdDoesNotExist()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(zone.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var settings = repository.GetZoneSettings(zone);

            Assert.That(settings.Resource, Is.EqualTo(zone));
            Assert.IsTrue(settings.RdpUsername.IsDefault);
            Assert.IsTrue(settings.RdpPassword.IsDefault);
            Assert.IsTrue(settings.RdpDomain.IsDefault);
            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
            Assert.IsTrue(settings.RdpAudioPlayback.IsDefault);
            Assert.IsTrue(settings.RdpAudioInput.IsDefault);
            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
            Assert.IsTrue(settings.RdpAutomaticLogon.IsDefault);
            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
            Assert.IsTrue(settings.RdpPort.IsDefault);
            Assert.IsTrue(settings.RdpTransport.IsDefault);
            Assert.IsTrue(settings.RdpRedirectWebAuthn.IsDefault);
            Assert.IsTrue(settings.RdpRestrictedAdminMode.IsDefault);
            Assert.IsTrue(settings.RdpSessionType.IsDefault);
            Assert.IsTrue(settings.RdpDesktopSize.IsDefault);
        }

        [Test]
        public void GetZoneSettings_WhenSetValidZoneSettings()
        {
            var zone = new ZoneLocator("project-1", "zone-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(zone.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var originalSettings = repository.GetZoneSettings(zone);
            originalSettings.RdpUsername.Value = "user-1";
            repository.SetZoneSettings(originalSettings);

            var settings = repository.GetZoneSettings(zone);

            Assert.That(settings.RdpUsername.Value, Is.EqualTo("user-1"));
        }

        [Test]
        public void GetZoneSettings_WhenProjectSettingsDeleted()
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
        // GetInstanceSettings.
        //---------------------------------------------------------------------

        [Test]
        public void GetInstanceSettings_WhenProjectIdDoesNotExist()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var repository = new ConnectionSettingsRepository(CreateProjectRepository());
            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetInstanceSettings(instance);
            });
        }

        [Test]
        public void GetInstanceSettings_WhenInstanceIdDoesNotExist()
        {
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(instance.Project);

            var repository = new ConnectionSettingsRepository(projectRepository);
            var settings = repository.GetInstanceSettings(instance);

            Assert.That(settings.Resource, Is.EqualTo(instance));
            Assert.IsTrue(settings.RdpUsername.IsDefault);
            Assert.IsTrue(settings.RdpPassword.IsDefault);
            Assert.IsTrue(settings.RdpDomain.IsDefault);
            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
            Assert.IsTrue(settings.RdpAudioPlayback.IsDefault);
            Assert.IsTrue(settings.RdpAudioInput.IsDefault);
            Assert.IsTrue(settings.RdpAutomaticLogon.IsDefault);
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
            Assert.IsTrue(settings.RdpDesktopSize.IsDefault);
        }

        [Test]
        public void GetInstanceSettings_WhenSetValidInstanceSettings()
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
            originalSettings.RdpAudioPlayback.Value = RdpAudioPlayback.DoNotPlay;
            originalSettings.RdpAudioInput.Value = RdpAudioInput.Enabled;
            originalSettings.RdpRedirectClipboard.Value = RdpRedirectClipboard.Enabled;
            originalSettings.RdpRedirectPrinter.Value = RdpRedirectPrinter.Enabled;
            originalSettings.RdpRedirectSmartCard.Value = RdpRedirectSmartCard.Enabled;
            originalSettings.RdpRedirectPort.Value = RdpRedirectPort.Enabled;
            originalSettings.RdpRedirectDrive.Value = RdpRedirectDrive.Enabled;
            originalSettings.RdpRedirectDevice.Value = RdpRedirectDevice.Enabled;
            originalSettings.RdpPort.Value = 13389;
            originalSettings.RdpTransport.Value = SessionTransportType.Vpc;
            originalSettings.RdpDpiScaling.Value = RdpDpiScaling.Disabled;
            originalSettings.RdpDesktopSize.Value = RdpDesktopSize.ScreenSize;

            repository.SetInstanceSettings(originalSettings);


            var settings = repository.GetInstanceSettings(instance);

            Assert.That(settings.RdpUsername.Value, Is.EqualTo("user-1"));
            Assert.That(settings.RdpConnectionBar.Value, Is.EqualTo(RdpConnectionBarState.Pinned));
            Assert.That(settings.RdpAuthenticationLevel.Value, Is.EqualTo(RdpAuthenticationLevel.RequireServerAuthentication));
            Assert.That(settings.RdpColorDepth.Value, Is.EqualTo(RdpColorDepth.DeepColor));
            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback.DoNotPlay));
            Assert.That(settings.RdpAudioInput.Value, Is.EqualTo(RdpAudioInput.Enabled));
            Assert.That(settings.RdpRedirectClipboard.Value, Is.EqualTo(RdpRedirectClipboard.Enabled));
            Assert.That(settings.RdpRedirectPrinter.Value, Is.EqualTo(RdpRedirectPrinter.Enabled));
            Assert.That(settings.RdpRedirectSmartCard.Value, Is.EqualTo(RdpRedirectSmartCard.Enabled));
            Assert.That(settings.RdpRedirectPort.Value, Is.EqualTo(RdpRedirectPort.Enabled));
            Assert.That(settings.RdpRedirectDrive.Value, Is.EqualTo(RdpRedirectDrive.Enabled));
            Assert.That(settings.RdpRedirectDevice.Value, Is.EqualTo(RdpRedirectDevice.Enabled));
            Assert.That(settings.RdpPort.Value, Is.EqualTo(13389));
            Assert.That(settings.RdpTransport.Value, Is.EqualTo(SessionTransportType.Vpc));
            Assert.That(settings.RdpDpiScaling.Value, Is.EqualTo(RdpDpiScaling.Disabled));
            Assert.That(settings.RdpDesktopSize.Value, Is.EqualTo(RdpDesktopSize.ScreenSize));
        }

        [Test]
        public void GetInstanceSettings_WhenProjectSettingsDeleted()
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
        public void GetInstanceSettingsByUrl_WhenProjectIdDoesNotExist()
        {
            var url = IapRdpUrl.FromString(
                "iap-rdp:///project-1/zone-1/instance-1?username=john%20doe&RdpPort=13389");

            var repository = new ConnectionSettingsRepository(CreateProjectRepository());
            var settings = repository.GetInstanceSettings(url, out var foundInInventory);
            Assert.IsNotNull(settings);
            Assert.That(foundInInventory, Is.False);

            Assert.That(
                settings.Resource, Is.EqualTo(new InstanceLocator("project-1", "zone-1", "instance-1")));

            Assert.That(settings.RdpUsername.Value, Is.EqualTo("john doe"));
            Assert.That(settings.RdpPort.Value, Is.EqualTo(13389));
            Assert.IsNull(settings.RdpDomain.Value);
        }

        [Test]
        public void GetInstanceSettingsByUrl_WhenProjectFound()
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
            Assert.That(settings.Resource, Is.EqualTo(instance));

            Assert.That(settings.RdpUsername.Value, Is.EqualTo("john doe"));
            Assert.That(settings.RdpPort.Value, Is.EqualTo(13389));
            Assert.That(settings.RdpDomain.Value, Is.EqualTo("domain"));
        }
    }
}
