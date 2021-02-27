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

using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.ConnectionSettings;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.ConnectionSettings
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

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceIsRunning_ThenInformationBarIsShown()
        {
            var viewModel = new ConnectionSettingsViewModel(this.service);

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");
            node.SetupGet(n => n.IsConnected).Returns(true);
            node.SetupGet(n => n.IsWindowsInstance).Returns(true);

            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsTrue(viewModel.IsInformationBarVisible);
        }

        [Test]
        public async Task WhenInstanceIsNotRunning_ThenInformationBarIsNotShown()
        {
            var viewModel = new ConnectionSettingsViewModel(this.service);

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");
            node.SetupGet(n => n.IsConnected).Returns(false);
            node.SetupGet(n => n.IsWindowsInstance).Returns(true);

            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenGridIsDisabled()
        {
            var viewModel = new ConnectionSettingsViewModel(this.service);

            var node = new Mock<IProjectExplorerCloudNode>();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(ConnectionSettingsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenGridIsPopulated()
        {
            var viewModel = new ConnectionSettingsViewModel(this.service);

            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
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
            var viewModel = new ConnectionSettingsViewModel(this.service);

            var node = new Mock<IProjectExplorerZoneNode>();
            node.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
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
            var viewModel = new ConnectionSettingsViewModel(this.service);

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns(SampleProjectId);
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");
            node.SetupGet(n => n.DisplayName).Returns("display");
            node.SetupGet(n => n.IsConnected).Returns(false);
            node.SetupGet(n => n.IsWindowsInstance).Returns(true);

            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsNotNull(viewModel.InspectedObject);
            StringAssert.Contains(ConnectionSettingsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("display", viewModel.WindowTitle);
        }
    }
}
