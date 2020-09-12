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
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services
{
    [TestFixture]
    public class TestConnectionSettingsService : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser, 
            RegistryView.Default);

        private ConnectionSettingsService service;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var repository = new ConnectionSettingsRepository(hkcu.CreateSubKey(TestKeyPath));
            repository.SetProjectSettings(new ProjectConnectionSettings()
            {
                ProjectId = "project-1",
                Domain = "project-domain"
            });

            this.service = new ConnectionSettingsService(repository);
        }

        [Test]
        public void WhenNodeUnsupported_ThenIsConnectionSettingsEditorAvailableReturnsFalse()
        {
            Assert.IsFalse(service.IsConnectionSettingsEditorAvailable(
                new Mock<IProjectExplorerNode>().Object));
            Assert.IsFalse(service.IsConnectionSettingsEditorAvailable(
                new Mock<IProjectExplorerCloudNode>().Object));
        }

        [Test]
        public void WhenNodeSupported_ThenIsConnectionSettingsEditorAvailableReturnsTrue()
        {
            Assert.IsTrue(service.IsConnectionSettingsEditorAvailable(
                new Mock<IProjectExplorerProjectNode>().Object));
            Assert.IsTrue(service.IsConnectionSettingsEditorAvailable(
                new Mock<IProjectExplorerZoneNode>().Object));
            Assert.IsTrue(service.IsConnectionSettingsEditorAvailable(
                new Mock<IProjectExplorerVmInstanceNode>().Object));
        }

        //---------------------------------------------------------------------
        // Project.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingProjectSettings_ThenExistingProjectSettingIsVisible()
        {
            var projectNode = new Mock<IProjectExplorerProjectNode>();
            projectNode.SetupGet(n => n.ProjectId).Returns("project-1");

            var editor = service.GetConnectionSettingsEditor(projectNode.Object);
            Assert.AreEqual("project-domain", editor.Domain);
        }

        [Test]
        public void WhenChangingProjectSetting_ThenSettingIsSaved()
        {
            var projectNode = new Mock<IProjectExplorerProjectNode>();
            projectNode.SetupGet(n => n.ProjectId).Returns("project-1");

            var firstEditor = service.GetConnectionSettingsEditor(projectNode.Object);
            firstEditor.Username = "bob";
            firstEditor.SaveChanges();

            var secondEditor = service.GetConnectionSettingsEditor(projectNode.Object);
            Assert.AreEqual("bob", secondEditor.Username);
        }

        //---------------------------------------------------------------------
        // Zone.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingZoneSettings_ThenExistingProjectSettingIsVisible()
        {
            var zoneNode = new Mock<IProjectExplorerZoneNode>();
            zoneNode.SetupGet(n => n.ProjectId).Returns("project-1");
            zoneNode.SetupGet(n => n.ZoneId).Returns("zone-1");

            var editor = service.GetConnectionSettingsEditor(zoneNode.Object);
            Assert.AreEqual("project-domain", editor.Domain);
        }

        [Test]
        public void WhenChangingZoneSetting_ThenSettingIsSaved()
        {
            var zoneNode = new Mock<IProjectExplorerZoneNode>();
            zoneNode.SetupGet(n => n.ProjectId).Returns("project-1");
            zoneNode.SetupGet(n => n.ZoneId).Returns("zone-1");

            var firstEditor = service.GetConnectionSettingsEditor(zoneNode.Object);
            firstEditor.Username = "bob";
            firstEditor.SaveChanges();

            var secondEditor = service.GetConnectionSettingsEditor(zoneNode.Object);
            Assert.AreEqual("bob", secondEditor.Username);
        }

        //---------------------------------------------------------------------
        // VM.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReadingVmInstanceSettings_ThenExistingProjectSettingIsVisible()
        {
            var vmNode = new Mock<IProjectExplorerVmInstanceNode>();
            vmNode.SetupGet(n => n.ProjectId).Returns("project-1");
            vmNode.SetupGet(n => n.ZoneId).Returns("zone-1");
            vmNode.SetupGet(n => n.InstanceName).Returns("instance-1");
            vmNode.SetupGet(n => n.Reference).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var editor = service.GetConnectionSettingsEditor(vmNode.Object);
            Assert.AreEqual("project-domain", editor.Domain);
        }

        [Test]
        public void WhenChangingVmInstanceSetting_ThenSettingIsSaved()
        {
            var vmNode = new Mock<IProjectExplorerVmInstanceNode>();
            vmNode.SetupGet(n => n.ProjectId).Returns("project-1");
            vmNode.SetupGet(n => n.ZoneId).Returns("zone-1");
            vmNode.SetupGet(n => n.InstanceName).Returns("instance-1");
            vmNode.SetupGet(n => n.Reference).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var firstEditor = service.GetConnectionSettingsEditor(vmNode.Object);
            firstEditor.Username = "bob";
            firstEditor.SaveChanges();

            var secondEditor = service.GetConnectionSettingsEditor(vmNode.Object);
            Assert.AreEqual("bob", secondEditor.Username);
        }
    }
}
