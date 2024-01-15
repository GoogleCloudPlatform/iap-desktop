// TODO: udpate tests
////
//// Copyright 2020 Google LLC
////
//// Licensed to the Apache Software Foundation (ASF) under one
//// or more contributor license agreements.  See the NOTICE file
//// distributed with this work for additional information
//// regarding copyright ownership.  The ASF licenses this file
//// to you under the Apache License, Version 2.0 (the
//// "License"); you may not use this file except in compliance
//// with the License.  You may obtain a copy of the License at
//// 
////   http://www.apache.org/licenses/LICENSE-2.0
//// 
//// Unless required by applicable law or agreed to in writing,
//// software distributed under the License is distributed on an
//// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//// KIND, either express or implied.  See the License for the
//// specific language governing permissions and limitations
//// under the License.
////

//using Google.Solutions.Apis.Locator;
//using Google.Solutions.IapDesktop.Application.Profile.Settings;
//using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
//using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
//using Google.Solutions.IapDesktop.Extensions.Session.Settings;
//using Microsoft.Win32;
//using NUnit.Framework;
//using System.Collections.Generic;

//namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
//{
//    [TestFixture]
//    public class TestConnectionSettingsRepository
//    {
//        private const string TestKeyPath = @"Software\Google\__Test";
//        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
//            RegistryHive.CurrentUser,
//            RegistryView.Default);

//        private ProjectRepository projectRepository;

//        private ConnectionSettingsRepository repository;

//        [SetUp]
//        public void SetUp()
//        {
//            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);

//            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);

//            this.projectRepository = new ProjectRepository(baseKey);
//            this.repository = new ConnectionSettingsRepository(
//                this.projectRepository);
//        }

//        //---------------------------------------------------------------------
//        // Projects.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenProjectIdDoesNotExist_GetProjectSettingsThrowsKeyNotFoundException()
//        {
//            Assert.Throws<KeyNotFoundException>(() =>
//            {
//                this.repository.GetProjectSettings("some-project");
//            });
//        }

//        [Test]
//        public void WhenProjectIdExists_GetProjectSettingsReturnsDefaults()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var settings = this.repository.GetProjectSettings("pro-1");

//            Assert.AreEqual("pro-1", settings.ProjectId);
//            Assert.IsTrue(settings.RdpUsername.IsDefault);
//            Assert.IsTrue(settings.RdpPassword.IsDefault);
//            Assert.IsTrue(settings.RdpDomain.IsDefault);
//            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
//            Assert.IsTrue(settings.RdpDesktopSize.IsDefault);
//            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
//            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
//            Assert.IsTrue(settings.RdpAudioMode.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
//            Assert.IsTrue(settings.RdpUserAuthenticationBehavior.IsDefault);
//            Assert.IsTrue(settings.RdpBitmapPersistence.IsDefault);
//            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
//            Assert.IsTrue(settings.RdpPort.IsDefault);
//            Assert.IsTrue(settings.RdpTransport.IsDefault);
//        }

//        [Test]
//        public void WhenProjectSettingsSaved_GetProjectSettingsReturnsData()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetProjectSettings("pro-1");
//            originalSettings.RdpUsername.Value = "user";

//            this.repository.SetProjectSettings(originalSettings);

//            var settings = this.repository.GetProjectSettings(originalSettings.ProjectId);

//            Assert.AreEqual(originalSettings.ProjectId, settings.ProjectId);
//            Assert.AreEqual(originalSettings.RdpUsername.Value, settings.RdpUsername.Value);
//        }

//        [Test]
//        public void WhenProjectSettingsSavedTwice_GetProjectSettingsReturnsLatestData()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetProjectSettings("pro-1");
//            originalSettings.RdpUsername.Value = "user";

//            this.repository.SetProjectSettings(originalSettings);

//            originalSettings.RdpUsername.Value = "new-user";
//            this.repository.SetProjectSettings(originalSettings);

//            var settings = this.repository.GetProjectSettings(originalSettings.ProjectId);

//            Assert.AreEqual(originalSettings.ProjectId, settings.ProjectId);
//            Assert.AreEqual(originalSettings.RdpUsername.Value, settings.RdpUsername.Value);
//        }

//        [Test]
//        public void WhenProjectSettingsDeleted_GetProjectSettingsThrowsKeyNotFoundException()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetProjectSettings("pro-1");
//            originalSettings.RdpUsername.Value = "user";
//            this.repository.SetProjectSettings(originalSettings);

//            this.projectRepository.RemoveProject(new ProjectLocator(originalSettings.ProjectId));

//            Assert.Throws<KeyNotFoundException>(() =>
//            {
//                this.repository.GetProjectSettings(originalSettings.ProjectId);
//            });
//        }

//        //---------------------------------------------------------------------
//        // Zones.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenProjectIdDoesNotExist_GetZoneSettingsThrowsKeyNotFoundException()
//        {
//            Assert.Throws<KeyNotFoundException>(() =>
//            {
//                this.repository.GetZoneSettings("nonexisting-project", "zone-id");
//            });
//        }

//        [Test]
//        public void WhenZoneIdDoesNotExist_GetZoneSettingsReturnsDefaults()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var settings = this.repository.GetZoneSettings("pro-1", "zone-1");

//            Assert.AreEqual("pro-1", settings.ProjectId);
//            Assert.AreEqual("zone-1", settings.ZoneId);
//            Assert.IsTrue(settings.RdpUsername.IsDefault);
//            Assert.IsTrue(settings.RdpPassword.IsDefault);
//            Assert.IsTrue(settings.RdpDomain.IsDefault);
//            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
//            Assert.IsTrue(settings.RdpDesktopSize.IsDefault);
//            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
//            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
//            Assert.IsTrue(settings.RdpAudioMode.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
//            Assert.IsTrue(settings.RdpUserAuthenticationBehavior.IsDefault);
//            Assert.IsTrue(settings.RdpBitmapPersistence.IsDefault);
//            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
//            Assert.IsTrue(settings.RdpPort.IsDefault);
//            Assert.IsTrue(settings.RdpTransport.IsDefault);
//        }

//        [Test]
//        public void WhenSetValidZoneSettings_GetZoneSettingsReturnSameValues()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetZoneSettings("pro-1", "zone-1");
//            originalSettings.RdpUsername.Value = "user-1";

//            this.repository.SetZoneSettings(originalSettings);

//            Assert.AreEqual("user-1", this.repository.GetZoneSettings("pro-1", "zone-1").RdpUsername.Value);
//        }

//        [Test]
//        public void WhenProjectSettingsDeleted_ZoneSettingsAreDeletedToo()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetZoneSettings("pro-1", "zone-1");
//            originalSettings.RdpUsername.Value = "user-1";
//            this.repository.SetZoneSettings(originalSettings);

//            this.projectRepository.RemoveProject(new ProjectLocator("pro-1"));

//            Assert.Throws<KeyNotFoundException>(() =>
//            {
//                this.repository.GetZoneSettings("pro-1", "zone-1");
//            });
//        }

//        //---------------------------------------------------------------------
//        // VmInstances.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenProjectIdDoesNotExist_GetVmInstanceSettingsThrowsKeyNotFoundException()
//        {
//            Assert.Throws<KeyNotFoundException>(() =>
//            {
//                this.repository.GetVmInstanceSettings("nonexisting-project", "vm-id");
//            });
//        }

//        [Test]
//        public void WhenVmInstanceIdDoesNotExist_GetZoneSettingsReturnsDefaults()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var settings = this.repository.GetVmInstanceSettings("pro-1", "instance-1");

//            Assert.AreEqual("pro-1", settings.ProjectId);
//            Assert.AreEqual("instance-1", settings.InstanceName);
//            Assert.IsTrue(settings.RdpUsername.IsDefault);
//            Assert.IsTrue(settings.RdpPassword.IsDefault);
//            Assert.IsTrue(settings.RdpDomain.IsDefault);
//            Assert.IsTrue(settings.RdpConnectionBar.IsDefault);
//            Assert.IsTrue(settings.RdpDesktopSize.IsDefault);
//            Assert.IsTrue(settings.RdpAuthenticationLevel.IsDefault);
//            Assert.IsTrue(settings.RdpColorDepth.IsDefault);
//            Assert.IsTrue(settings.RdpAudioMode.IsDefault);
//            Assert.IsTrue(settings.RdpUserAuthenticationBehavior.IsDefault);
//            Assert.IsTrue(settings.RdpBitmapPersistence.IsDefault);
//            Assert.IsTrue(settings.RdpConnectionTimeout.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectClipboard.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectPrinter.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectSmartCard.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectPort.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectDrive.IsDefault);
//            Assert.IsTrue(settings.RdpRedirectDevice.IsDefault);
//            Assert.IsTrue(settings.RdpPort.IsDefault);
//            Assert.IsTrue(settings.RdpTransport.IsDefault);
//        }

//        [Test]
//        public void WhenSetValidVmInstanceSettings_GetVmInstanceSettingsReturnSameValues()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetVmInstanceSettings("pro-1", "vm-1");
//            originalSettings.RdpUsername.Value = "user-1";
//            originalSettings.RdpConnectionBar.Value = RdpConnectionBarState.Pinned;
//            originalSettings.RdpDesktopSize.Value = RdpDesktopSize.ScreenSize;
//            originalSettings.RdpAuthenticationLevel.Value = RdpAuthenticationLevel.RequireServerAuthentication;
//            originalSettings.RdpColorDepth.Value = RdpColorDepth.DeepColor;
//            originalSettings.RdpAudioMode.Value = RdpAudioMode.DoNotPlay;
//            originalSettings.RdpRedirectClipboard.Value = RdpRedirectClipboard.Enabled;
//            originalSettings.RdpRedirectPrinter.Value = RdpRedirectPrinter.Enabled;
//            originalSettings.RdpRedirectSmartCard.Value = RdpRedirectSmartCard.Enabled;
//            originalSettings.RdpRedirectPort.Value = RdpRedirectPort.Enabled;
//            originalSettings.RdpRedirectDrive.Value = RdpRedirectDrive.Enabled;
//            originalSettings.RdpRedirectDevice.Value = RdpRedirectDevice.Enabled;
//            originalSettings.RdpPort.Value = 13389;
//            originalSettings.RdpTransport.Value = SessionTransportType.Vpc;

//            this.repository.SetVmInstanceSettings(originalSettings);


//            var settings = this.repository.GetVmInstanceSettings("pro-1", "vm-1");

//            Assert.AreEqual("user-1", settings.RdpUsername.Value);
//            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.RdpConnectionBar.Value);
//            Assert.AreEqual(RdpDesktopSize.ScreenSize, settings.RdpDesktopSize.Value);
//            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, settings.RdpAuthenticationLevel.Value);
//            Assert.AreEqual(RdpColorDepth.DeepColor, settings.RdpColorDepth.Value);
//            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.RdpAudioMode.Value);
//            Assert.AreEqual(RdpRedirectClipboard.Enabled, settings.RdpRedirectClipboard.Value);
//            Assert.AreEqual(RdpRedirectPrinter.Enabled, settings.RdpRedirectPrinter.Value);
//            Assert.AreEqual(RdpRedirectSmartCard.Enabled, settings.RdpRedirectSmartCard.Value);
//            Assert.AreEqual(RdpRedirectPort.Enabled, settings.RdpRedirectPort.Value);
//            Assert.AreEqual(RdpRedirectDrive.Enabled, settings.RdpRedirectDrive.Value);
//            Assert.AreEqual(RdpRedirectDevice.Enabled, settings.RdpRedirectDevice.Value);
//            Assert.AreEqual(13389, settings.RdpPort.IntValue);
//            Assert.AreEqual(SessionTransportType.Vpc, settings.RdpTransport.EnumValue);
//        }

//        [Test]
//        public void WhenProjectSettingsDeleted_VmInstanceSettingsAreDeletedToo()
//        {
//            this.projectRepository.AddProject(new ProjectLocator("pro-1"));
//            var originalSettings = this.repository.GetVmInstanceSettings("pro-1", "vm-1");
//            originalSettings.RdpUsername.Value = "user-1";
//            this.repository.SetVmInstanceSettings(originalSettings);

//            this.projectRepository.RemoveProject(new ProjectLocator("pro-1"));

//            Assert.Throws<KeyNotFoundException>(() =>
//            {
//                this.repository.GetVmInstanceSettings("pro-1", "vm-1");
//            });
//        }
//    }
//}
