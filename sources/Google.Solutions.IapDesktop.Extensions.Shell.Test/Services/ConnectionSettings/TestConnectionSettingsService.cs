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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.ConnectionSettings
{
    [TestFixture]
    public class TestConnectionSettingsService : CommonFixtureBase
    {
        private const string SampleProjectId = "project-1";
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private ConnectionSettingsService service;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var projectRepository = new ProjectRepository(
                hkcu.CreateSubKey(TestKeyPath),
                new Mock<IEventService>().Object);
            var settingsRepository = new ConnectionSettingsRepository(projectRepository);
            this.service = new ConnectionSettingsService(settingsRepository);

            // Set some initial project settings.
            projectRepository.AddProjectAsync(SampleProjectId).Wait();

            var projectSettings = settingsRepository.GetProjectSettings(SampleProjectId);
            projectSettings.RdpDomain.Value = "project-domain";
            settingsRepository.SetProjectSettings(projectSettings);
        }


        private IProjectExplorerProjectNode CreateProjectNode()
        {
            var projectNode = new Mock<IProjectExplorerProjectNode>();
            projectNode.SetupGet(n => n.Project).Returns(new ProjectLocator(SampleProjectId));

            return projectNode.Object;
        }

        private IProjectExplorerZoneNode CreateZoneNode()
        {
            var zoneNode = new Mock<IProjectExplorerZoneNode>();
            zoneNode.SetupGet(n => n.Zone).Returns(new ZoneLocator(SampleProjectId, "zone-1"));

            return zoneNode.Object;
        }

        private IProjectExplorerVmInstanceNode CreateVmInstanceNode(bool isWindows = false)
        {
            var vmNode = new Mock<IProjectExplorerVmInstanceNode>();
            vmNode.SetupGet(n => n.Reference).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            vmNode.SetupGet(n => n.IsWindowsInstance).Returns(isWindows);

            return vmNode.Object;
        }

        //---------------------------------------------------------------------
        // IsConnectionSettingsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNodeUnsupported_ThenIsConnectionSettingsAvailableReturnsFalse()
        {
            Assert.IsFalse(service.IsConnectionSettingsAvailable(
                new Mock<IProjectExplorerNode>().Object));
            Assert.IsFalse(service.IsConnectionSettingsAvailable(
                new Mock<IProjectExplorerCloudNode>().Object));
        }

        [Test]
        public void WhenNodeUnsupported_ThenGetConnectionSettingsRaisesArgumentException()
        {
            Assert.Throws<ArgumentException>(() => service.GetConnectionSettings(
                new Mock<IProjectExplorerNode>().Object));
        }

        //---------------------------------------------------------------------
        // Project.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingProjectSettings_ThenExistingProjectSettingIsVisible()
        {
            var projectNode = CreateProjectNode();

            var settings = this.service.GetConnectionSettings(projectNode);
            Assert.AreEqual("project-domain", settings.TypedCollection.RdpDomain.Value);
        }

        [Test]
        public void WhenChangingProjectSetting_ThenSettingIsSaved()
        {
            var projectNode = CreateProjectNode();

            var firstSettings = this.service.GetConnectionSettings(projectNode);
            firstSettings.TypedCollection.RdpUsername.Value = "bob";
            firstSettings.Save();

            var secondSettings = this.service.GetConnectionSettings(projectNode);
            Assert.AreEqual("bob", secondSettings.TypedCollection.RdpUsername.Value);
        }

        //---------------------------------------------------------------------
        // Zone.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingZoneSettings_ThenExistingProjectSettingIsVisible()
        {
            var zoneNode = CreateZoneNode();

            var settings = this.service.GetConnectionSettings(zoneNode);
            Assert.AreEqual("project-domain", settings.TypedCollection.RdpDomain.Value);
        }

        [Test]
        public void WhenChangingZoneSetting_ThenSettingIsSaved()
        {
            var zoneNode = CreateZoneNode();

            var firstSettings = this.service.GetConnectionSettings(zoneNode);
            firstSettings.TypedCollection.RdpUsername.Value = "bob";
            firstSettings.Save();

            var secondSettings = this.service.GetConnectionSettings(zoneNode);
            Assert.AreEqual("bob", secondSettings.TypedCollection.RdpUsername.Value);
        }

        //---------------------------------------------------------------------
        // VM.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingVmInstanceSettings_ThenExistingProjectSettingIsVisible()
        {
            var vmNode = CreateVmInstanceNode();

            var settings = this.service.GetConnectionSettings(vmNode);
            Assert.AreEqual("project-domain", settings.TypedCollection.RdpDomain.Value);
        }

        [Test]
        public void WhenChangingVmInstanceSetting_ThenSettingIsSaved()
        {
            var vmNode = CreateVmInstanceNode();

            var firstSettings = this.service.GetConnectionSettings(vmNode);
            firstSettings.TypedCollection.RdpUsername.Value = "bob";
            firstSettings.Save();

            var secondSettings = this.service.GetConnectionSettings(vmNode);
            Assert.AreEqual("bob", secondSettings.TypedCollection.RdpUsername.Value);
        }

        //---------------------------------------------------------------------
        // Inheritance.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsernameSetInProject_ProjectValueIsInheritedDownToVm(
            [Values("user", null)]
                string username)
        {
            var projectSettings = this.service.GetConnectionSettings(CreateProjectNode());
            projectSettings.TypedCollection.RdpUsername.Value = username;
            projectSettings.Save();

            // Inherited value is shown...
            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual(username, instanceSettings.TypedCollection.RdpUsername.Value);
            Assert.IsTrue(instanceSettings.TypedCollection.RdpUsername.IsDefault);
        }

        [Test]
        public void WhenUsernameSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            var projectSettings = this.service.GetConnectionSettings(CreateProjectNode());
            projectSettings.TypedCollection.RdpUsername.Value = "root-value";
            projectSettings.Save();

            var zoneSettings = this.service.GetConnectionSettings(CreateZoneNode());
            zoneSettings.TypedCollection.RdpUsername.Value = "overriden-value";
            zoneSettings.Save();

            // Inherited value is shown...
            zoneSettings = this.service.GetConnectionSettings(CreateZoneNode());
            Assert.AreEqual("overriden-value", zoneSettings.TypedCollection.RdpUsername.Value);
            Assert.IsFalse(zoneSettings.TypedCollection.RdpUsername.IsDefault);

            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual("overriden-value", instanceSettings.TypedCollection.RdpUsername.Value);
            Assert.IsTrue(instanceSettings.TypedCollection.RdpUsername.IsDefault);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceIsWindows_ThenSettingsOnlyContainRdpSettings()
        {
            var vmNode = CreateVmInstanceNode(true);

            var settings = this.service.GetConnectionSettings(vmNode);
            CollectionAssert.AreEquivalent(
                settings.TypedCollection.RdpSettings,
                settings.Settings);
        }

        [Test]
        public void WhenInstanceIsLinux_ThenSettingsOnlyContainRdpSettings()
        {
            var vmNode = CreateVmInstanceNode(false);

            var settings = this.service.GetConnectionSettings(vmNode);
            CollectionAssert.AreEquivalent(
                settings.TypedCollection.SshSettings,
                settings.Settings);
        }
    }
}
