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

using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Services;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.Ssh;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Ssh
{

    [TestFixture]
    public class TestSshConnectionService : ApplicationFixtureBase
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        private const string SampleEmail = "bob@example.com";
        private readonly InstanceLocator SampleLocator = new InstanceLocator("project-1", "zone-1", "instance-1");

        private Mock<IKeyStoreAdapter> keyStore;
        private Mock<ITunnelBrokerService> tunnelBrokerService;
        private Mock<ISshTerminalSessionBroker> sessionBroker;
        private Mock<IAuthorizedKeyService> authorizedKeyService;

        [SetUp]
        public void SetUp()
        {
            this.serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();

            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(1);

            this.tunnelBrokerService = new Mock<ITunnelBrokerService>();
            this.tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(tunnel.Object);
            this.serviceRegistry.AddSingleton<ITunnelBrokerService>(this.tunnelBrokerService.Object);

            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Email).Returns(SampleEmail);

            var authzAdapter = this.serviceRegistry.AddMock<IAuthorizationAdapter>();
            authzAdapter.SetupGet(a => a.Authorization)
                .Returns(authz.Object);

            this.keyStore = this.serviceRegistry.AddMock<IKeyStoreAdapter>();
            this.keyStore.Setup(k => k.CreateRsaKey(
                    It.IsAny<string>(),
                    It.IsAny<CngKeyUsages>(),
                    It.IsAny<bool>(),
                    It.IsAny<IWin32Window>()))
                .Returns(new RSACng());

            this.sessionBroker = this.serviceRegistry.AddMock<ISshTerminalSessionBroker>();
            this.authorizedKeyService = this.serviceRegistry.AddMock<IAuthorizedKeyService>();

            this.serviceRegistry.AddMock<IMainForm>();

            this.serviceRegistry.AddMock<IProjectModelService>()
                .Setup(p => p.GetNodeAsync(
                    It.Is<ResourceLocator>(l => l == (ResourceLocator)SampleLocator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateInstanceNodeMock().Object);

            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            this.serviceRegistry.AddSingleton(new SshSettingsRepository(
                hkcu.CreateSubKey(@"Software\Google\__Test")));
        }

        private Mock<IProjectModelInstanceNode> CreateInstanceNodeMock()
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Linux);
            vmNode.SetupGet(n => n.Instance).Returns(SampleLocator);
            return vmNode;
        }

        [Test]
        public async Task WhenSessionExists_ThenActivateOrConnectInstanceAsyncActivatesSession()
        {
            this.serviceRegistry.AddMock<IConnectionSettingsService>();

            this.sessionBroker.Setup(b => b.TryActivate(
                    It.Is<InstanceLocator>(l => l == SampleLocator)))
                .Returns(true);

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceAsync(vmNode.Object);

            sessionBroker.Verify(b => b.TryActivate(
                It.Is<InstanceLocator>(l => l == SampleLocator)), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncCreatesRsaKey()
        {
            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceAsync(vmNode.Object);

            this.keyStore.Verify(k => k.CreateRsaKey(
                It.Is<string>(name => name == "IAPDESKTOP_" + SampleEmail),
                It.Is<CngKeyUsages>(u => u == CngKeyUsages.Signing),
                It.Is<bool>(create => create),
                It.IsAny<IWin32Window>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncConnectsToConfiguredPort()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;

            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceAsync(vmNode.Object);

            this.tunnelBrokerService.Verify(s => s.ConnectAsync(
                It.Is<TunnelDestination>(d => d.RemotePort == 2222),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncUsesPreferredUsername()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;
            settings.SshUsername.StringValue = "bob";

            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceAsync(vmNode.Object);

            this.authorizedKeyService.Verify(s => s.AuthorizeKeyAsync(
                It.Is<InstanceLocator>(l => l == SampleLocator),
                It.IsAny<ISshKey>(),
                It.IsAny<TimeSpan>(),
                It.Is<string>(user => user == "bob"),
                It.IsAny<AuthorizeKeyMethods>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncUsesKeyValidityFromSettings()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;
            settings.SshUsername.StringValue = "bob";

            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var sshSettingsRepository = this.serviceRegistry.GetService<SshSettingsRepository>();
            var sshSettings = sshSettingsRepository.GetSettings();
            sshSettings.PublicKeyValidity.IntValue = (int)TimeSpan.FromDays(4).TotalSeconds;
            sshSettingsRepository.SetSettings(sshSettings);

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceAsync(vmNode.Object);

            this.authorizedKeyService.Verify(s => s.AuthorizeKeyAsync(
                It.Is<InstanceLocator>(l => l == SampleLocator),
                It.IsAny<ISshKey>(),
                It.Is<TimeSpan>(validity => validity == TimeSpan.FromDays(4)),
                It.IsAny<string>(),
                It.IsAny<AuthorizeKeyMethods>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncUsesConnectionTimeout()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;
            settings.SshConnectionTimeout.IntValue = (int)TimeSpan.FromSeconds(123).TotalSeconds;

            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceAsync(vmNode.Object);

            this.tunnelBrokerService.Verify(s => s.ConnectAsync(
                It.Is<TunnelDestination>(d => d.RemotePort == 2222),
                It.IsAny<ISshRelayPolicy>(),
                It.Is<TimeSpan>(t => t == TimeSpan.FromSeconds(123))), Times.Once);
        }

        [Test]
        public void WhenTunnelUnauthorized_ThenActivateOrConnectInstanceAsyncActivatesSessionThrowsConnectionFailedException()
        {
            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            this.tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>())).ThrowsAsync(new UnauthorizedException("mock"));

            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.Instance).Returns(SampleLocator);

            var service = new SshConnectionService(this.serviceRegistry);
            AssertEx.ThrowsAggregateException<ConnectionFailedException>(
                () => service.ActivateOrConnectInstanceAsync(vmNode.Object).Wait());
        }

        [Test]
        public void WhenKeyAuthorizationFails_ThenActivateOrConnectInstanceAsyncActivatesSessionThrowsSshKeyPushFailedException()
        {
            var settingsService = this.serviceRegistry.AddMock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            this.authorizedKeyService.Setup(a => a.AuthorizeKeyAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ISshKey>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthorizeKeyMethods>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SshKeyPushFailedException("mock", HelpTopics.ManagingOsLogin));

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(this.serviceRegistry);
            AssertEx.ThrowsAggregateException<SshKeyPushFailedException>(
                () => service.ActivateOrConnectInstanceAsync(vmNode.Object).Wait());
        }
    }
}
