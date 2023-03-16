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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Rdp
{
    [TestFixture]
    public class TestRdpConnectionService : ShellFixtureBase
    {
        private Mock<ITunnelBrokerService> CreateTunnelBrokerServiceMock()
        {
            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(1);

            var tunnelBrokerService = new Mock<ITunnelBrokerService>();
            tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>())).Returns(Task.FromResult(tunnel.Object));

            return tunnelBrokerService;
        }

        private Mock<IProjectModelInstanceNode> CreateInstanceNodeMock()
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);
            vmNode.SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            return vmNode;
        }

        //---------------------------------------------------------------------
        // Connect by node.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnectingByNodeAndPersistentCredentialsDisallowed_ThenPasswordIsClear()
        {
            var settings = InstanceConnectionSettings.CreateNew("project", "instance-1");
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var sessionBroker = new Mock<IRemoteDesktopSessionBroker>();
            sessionBroker.Setup(s => s.Connect(
                    It.IsAny<InstanceLocator>(),
                    "localhost",
                    It.IsAny<ushort>(),
                    It.IsAny<InstanceConnectionSettings>()))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                sessionBroker.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                new Mock<ISelectCredentialsWorkflow>().Object);

            var session = await service
                .ActivateOrConnectInstanceAsync(vmNode.Object, false)
                .ConfigureAwait(false);
            Assert.IsNotNull(session);

            sessionBroker.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<InstanceConnectionSettings>(i =>
                    i.RdpUsername.StringValue == "existinguser" &&
                    i.RdpPassword.ClearTextValue == "")), Times.Once);
        }

        [Test]
        public async Task WhenConnectingByNodeAndPersistentCredentialsAllowed_ThenCredentialsAreUsed()
        {
            var settings = InstanceConnectionSettings.CreateNew("project", "instance-1");
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            bool settingsSaved = false;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => settingsSaved = true));

            var vmNode = CreateInstanceNodeMock();

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var sessionBroker = new Mock<IRemoteDesktopSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(
                    It.IsAny<InstanceLocator>(),
                    "localhost",
                    It.IsAny<ushort>(),
                    It.IsAny<InstanceConnectionSettings>()))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                sessionBroker.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                new Mock<ISelectCredentialsWorkflow>().Object);

            var session = await service
                .ActivateOrConnectInstanceAsync(vmNode.Object, true)
                .ConfigureAwait(false);
            Assert.IsNotNull(session);

            sessionBroker.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<InstanceConnectionSettings>(i =>
                    i.RdpUsername.StringValue == "existinguser" &&
                    i.RdpPassword.ClearTextValue == "password")), Times.Once);

            Assert.IsTrue(settingsSaved);
        }

        //---------------------------------------------------------------------
        // Connect by URL.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnectingByUrlWithoutUsernameAndNoCredentialsExist_ThenConnectionIsMadeWithoutUsername()
        {
            var settingsService = new Mock<IConnectionSettingsService>();

            var credentialPrompt = new Mock<ISelectCredentialsWorkflow>();
            credentialPrompt.Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    It.IsAny<bool>())); // Nop -> Connect without configuring credentials.

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IProjectModelNode)null); // Not found

            var sessionBroker = new Mock<IRemoteDesktopSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(
                    It.IsAny<InstanceLocator>(),
                    "localhost",
                    It.IsAny<ushort>(),
                    It.IsAny<InstanceConnectionSettings>()))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                sessionBroker.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                credentialPrompt.Object);

            var session = await service
                .ActivateOrConnectInstanceAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance"))
                .ConfigureAwait(false);
            Assert.IsNotNull(session);

            sessionBroker.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<InstanceConnectionSettings>(i => i.RdpUsername.Value == null)), Times.Once);
            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Never);
        }

        [Test]
        public async Task WhenConnectingByUrlWithUsernameAndNoCredentialsExist_ThenConnectionIsMadeWithThisUsername()
        {
            var settingsService = new Mock<IConnectionSettingsService>();

            var credentialPrompt = new Mock<ISelectCredentialsWorkflow>();
            credentialPrompt
                .Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    It.IsAny<bool>())); // Nop -> Connect without configuring credentials.

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IProjectModelNode)null); // Not found

            var sessionBroker = new Mock<IRemoteDesktopSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(
                    It.IsAny<InstanceLocator>(),
                    "localhost",
                    It.IsAny<ushort>(),
                    It.IsAny<InstanceConnectionSettings>()))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                sessionBroker.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                credentialPrompt.Object);

            var session = await service
                .ActivateOrConnectInstanceAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance?username=john%20doe"))
                .ConfigureAwait(false);
            Assert.IsNotNull(session);

            sessionBroker.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<InstanceConnectionSettings>(i => i.RdpUsername.StringValue == "john doe")), Times.Once);
            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Never);
        }

        [Test]
        public async Task WhenConnectingByUrlWithUsernameAndCredentialsExist_ThenConnectionIsMadeWithUsernameFromUrl()
        {
            var settings = InstanceConnectionSettings.CreateNew("project", "instance-1");
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            var credentialPrompt = new Mock<ISelectCredentialsWorkflow>();
            credentialPrompt
                .Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    It.IsAny<bool>()));

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var sessionBroker = new Mock<IRemoteDesktopSessionBroker>();
            sessionBroker.Setup(s => s.Connect(
                    It.IsAny<InstanceLocator>(),
                    "localhost",
                    It.IsAny<ushort>(),
                    It.IsAny<InstanceConnectionSettings>()))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                sessionBroker.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                credentialPrompt.Object);

            var session = await service
                .ActivateOrConnectInstanceAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe"))
                .ConfigureAwait(false);
            Assert.IsNotNull(session);

            sessionBroker.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<InstanceConnectionSettings>(i => i.RdpUsername.StringValue == "john doe")), Times.Once);
            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Once);
        }
    }
}
