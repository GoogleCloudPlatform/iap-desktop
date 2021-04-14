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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.ConnectionSettings
{
    [TestFixture]
    public class TestConnectionSettingsViewModel : CommonFixtureBase
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

            var projectRepository = new ProjectRepository(hkcu.CreateSubKey(TestKeyPath));
            var settingsRepository = new ConnectionSettingsRepository(projectRepository);
            this.service = new ConnectionSettingsService(settingsRepository);

            // Set some initial project settings.
            projectRepository.AddProject(new ProjectLocator(SampleProjectId));

            var projectSettings = settingsRepository.GetProjectSettings(SampleProjectId);
            projectSettings.RdpDomain.Value = "project-domain";
            settingsRepository.SetProjectSettings(projectSettings);
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceIsRunning_ThenInformationBarIsShown()
        {
            var broker = new Mock<IGlobalSessionBroker>();
            broker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(true);
            var viewModel = new ConnectionSettingsViewModel(
                this.service,
                broker.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);

            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsTrue(viewModel.IsInformationBarVisible);
        }

        [Test]
        public async Task WhenInstanceIsNotRunning_ThenInformationBarIsNotShown()
        {
            var broker = new Mock<IGlobalSessionBroker>();
            broker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                this.service,
                broker.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);

            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenGridIsDisabled()
        {
            var broker = new Mock<IGlobalSessionBroker>();
            broker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                this.service,
                broker.Object);

            var node = new Mock<IProjectModelCloudNode>();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(ConnectionSettingsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenGridIsPopulated()
        {
            var broker = new Mock<IGlobalSessionBroker>();
            broker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                this.service,
                broker.Object);

            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator(SampleProjectId));
            node.SetupGet(n => n.DisplayName).Returns("display");

            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsNotNull(viewModel.InspectedObject);
            StringAssert.Contains(ConnectionSettingsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("display", viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenGridIsPopulated()
        {
            var broker = new Mock<IGlobalSessionBroker>();
            broker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                this.service,
                broker.Object);

            var node = new Mock<IProjectModelZoneNode>();
            node.SetupGet(n => n.Zone).Returns(new ZoneLocator(SampleProjectId, "zone-1"));
            node.SetupGet(n => n.DisplayName).Returns("display");

            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsNotNull(viewModel.InspectedObject);
            StringAssert.Contains(ConnectionSettingsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("display", viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenGridIsPopulated()
        {
            var broker = new Mock<IGlobalSessionBroker>();
            broker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                this.service,
                broker.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            node.SetupGet(n => n.DisplayName).Returns("display");
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);

            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsNotNull(viewModel.InspectedObject);
            StringAssert.Contains(ConnectionSettingsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("display", viewModel.WindowTitle);
        }
    }
}
