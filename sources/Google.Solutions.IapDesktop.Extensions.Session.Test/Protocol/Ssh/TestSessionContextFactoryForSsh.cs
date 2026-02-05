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
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Ssh;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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

        private static Mock<IProjectModelInstanceNode> CreateInstanceNodeMock(OperatingSystems os)
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(os);
            vmNode.SetupGet(n => n.Instance).Returns(SampleLocator);

            return vmNode;
        }

        private static IRepository<ISshSettings> CreateSshSettingsRepository(
            IDictionary<string, string> settings)
        {
            var repository = new Mock<IRepository<ISshSettings>>();
            repository
                .Setup(r => r.GetSettings())
                .Returns(new SshSettingsRepository.SshSettings(
                    new DictionarySettingsStore(settings),
                    UserProfile.SchemaVersion.Current));

            return repository.Object;
        }

        private static Mock<IKeyStore> CreateKeyStoreMock()
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

        private static Mock<IAuthorization> CreateAuthorizationMock()
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
        public async Task CreateSshSessionContext_WhenPublicKeyAuthEnabled()
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
                new Mock<IRdpCredentialEditorFactory>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(new Dictionary<string, string>
                {
                    { "UsePersistentKey", "true" },
                    { "PublicKeyValidity", "60" } }
                ));

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.That(context.UsePlatformManagedCredential, Is.True);

                Assert.That(context.Parameters.Port, Is.EqualTo(2222));
                Assert.That(context.Parameters.PreferredUsername, Is.EqualTo("user"));
                Assert.That(context.Parameters.ConnectionTimeout, Is.EqualTo(TimeSpan.FromSeconds(123)));
            }
        }

        [Test]
        public async Task CreateSshSessionContext_WhenPublicKeyAuthDisabled(
            [Values("user", "", null)] string? username)
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
                new Mock<IRdpCredentialEditorFactory>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(new Dictionary<string, string>
                {
                    { "UsePersistentKey", "true" },
                    { "PublicKeyValidity", "60" } }
                ));

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.That(context.UsePlatformManagedCredential, Is.False);
                Assert.IsNull(context.Parameters.PreferredUsername);

                var credential = await context
                    .AuthorizeCredentialAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsInstanceOf<StaticPasswordCredential>(credential);
                Assert.That(
                    credential.Username, Is.EqualTo(string.IsNullOrEmpty(username) ? "bob" : username));
                Assert.That(
                    ((StaticPasswordCredential)credential).Password.ToClearText(), Is.EqualTo(string.Empty));
            }
        }

        //---------------------------------------------------------------------
        // CreateSshSessionContext - global settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateSshSessionContext_UsesSshSettings()
        {
            var sshSettingsRepository = CreateSshSettingsRepository(
                new Dictionary<string, string>
                {
                    { "UsePersistentKey", "true" },
                    { "PublicKeyValidity", "86400" },
                    { "EnableFileAccess", "false" } 
                });
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
                new Mock<IRdpCredentialEditorFactory>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                sshSettingsRepository);

            using (var context = (SshContext)await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.That(context.Parameters.PublicKeyValidity, Is.EqualTo(TimeSpan.FromDays(1)));
                Assert.That(context.Parameters.EnableFileAccess, Is.False);
            }
        }

        [Test]
        public async Task CreateSshSessionContext_WhenUsePersistentKeyIsTrue()
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
                new Mock<IRdpCredentialEditorFactory>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(new Dictionary<string, string>
                {
                    { "UsePersistentKey", "true" },
                    { "PublicKeyValidity", "600" },
                    { "PublicKeyType", "1" }
                }));

            using (var context = await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.That(context.Parameters.PublicKeyValidity, Is.EqualTo(TimeSpan.FromMinutes(10)));
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
        public async Task CreateSshSessionContext_WhenUsePersistentKeyIsFalse()
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
                new Mock<IRdpCredentialEditorFactory>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository(new Dictionary<string, string>
                {
                    { "UsePersistentKey", "false" },
                    { "PublicKeyValidity", "600" } }
                ));

            using (var context = await factory
                .CreateSshSessionContextAsync(vmNode.Object, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.That(
                    context.Parameters.PublicKeyValidity, Is.EqualTo(SessionContextFactory.EphemeralKeyValidity));
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
