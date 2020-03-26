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

using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Settings
{
    [TestFixture]
    public class TestInventorySettingsRepository
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);


        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenBaseKeyIsEmpty_SettingsAreEmpty()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);
        }

        [Test]
        public void WhenSettingsSaved_GetSettingsReturnsData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new InventorySettings()
            {
                Username = "user"
            };

            repository.SetSettings(originalSettings);

            var settings = repository.GetSettings();

            Assert.AreEqual(originalSettings.Username, settings.Username);
        }


        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBaseKeyDoesNotExist_ListProjectSettingsReturnsEmptyList()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var projects = repository.ListProjectSettings();

            Assert.IsFalse(projects.Any());
        }

        [Test]
        public void WhenProjectSettingsSaved_ProjectReturnedInListProjects()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);

            var projects = repository.ListProjectSettings();

            Assert.AreEqual(1, projects.Count());
        }

        [Test]
        public void WhenProjectSettingsSaved_GetProjectSettingsReturnsData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(originalSettings.ProjectId);

            Assert.AreEqual(originalSettings.ProjectId, settings.ProjectId);
            Assert.AreEqual(originalSettings.Username, settings.Username);
        }

        [Test]
        public void WhenProjectSettingsSavedTwice_GetProjectSettingsReturnsLatestData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);

            originalSettings.Username = "new-user";
            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(originalSettings.ProjectId);

            Assert.AreEqual(originalSettings.ProjectId, settings.ProjectId);
            Assert.AreEqual(originalSettings.Username, settings.Username);
        }

        [Test]
        public void WhenProjectSettingsDeleted_GetProjectSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);
            repository.DeleteProjectSettings(originalSettings.ProjectId);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings(originalSettings.ProjectId);
            });
        }

        [Test]
        public void WhenIdDoesNotExist_GetProjectSettingsReturnsDefaults()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings("some-project");
            });
        }


        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_GetZoneSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings("nonexisting-project", "zone-id");
            });
        }

        [Test]
        public void WhenZoneIdDoesNotExist_GetZoneSettingsReturnsDefaults()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });

            var settings = repository.GetZoneSettings("pro-1", "some-zone");
            Assert.AreEqual("some-zone", settings.ZoneId);
        }

        [Test]
        public void WhenSetValidZoneSettings_GetZoneSettingsReturnSameValues()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetZoneSettings("pro-1", new ZoneSettings()
            {
                ZoneId = "zone-1",
                Username = "user-1"
            });

            Assert.AreEqual("user-1", repository.GetZoneSettings("pro-1", "zone-1").Username);
        }

        [Test]
        public void WhenProjectSettingsDeleted_ZoneSettingsAreDeletedToo()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetZoneSettings("pro-1", new ZoneSettings()
            {
                ZoneId = "zone-1",
                Username = "user-1"
            });
            repository.DeleteProjectSettings("pro-1");

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings("pro-1", "zone-1");
            });
        }

        //---------------------------------------------------------------------
        // VmInstances.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_GetVmInstanceSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetVmInstanceSettings("nonexisting-project", "vm-id");
            });
        }

        [Test]
        public void WhenVmInstanceIdDoesNotExist_GetVmInstanceSettingsReturnsDefaults()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });

            var settings = repository.GetVmInstanceSettings("pro-1", "some-vm");
            Assert.AreEqual("some-vm", settings.InstanceName);
            Assert.IsNull(settings.Username);
        }

        [Test]
        public void WhenSetValidVmInstanceSettings_GetVmInstanceSettingsReturnSameValues()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetVmInstanceSettings("pro-1", new VmInstanceSettings()
            {
                InstanceName = "vm-1",
                Username = "user-1",
                ConnectionBar = RdpConnectionBarState.Pinned,
                DesktopSize = RdpDesktopSize.ScreenSize,
                AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication,
                ColorDepth = RdpColorDepth.DeepColor,
                AudioMode = RdpAudioMode.DoNotPlay,
                RedirectClipboard = RdpRedirectClipboard.Enabled
            });

            var settings = repository.GetVmInstanceSettings("pro-1", "vm-1");

            Assert.AreEqual("user-1", settings.Username);
            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize.ScreenSize, settings.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, settings.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth.DeepColor, settings.ColorDepth);
            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.AudioMode);
            Assert.AreEqual(RdpRedirectClipboard.Enabled, settings.RedirectClipboard);
        }

        [Test]
        public void WhenProjectSettingsDeleted_VmInstanceSettingsAreDeletedToo()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetVmInstanceSettings("pro-1", new VmInstanceSettings()
            {
                InstanceName = "vm-1",
                Username = "user-1"
            });
            repository.DeleteProjectSettings("pro-1");

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetVmInstanceSettings("pro-1", "vm-1");
            });
        }
    }
}
