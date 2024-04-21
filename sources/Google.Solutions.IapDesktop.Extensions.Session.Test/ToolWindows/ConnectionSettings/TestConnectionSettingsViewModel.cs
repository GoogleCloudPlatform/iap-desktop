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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.ConnectionSettings;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.ConnectionSettings
{
    [TestFixture]
    public class TestConnectionSettingsViewModel
    {
        private const string SampleProjectId = "project-1";
        private const string TestKeyPath = @"Software\Google\__Test";
        private static readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        private static ConnectionSettingsService CreateConnectionSettingsService()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var projectRepository = new ProjectRepository(hkcu.CreateSubKey(TestKeyPath));
            var settingsRepository = new ConnectionSettingsRepository(projectRepository);

            // Set some initial project settings.
            projectRepository.AddProject(new ProjectLocator(SampleProjectId));

            var projectSettings = settingsRepository.GetProjectSettings(new ProjectLocator(SampleProjectId));
            projectSettings.RdpDomain.Value = "project-domain";
            settingsRepository.SetProjectSettings(projectSettings);

            return new ConnectionSettingsService(settingsRepository);
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceIsRunning_ThenInformationTextIsSet()
        {
            var service = CreateConnectionSettingsService();

            var broker = new Mock<ISessionBroker>();
            broker
                .Setup(b => b.IsConnected(It.IsAny<InstanceLocator>()))
                .Returns(true);
            var viewModel = new ConnectionSettingsViewModel(
                service,
                broker.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.AreEqual(
                ConnectionSettingsViewModel.RequiresReconnectWarning,
                viewModel.InformationText.Value);
        }

        [Test]
        public async Task WhenInstanceIsNotRunning_ThenInformationTextIsNull()
        {
            var service = CreateConnectionSettingsService();

            var broker = new Mock<ISessionBroker>();
            broker
                .Setup(b => b.IsConnected(It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                service,
                broker.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsNull(viewModel.InformationText.Value);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenInspectedObjectIsNull()
        {
            var service = CreateConnectionSettingsService();

            var broker = new Mock<ISessionBroker>();
            broker
                .Setup(b => b.IsConnected(It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                service,
                broker.Object);

            var node = new Mock<IProjectModelCloudNode>();
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsNull(viewModel.InformationText.Value);
            Assert.IsNull(viewModel.InspectedObject.Value);
            Assert.AreEqual(
                ConnectionSettingsViewModel.DefaultWindowTitle,
                viewModel.WindowTitle.Value);
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenInspectedObjectIsSet()
        {
            var service = CreateConnectionSettingsService();

            var broker = new Mock<ISessionBroker>();
            broker
                .Setup(b => b.IsConnected(It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                service,
                broker.Object);

            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator(SampleProjectId));
            node.SetupGet(n => n.DisplayName).Returns("display");

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsNotNull(viewModel.InspectedObject.Value);
            StringAssert.Contains(
                ConnectionSettingsViewModel.DefaultWindowTitle,
                viewModel.WindowTitle.Value);
            StringAssert.Contains(
                "display",
                viewModel.WindowTitle.Value);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenInspectedObjectIsSet()
        {
            var service = CreateConnectionSettingsService();

            var broker = new Mock<ISessionBroker>();
            broker
                .Setup(b => b.IsConnected(It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                service,
                broker.Object);

            var node = new Mock<IProjectModelZoneNode>();
            node.SetupGet(n => n.Zone).Returns(new ZoneLocator(SampleProjectId, "zone-1"));
            node.SetupGet(n => n.DisplayName).Returns("display");

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsNotNull(viewModel.InspectedObject.Value);
            StringAssert.Contains(
                ConnectionSettingsViewModel.DefaultWindowTitle,
                viewModel.WindowTitle.Value);
            StringAssert.Contains(
                "display",
                viewModel.WindowTitle.Value);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenInspectedObjectIsSet()
        {
            var service = CreateConnectionSettingsService();

            var broker = new Mock<ISessionBroker>();
            broker
                .Setup(b => b.IsConnected(It.IsAny<InstanceLocator>()))
                .Returns(false);
            var viewModel = new ConnectionSettingsViewModel(
                service,
                broker.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(SampleProjectId, "zone-1", "instance-1"));
            node.SetupGet(n => n.DisplayName).Returns("display");
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsNotNull(viewModel.InspectedObject.Value);
            StringAssert.Contains(
                ConnectionSettingsViewModel.DefaultWindowTitle,
                viewModel.WindowTitle.Value);
            StringAssert.Contains(
                "display",
                viewModel.WindowTitle.Value);
        }
    }
}
