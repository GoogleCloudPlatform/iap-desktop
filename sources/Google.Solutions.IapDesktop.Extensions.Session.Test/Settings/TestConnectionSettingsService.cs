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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]
    public class TestConnectionSettingsService
    {
        private static readonly ProjectLocator SampleProject = new ProjectLocator("project-1");
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private ConnectionSettingsService service;

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var projectRepository = new ProjectRepository(this.hkcu.CreateSubKey(TestKeyPath));
            var settingsRepository = new ConnectionSettingsRepository(projectRepository);
            this.service = new ConnectionSettingsService(settingsRepository);

            // Set some initial project settings.
            projectRepository.AddProject(SampleProject);

            var projectSettings = settingsRepository.GetProjectSettings(SampleProject);
            projectSettings.RdpDomain.Value = "project-domain";
            settingsRepository.SetProjectSettings(projectSettings);
        }

        private IProjectModelProjectNode CreateProjectNode()
        {
            var projectNode = new Mock<IProjectModelProjectNode>();
            projectNode.SetupGet(n => n.Project).Returns(SampleProject);

            return projectNode.Object;
        }

        private IProjectModelZoneNode CreateZoneNode()
        {
            var zoneNode = new Mock<IProjectModelZoneNode>();
            zoneNode.SetupGet(n => n.Zone).Returns(new ZoneLocator(SampleProject.ProjectId, "zone-1"));

            return zoneNode.Object;
        }

        private IProjectModelInstanceNode CreateVmInstanceNode(bool isWindows = false)
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProject, "zone-1", "instance-1"));
            vmNode.SetupGet(n => n.OperatingSystem).Returns(
                isWindows ? OperatingSystems.Windows : OperatingSystems.Linux);

            return vmNode.Object;
        }

        //---------------------------------------------------------------------
        // IsConnectionSettingsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNodeUnsupported_ThenIsConnectionSettingsAvailableReturnsFalse()
        {
            Assert.IsFalse(this.service.IsConnectionSettingsAvailable(
                new Mock<IProjectModelNode>().Object));
            Assert.IsFalse(this.service.IsConnectionSettingsAvailable(
                new Mock<IProjectModelCloudNode>().Object));
        }

        [Test]
        public void WhenNodeUnsupported_ThenGetConnectionSettingsRaisesArgumentException()
        {
            Assert.Throws<ArgumentException>(() => this.service.GetConnectionSettings(
                new Mock<IProjectModelNode>().Object));
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

        [Test]
        public void WhenPortSetInProject_ProjectValueIsInheritedDownToVm()
        {
            var projectSettings = this.service.GetConnectionSettings(CreateProjectNode());
            projectSettings.TypedCollection.RdpPort.IntValue = 13389;
            projectSettings.Save();

            // Inherited value is shown...
            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual(13389, instanceSettings.TypedCollection.RdpPort.Value);
            Assert.IsTrue(instanceSettings.TypedCollection.RdpPort.IsDefault);
        }

        [Test]
        public void WhenPortSetInZoneAndResetInVm_ZoneVmValueApplies()
        {
            var zoneSettings = this.service.GetConnectionSettings(CreateZoneNode());
            zoneSettings.TypedCollection.RdpPort.IntValue = 13389;
            zoneSettings.Save();

            // Reset to default...
            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            instanceSettings.TypedCollection.RdpPort.IntValue = 3389;
            instanceSettings.Save();

            // Own value is shown...
            var effectiveSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual(3389, effectiveSettings.TypedCollection.RdpPort.Value);
            Assert.IsFalse(effectiveSettings.TypedCollection.RdpPort.IsDefault);
        }

        [Test]
        public void WhenPortSetInProjectAndResetInVm_ZoneVmValueApplies()
        {
            var projectSettings = this.service.GetConnectionSettings(CreateProjectNode());
            projectSettings.TypedCollection.RdpPort.IntValue = 13389;
            projectSettings.Save();

            // Reset to default...
            var instanceSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            instanceSettings.TypedCollection.RdpPort.IntValue = 3389;
            instanceSettings.Save();

            // Own value is shown...
            var effectiveSettings = this.service.GetConnectionSettings(CreateVmInstanceNode());
            Assert.AreEqual(3389, effectiveSettings.TypedCollection.RdpPort.Value);
            Assert.IsFalse(effectiveSettings.TypedCollection.RdpPort.IsDefault);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceIsWindows_ThenSettingsContainRdpAndAppSettings()
        {
            var vmNode = CreateVmInstanceNode(true);

            var settings = this.service.GetConnectionSettings(vmNode);

            CollectionAssert.IsSupersetOf(
                settings.Settings,
                settings.TypedCollection.RdpSettings);
            CollectionAssert.IsSupersetOf(
                settings.Settings,
                settings.TypedCollection.AppSettings);

            CollectionAssert.IsNotSupersetOf(
                settings.Settings,
                settings.TypedCollection.SshSettings);
        }

        [Test]
        public void WhenInstanceIsLinux_ThenSettingsContainSshAndAppSettings()
        {
            var vmNode = CreateVmInstanceNode(false);

            var settings = this.service.GetConnectionSettings(vmNode);

            CollectionAssert.IsSupersetOf(
                settings.Settings,
                settings.TypedCollection.SshSettings);
            CollectionAssert.IsSupersetOf(
                settings.Settings,
                settings.TypedCollection.AppSettings);

            CollectionAssert.IsNotSupersetOf(
                settings.Settings,
                settings.TypedCollection.RdpSettings);
        }

        //---------------------------------------------------------------------
        // AppSettings.
        //---------------------------------------------------------------------

        [Test]
        public void AppUsername()
        {
            var vmNode = CreateVmInstanceNode(false);

            var settings = this.service.GetConnectionSettings(vmNode);

            settings.TypedCollection.AppUsername.Value = "sa";
            settings.TypedCollection.AppUsername.Value = null;
            settings.TypedCollection.AppUsername.Value = string.Empty;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => settings.TypedCollection.AppUsername.Value = "has spaces");
        }
    }
}
