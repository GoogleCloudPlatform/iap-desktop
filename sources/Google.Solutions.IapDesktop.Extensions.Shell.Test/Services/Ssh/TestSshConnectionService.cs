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
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Common;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Ssh
{
    [TestFixture]
    public class TestSshConnectionService : ShellFixtureBase
    {
        private const string SampleEmail = "bob@example.com";
        private static readonly InstanceLocator SampleLocator = new InstanceLocator("project-1", "zone-1", "instance-1");

        private Mock<ITunnelBrokerService> CreateTunnelBrokerServiceMock()
        {
            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(1);

            var tunnelBrokerService = new Mock<ITunnelBrokerService>();
            tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(tunnel.Object);

            return tunnelBrokerService;
        }

        private Mock<IAuthorizationSource> CreateAuthorizationSourceMock()
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Email).Returns(SampleEmail);

            var authzSource = new Mock<IAuthorizationSource>();
            authzSource.SetupGet(a => a.Authorization)
                .Returns(authz.Object);

            return authzSource;
        }

        private Mock<IKeyStoreAdapter> CreateKeyStoreAdapterMock()
        {
            var keyStore = new Mock<IKeyStoreAdapter>();
            keyStore
                .Setup(k => k.OpenSshKeyPair(
                    It.IsAny<SshKeyType>(),
                    It.IsAny<IAuthorization>(),
                    It.IsAny<bool>(),
                    It.IsAny<IWin32Window>()))
                .Returns(RsaSshKeyPair.NewEphemeralKey(1024));

            return keyStore;
        }

        private Mock<IProjectModelService> CreateProjectModelServiceMock()
        {
            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.Is<ResourceLocator>(l => l == (ResourceLocator)SampleLocator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateInstanceNodeMock().Object);

            return modelService;
        }

        private SshSettingsRepository CreateSshSettingsRepository()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(@"Software\Google\__Test", false);

            return new SshSettingsRepository(
                hkcu.CreateSubKey(@"Software\Google\__Test"),
                null,
                null,
                Profile.SchemaVersion.Current);
        }

        private Mock<IProjectModelInstanceNode> CreateInstanceNodeMock()
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Linux);
            vmNode.SetupGet(n => n.Instance).Returns(SampleLocator);
            return vmNode;
        }

        //---------------------------------------------------------------------
        // ActivateOrConnectInstanceAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSessionExists_ThenActivateOrConnectInstanceAsyncActivatesSession()
        {
            var sessionBroker = new Mock<ISshTerminalSessionBroker>();
            sessionBroker.Setup(b => b.TryActivate(
                    It.Is<InstanceLocator>(l => l == SampleLocator)))
                .Returns(true);
            sessionBroker.SetupGet(b => b.ActiveSession)
                .Returns(new Mock<ISshTerminalSession>().Object);

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                sessionBroker.Object,
                CreateTunnelBrokerServiceMock().Object,
                new Mock<IConnectionSettingsService>().Object,
                new Mock<IKeyAuthorizationService>().Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            var session = await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);

            sessionBroker.Verify(b => b.TryActivate(
                It.Is<InstanceLocator>(l => l == SampleLocator)), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncOpensSshKey()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();
            var keyStore = CreateKeyStoreAdapterMock();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                CreateTunnelBrokerServiceMock().Object,
                settingsService.Object,
                new Mock<IKeyAuthorizationService>().Object,
                keyStore.Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            keyStore.Verify(k => k.OpenSshKeyPair(
                It.Is<SshKeyType>(t => t == SshKeyType.EcdsaNistp384),
                It.IsAny<IAuthorization>(),
                It.Is<bool>(create => true),
                It.IsAny<IWin32Window>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncConnectsToConfiguredPort()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();
            var tunnelBrokerService = CreateTunnelBrokerServiceMock();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                tunnelBrokerService.Object,
                settingsService.Object,
                new Mock<IKeyAuthorizationService>().Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            tunnelBrokerService.Verify(s => s.ConnectAsync(
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

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var authorizedKeyService = new Mock<IKeyAuthorizationService>();
            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                CreateTunnelBrokerServiceMock().Object,
                settingsService.Object,
                authorizedKeyService.Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            authorizedKeyService.Verify(s => s.AuthorizeKeyAsync(
                It.Is<InstanceLocator>(l => l == SampleLocator),
                It.IsAny<ISshKeyPair>(),
                It.IsAny<TimeSpan>(),
                It.Is<string>(user => user == "bob"),
                It.IsAny<KeyAuthorizationMethods>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncUsesKeyValidityFromSettings()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;
            settings.SshUsername.StringValue = "bob";

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var sshSettingsRepository = CreateSshSettingsRepository();
            var sshSettings = sshSettingsRepository.GetSettings();
            sshSettings.PublicKeyValidity.IntValue = (int)TimeSpan.FromDays(4).TotalSeconds;
            sshSettingsRepository.SetSettings(sshSettings);

            var vmNode = CreateInstanceNodeMock();
            var authorizedKeyService = new Mock<IKeyAuthorizationService>();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                CreateTunnelBrokerServiceMock().Object,
                settingsService.Object,
                authorizedKeyService.Object,
                CreateKeyStoreAdapterMock().Object,
                sshSettingsRepository,
                new SynchronousJobService());

            await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            authorizedKeyService.Verify(s => s.AuthorizeKeyAsync(
                It.Is<InstanceLocator>(l => l == SampleLocator),
                It.IsAny<ISshKeyPair>(),
                It.Is<TimeSpan>(validity => validity == TimeSpan.FromDays(4)),
                It.IsAny<string>(),
                It.IsAny<KeyAuthorizationMethods>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenNoSessionExists_ThenActivateOrConnectInstanceAsyncUsesConnectionTimeout()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;
            settings.SshConnectionTimeout.IntValue = (int)TimeSpan.FromSeconds(123).TotalSeconds;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();
            var tunnelBrokerService = CreateTunnelBrokerServiceMock();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                tunnelBrokerService.Object,
                settingsService.Object,
                new Mock<IKeyAuthorizationService>().Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            tunnelBrokerService.Verify(s => s.ConnectAsync(
                It.Is<TunnelDestination>(d => d.RemotePort == 2222),
                It.IsAny<ISshRelayPolicy>(),
                It.Is<TimeSpan>(t => t == TimeSpan.FromSeconds(123))), Times.Once);
        }

        [Test]
        public void WhenTunnelUnauthorized_ThenActivateOrConnectInstanceAsyncActivatesSessionThrowsConnectionFailedException()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var tunnelBrokerService = CreateTunnelBrokerServiceMock();
            tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>())).ThrowsAsync(new SshRelayDeniedException("mock"));

            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.Instance).Returns(SampleLocator);

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                tunnelBrokerService.Object,
                settingsService.Object,
                new Mock<IKeyAuthorizationService>().Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            ExceptionAssert.ThrowsAggregateException<ConnectionFailedException>(
                () => service.ActivateOrConnectInstanceAsync(vmNode.Object).Wait());
        }

        [Test]
        public void WhenKeyAuthorizationFails_ThenActivateOrConnectInstanceAsyncActivatesSessionThrowsSshKeyPushFailedException()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var authorizedKeyService = new Mock<IKeyAuthorizationService>();
            authorizedKeyService.Setup(a => a.AuthorizeKeyAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ISshKeyPair>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    It.IsAny<KeyAuthorizationMethods>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SshKeyPushFailedException("mock", HelpTopics.ManagingOsLogin));

            var vmNode = CreateInstanceNodeMock();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                new Mock<ISshTerminalSessionBroker>().Object,
                CreateTunnelBrokerServiceMock().Object,
                settingsService.Object,
                authorizedKeyService.Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            ExceptionAssert.ThrowsAggregateException<SshKeyPushFailedException>(
                () => service.ActivateOrConnectInstanceAsync(vmNode.Object).Wait());
        }

        //---------------------------------------------------------------------
        // ConnectInstanceAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSessionExists_ThenConnectInstanceAsyncCreatesNewSessionSession()
        {
            var sessionBroker = new Mock<ISshTerminalSessionBroker>();
            sessionBroker
                .Setup(b => b.TryActivate(It.Is<InstanceLocator>(l => l == SampleLocator)))
                .Returns(false);
            sessionBroker
                .Setup(b => b.ConnectAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<AuthorizedKeyPair>(),
                    It.IsAny<CultureInfo>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();
            var tunnelBrokerService = CreateTunnelBrokerServiceMock();

            var service = new SshConnectionService(
                new Mock<IMainForm>().Object,
                CreateAuthorizationSourceMock().Object,
                CreateProjectModelServiceMock().Object,
                sessionBroker.Object,
                tunnelBrokerService.Object,
                settingsService.Object,
                new Mock<IKeyAuthorizationService>().Object,
                CreateKeyStoreAdapterMock().Object,
                CreateSshSettingsRepository(),
                new SynchronousJobService());

            var session = await service
                .ActivateOrConnectInstanceAsync(vmNode.Object)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);

            tunnelBrokerService.Verify(s => s.ConnectAsync(
                It.Is<TunnelDestination>(d => d.RemotePort == 22),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
