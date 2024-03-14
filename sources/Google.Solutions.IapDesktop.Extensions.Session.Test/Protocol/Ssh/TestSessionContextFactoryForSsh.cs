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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
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

        private IRepository<ISshSettings> CreateSshSettingsRepository(
            bool usePersistentKey,
            TimeSpan keyValidity)
        {
            var keyTypeSetting = new Mock<ISetting<SshKeyType>>();
            keyTypeSetting.SetupGet(s => s.Value).Returns(SshKeyType.Rsa3072);

            var validitySetting = new Mock<ISetting<int>>();
            validitySetting.SetupGet(s => s.Value).Returns((int)keyValidity.TotalSeconds);

            var usePersistentKeySetting = new Mock<ISetting<bool>>();
            usePersistentKeySetting.SetupGet(s => s.Value).Returns(usePersistentKey);

            var localeSetting = new Mock<ISetting<bool>>();

            var settings = new Mock<ISshSettings>();
            settings.SetupGet(s => s.PublicKeyType).Returns(keyTypeSetting.Object);
            settings.SetupGet(s => s.PublicKeyValidity).Returns(validitySetting.Object);
            settings.SetupGet(s => s.IsPropagateLocaleEnabled).Returns(localeSetting.Object);
            settings.SetupGet(s => s.UsePersistentKey).Returns(usePersistentKeySetting.Object);

            var repository = new Mock<IRepository<ISshSettings>>();
            repository
                .Setup(r => r.GetSettings())
                .Returns(settings.Object);

            return repository.Object;
        }

        private Mock<IKeyStore> CreateKeyStoreMock()
        {
            var keyStore = new Mock<IKeyStore>();
            keyStore
                .SetupGet(k => k.Provider)
                .Returns(CngProvider.MicrosoftSoftwareKeyStorageProvider);
            keyStore
                .Setup(k => k.OpenKey(
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.IsAny<KeyType>(),
                    It.IsAny<CngKeyUsages>(),
                    It.IsAny<bool>()))
                .Returns(new RSACng(3072).Key); // Matching setting

            return keyStore;
        }

        private Mock<IAuthorization> CreateAuthorizationMock()
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(a => a.Username)
                .Returns(SampleEmail);

            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Session)
                .Returns(session.Object);

            return authorization;
        }

        //---------------------------------------------------------------------
        // CreateSshSessionContext - connection settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPublicKeyAuthEnabled_ThenCreateSshSessionContextUsesPlatformCredential()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.SshPort.Value = 2222;
            settings.SshUsername.Value = "user";
            settings.SshConnectionTimeout.Value = (int)TimeSpan.FromSeconds(123).TotalSeconds;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                CreateKeyStoreMock().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(true, TimeSpan.FromMinutes(1)));

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.IsTrue(context.UsePlatformManagedCredential);

                Assert.AreEqual(2222, context.Parameters.Port);
                Assert.AreEqual("user", context.Parameters.PreferredUsername);
                Assert.AreEqual(TimeSpan.FromSeconds(123), context.Parameters.ConnectionTimeout);
            }
        }

        [Test]
        public async Task WhenPublicKeyAuthDisabled_ThenCreateSshSessionContextUsesPasswordCredential(
            [Values("user", "", null)] string username)
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.SshPublicKeyAuthentication.Value = SshPublicKeyAuthentication.Disabled;
            settings.SshUsername.Value = username;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService.Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                CreateKeyStoreMock().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(true, TimeSpan.FromMinutes(1)));

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.IsFalse(context.UsePlatformManagedCredential);
                Assert.IsNull(context.Parameters.PreferredUsername);

                var credential = await context
                    .AuthorizeCredentialAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsInstanceOf<StaticPasswordCredential>(credential);
                Assert.AreEqual(
                    string.IsNullOrEmpty(username) ? "bob" : username,
                    credential.Username);
                Assert.AreEqual(
                    string.Empty,
                    ((StaticPasswordCredential)credential).Password.AsClearText());
            }
        }

        //---------------------------------------------------------------------
        // CreateSshSessionContext - global settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateSshSessionContextUsesSshSettings()
        {
            var sshSettingsRepository = CreateSshSettingsRepository(true, TimeSpan.FromDays(4));
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(new ConnectionSettings(SampleLocator)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                CreateKeyStoreMock().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                sshSettingsRepository);

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual(TimeSpan.FromDays(4), context.Parameters.PublicKeyValidity);
            }
        }

        [Test]
        public async Task WhenUsePersistentKeyIsTrue_ThenCreateSshSessionContextOpensSshKey()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(new ConnectionSettings(SampleLocator)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);
            var keyStore = CreateKeyStoreMock();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                keyStore.Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(true, TimeSpan.FromMinutes(123)));

            using (var context = await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual(TimeSpan.FromMinutes(123), context.Parameters.PublicKeyValidity);
            }

            keyStore.Verify(
                k => k.OpenKey(
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.Is<KeyType>(t => t.Algorithm == CngAlgorithm.Rsa),
                    CngKeyUsages.Signing,
                    false),
                Times.Once);
        }

        [Test]
        public async Task WhenUsePersistentKeyIsFalse_ThenCreateSshSessionContextUsesEphemeralKey()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(new ConnectionSettings(SampleLocator)
                        .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock(OperatingSystems.Linux);
            var keyStore = CreateKeyStoreMock();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                CreateAuthorizationMock().Object,
                keyStore.Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(false, TimeSpan.FromMinutes(123)));

            using (var context = await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual(
                    SessionContextFactory.EphemeralKeyValidity,
                    context.Parameters.PublicKeyValidity);
            }

            keyStore.Verify(
                k => k.OpenKey(
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.Is<KeyType>(t => t.Algorithm == CngAlgorithm.Rsa),
                    CngKeyUsages.Signing,
                    false),
                Times.Never);
        }
    }
}
