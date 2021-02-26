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
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Services.Connection
{
    [TestFixture]
    public class TestRdpSettingsService : CommonFixtureBase
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
            projectSettings.Domain.Value = "project-domain";
            settingsRepository.SetProjectSettings(projectSettings);
        }


        private IProjectExplorerProjectNode CreateProjectNode()
        {
            var projectNode = new Mock<IProjectExplorerProjectNode>();
            projectNode.SetupGet(n => n.ProjectId).Returns(SampleProjectId);

            return projectNode.Object;
        }

        private IProjectExplorerZoneNode CreateZoneNode()
        {
            var zoneNode = new Mock<IProjectExplorerZoneNode>();
            zoneNode.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
            zoneNode.SetupGet(n => n.ZoneId).Returns("zone-1");

            return zoneNode.Object;
        }

        private IProjectExplorerVmInstanceNode CreateVmInstanceNode()
        {
            var vmNode = new Mock<IProjectExplorerVmInstanceNode>();
            vmNode.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
            vmNode.SetupGet(n => n.ZoneId).Returns("zone-1");
            vmNode.SetupGet(n => n.InstanceName).Returns("instance-1");
            vmNode.SetupGet(n => n.Reference).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));

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

        [Test]
        public void WhenNodeSupported_ThenIsConnectionSettingsAvailableReturnsTrue()
        {
            var windowsVm = new Mock<IProjectExplorerVmInstanceNode>();
            windowsVm.SetupGet(n => n.IsWindowsInstance).Returns(true);
            var linuxVm = new Mock<IProjectExplorerVmInstanceNode>();
            linuxVm.SetupGet(n => n.IsWindowsInstance).Returns(false);

            Assert.IsTrue(service.IsConnectionSettingsAvailable(
                new Mock<IProjectExplorerProjectNode>().Object));
            Assert.IsTrue(service.IsConnectionSettingsAvailable(
                new Mock<IProjectExplorerZoneNode>().Object));
            Assert.IsFalse(service.IsConnectionSettingsAvailable(linuxVm.Object));
            Assert.IsTrue(service.IsConnectionSettingsAvailable(windowsVm.Object));
        }

        //---------------------------------------------------------------------
        // Project.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingProjectSettings_ThenExistingProjectSettingIsVisible()
        {
            var projectNode = CreateProjectNode();

            var settings = this.service.GetConnectionSettings(projectNode);
            Assert.AreEqual("project-domain", settings.TypedCollection.Domain.Value);
        }

        [Test]
        public void WhenChangingProjectSetting_ThenSettingIsSaved()
        {
            var projectNode = CreateProjectNode();

            var firstSettings = this.service.GetConnectionSettings(projectNode);
            firstSettings.TypedCollection.Username.Value = "bob";
            firstSettings.Save();

            var secondSettings = this.service.GetConnectionSettings(projectNode);
            Assert.AreEqual("bob", secondSettings.TypedCollection.Username.Value);
        }

        //---------------------------------------------------------------------
        // Zone.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingZoneSettings_ThenExistingProjectSettingIsVisible()
        {
            var zoneNode = CreateZoneNode();

            var settings = this.service.GetConnectionSettings(zoneNode);
            Assert.AreEqual("project-domain", settings.TypedCollection.Domain.Value);
        }

        [Test]
        public void WhenChangingZoneSetting_ThenSettingIsSaved()
        {
            var zoneNode = CreateZoneNode();

            var firstSettings = this.service.GetConnectionSettings(zoneNode);
            firstSettings.TypedCollection.Username.Value = "bob";
            firstSettings.Save();

            var secondSettings = this.service.GetConnectionSettings(zoneNode);
            Assert.AreEqual("bob", secondSettings.TypedCollection.Username.Value);
        }

        //---------------------------------------------------------------------
        // VM.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingVmInstanceSettings_ThenExistingProjectSettingIsVisible()
        {
            var vmNode = CreateVmInstanceNode();

            var settings = this.service.GetConnectionSettings(vmNode);
            Assert.AreEqual("project-domain", settings.TypedCollection.Domain.Value);
        }

        [Test]
        public void WhenChangingVmInstanceSetting_ThenSettingIsSaved()
        {
            var vmNode = CreateVmInstanceNode();

            var firstSettings = this.service.GetConnectionSettings(vmNode);
            firstSettings.TypedCollection.Username.Value = "bob";
            firstSettings.Save();

            var secondSettings = this.service.GetConnectionSettings(vmNode);
            Assert.AreEqual("bob", secondSettings.TypedCollection.Username.Value);
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
            projectSettings.TypedCollection.Username.Value = username;
            projectSettings.Save();

            // Inherited value is shown...
            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual(username, instanceSettings.TypedCollection.Username.Value);
            Assert.IsTrue(instanceSettings.TypedCollection.Username.IsDefault);
        }

        [Test]
        public void WhenUsernameSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            var projectSettings = this.service.GetConnectionSettings(CreateProjectNode());
            projectSettings.TypedCollection.Username.Value = "root-value";
            projectSettings.Save();

            var zoneSettings = this.service.GetConnectionSettings(CreateZoneNode());
            zoneSettings.TypedCollection.Username.Value = "overriden-value";
            zoneSettings.Save();

            // Inherited value is shown...
            zoneSettings = this.service.GetConnectionSettings(CreateZoneNode());
            Assert.AreEqual("overriden-value", zoneSettings.TypedCollection.Username.Value);
            Assert.IsFalse(zoneSettings.TypedCollection.Username.IsDefault);

            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual("overriden-value", instanceSettings.TypedCollection.Username.Value);
            Assert.IsTrue(instanceSettings.TypedCollection.Username.IsDefault);
        }
    }
}
