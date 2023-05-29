//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Session.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Session.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Session.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Views.Credentials;
using Google.Solutions.Ssh.Auth;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Services.Session
{
    [TestFixture]
    public class TestSessionContextFactoryForSsh
    {
        private const string SampleEmail = "bob@example.com";
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private Mock<IProjectModelInstanceNode> CreateInstanceNodeMock(OperatingSystems os)
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(os);
            vmNode.SetupGet(n => n.Instance).Returns(SampleLocator);

            return vmNode;
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

        private Mock<IAuthorization> CreateAuthorizationMock()
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Email).Returns(SampleEmail);
            return authz;
        }

        private Mock<IProjectWorkspace> CreateProjectModelServiceMock(OperatingSystems os)
        {
            var modelService = new Mock<IProjectWorkspace>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.Is<ResourceLocator>(l => l == (ResourceLocator)SampleLocator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateInstanceNodeMock(os).Object);

            return modelService;
        }

        //---------------------------------------------------------------------
        // CreateSshSessionContext - key.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateSshSessionContextOpensSshKey()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);
            var keyStore = CreateKeyStoreAdapterMock();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                CreateProjectModelServiceMock(OperatingSystems.Linux).Object,
                keyStore.Object,
                new Mock<IKeyAuthorizationService>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<IAddressResolver>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallbackService>().Object,
                CreateSshSettingsRepository());

            using (await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            { }

            keyStore.Verify(k => k.OpenSshKeyPair(
                It.Is<SshKeyType>(t => t == SshKeyType.EcdsaNistp384),
                It.IsAny<IAuthorization>(),
                It.Is<bool>(create => true),
                It.IsAny<IWin32Window>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // CreateSshSessionContext - connection settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateSshSessionContextUsesConnectionSettings()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator.ProjectId, SampleLocator.Name);
            settings.SshPort.IntValue = 2222;
            settings.SshUsername.StringValue = "user";
            settings.SshConnectionTimeout.Value = (int)TimeSpan.FromSeconds(123).TotalSeconds;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                CreateProjectModelServiceMock(OperatingSystems.Linux).Object,
                CreateKeyStoreAdapterMock().Object,
                new Mock<IKeyAuthorizationService>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<IAddressResolver>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallbackService>().Object,
                CreateSshSettingsRepository());

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual(2222, context.Parameters.Port);
                Assert.AreEqual("user", context.Parameters.PreferredUsername);
                Assert.AreEqual(TimeSpan.FromSeconds(123), context.Parameters.ConnectionTimeout);
            }
        }

        //---------------------------------------------------------------------
        // CreateSshSessionContext - global settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateSshSessionContextUsesSshSettings()
        {
            var sshSettingsRepository = CreateSshSettingsRepository();
            var sshSettings = sshSettingsRepository.GetSettings();
            sshSettings.PublicKeyValidity.IntValue = (int)TimeSpan.FromDays(4).TotalSeconds;
            sshSettingsRepository.SetSettings(sshSettings);

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(
                    InstanceConnectionSettings
                        .CreateNew(SampleLocator.ProjectId, SampleLocator.Name)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                CreateProjectModelServiceMock(OperatingSystems.Linux).Object,
                CreateKeyStoreAdapterMock().Object,
                new Mock<IKeyAuthorizationService>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<IAddressResolver>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallbackService>().Object,
                sshSettingsRepository);

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual(TimeSpan.FromDays(4), context.Parameters.PublicKeyValidity);
            }
        }
    }
}
