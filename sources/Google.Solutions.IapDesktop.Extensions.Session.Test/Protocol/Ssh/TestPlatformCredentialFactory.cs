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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestPlatformCredentialFactory
    {
        private const string SampleEmailAddress = "bob@example.com";
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static Mock<IAuthorization> CreateAuthorizationMock(string username)
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(a => a.Username)
                .Returns(username);

            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Session)
                .Returns(session.Object);

            return authorization;
        }

        private Mock<IComputeEngineClient> CreateComputeEngineClientMock(
            bool? osLoginEnabledForProject,
            bool? osLoginEnabledForInstance,
            bool osLogin2fa,
            bool osLoginSk)
        {
            var projectMetadata = new Metadata();
            if (osLoginEnabledForProject.HasValue)
            {
                projectMetadata.Add("enable-oslogin",
                    osLoginEnabledForProject.Value.ToString());
            }

            if (osLoginEnabledForProject.HasValue && osLogin2fa)
            {
                projectMetadata.Add("enable-oslogin-2fa", "true");
            }

            var instanceMetadata = new Metadata();
            if (osLoginEnabledForInstance.HasValue)
            {
                instanceMetadata.Add("enable-oslogin",
                    osLoginEnabledForInstance.Value.ToString());
            }

            if (osLoginEnabledForInstance.HasValue && osLogin2fa)
            {
                instanceMetadata.Add("enable-oslogin-2fa", "true");
            }

            if (osLoginEnabledForInstance.HasValue && osLoginSk)
            {
                instanceMetadata.Add("enable-oslogin-sk", "true");
            }

            var adapter = new Mock<IComputeEngineClient>();
            adapter
                .Setup(a => a.GetProjectAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project()
                {
                    CommonInstanceMetadata = projectMetadata
                });
            adapter
                .Setup(a => a.GetInstanceAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Instance()
                {
                    Metadata = instanceMetadata
                });
            return adapter;
        }

        private static Mock<IOsLoginProfile> CreateOsLoginServiceMock()
        {
            var osLoginService = new Mock<IOsLoginProfile>();
            osLoginService
                .Setup(s => s.AuthorizeKeyAsync(
                        It.IsAny<ZoneLocator>(),
                        It.IsAny<ulong>(),
                        It.Is((OsLoginSystemType os) => os == OsLoginSystemType.Linux),
                        It.IsAny<ServiceAccountEmail>(),
                        It.IsAny<IAsymmetricKeySigner>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlatformCredential(
                    new Mock<IAsymmetricKeySigner>().Object,
                    KeyAuthorizationMethods.Oslogin,
                    "bob"));
            return osLoginService;
        }

        //---------------------------------------------------------------------
        // CreateUsernameForMetadata.
        //---------------------------------------------------------------------

        [Test]
        public void CreateUsernameForMetadata_WhenPreferredUsernameIsInvalid_ThenCreateUsernameForMetadataThrowsException(
            [Values("", " ", "!user")] string username)
        {
            var authorizer = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.Throws<ArgumentException>(
                () => authorizer.CreateUsernameForMetadata(username));
        }


        [Test]
        public void CreateUsernameForMetadata_WhenPreferredUsernameIsValid_ThenCreateUsernameForMetadataReturnsPreferredUsername()
        {
            var authorizer = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.AreEqual(
                "user",
                authorizer.CreateUsernameForMetadata("user"));
        }

        [Test]
        public void CreateUsernameForMetadata_WhenPreferredUsernameNullButSessionUsernameValid_ThenCreateUsernameForMetadataGeneratesUsername()
        {
            var authorizer = new PlatformCredentialFactory(
                CreateAuthorizationMock("j@ex.ample").Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.AreEqual(
                "j",
                authorizer.CreateUsernameForMetadata(null));
        }

        [Test]
        public void CreateUsernameForMetadata_WhenPreferredUsernameNullAndSessionUsernameTooLong_ThenCreateUsernameForMetadataStripsUsername()
        {
            var authorizer = new PlatformCredentialFactory(
                CreateAuthorizationMock("ABCDEFGHIJKLMNOPQRSTUVWXYZabcxyz0@ex.ample").Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.AreEqual(
                "abcdefghijklmnopqrstuvwxyzabcxyz",
                authorizer.CreateUsernameForMetadata(null));
        }

        //---------------------------------------------------------------------
        // CreateCredential - Os Login.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateCredential_WhenOsLoginEnabledForProject_ThenUsesOsLogin()
        {
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: null,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .CreateCredentialAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorizedKey);
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, authorizedKey.AuthorizationMethod);
            Assert.AreEqual("bob", authorizedKey.Username);
        }

        [Test]
        public async Task CreateCredential_WhenOsLoginEnabledForInstance_ThenUsesOsLogin()
        {
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .CreateCredentialAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorizedKey);
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, authorizedKey.AuthorizationMethod);
            Assert.AreEqual("bob", authorizedKey.Username);
        }

        [Test]
        public async Task CreateCredential_WhenOsLoginDisabledForProjectButEnabledForInstance_ThenUsesOsLogin()
        {
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: false,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .CreateCredentialAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorizedKey);
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, authorizedKey.AuthorizationMethod);
            Assert.AreEqual("bob", authorizedKey.Username);
        }

        [Test]
        public async Task CreateCredential_WhenOsLoginEnabledForProjectButDisabledForInstance_ThenPushesKeyToMetadata()
        {
            var computeClient = CreateComputeEngineClientMock(
                osLoginEnabledForProject: true,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                osLoginSk: false);
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                computeClient.Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            using (var signer = AsymmetricKeySigner.CreateEphemeral(SshKeyType.Rsa3072))
            {
                var authorizedKey = await service
                    .CreateCredentialAsync(
                        SampleLocator,
                        signer,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeClient.Verify(a => a.UpdateMetadataAsync(
                    It.Is<InstanceLocator>(loc => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public void CreateCredential_WhenOsLoginEnabledForProjectButOsLoginNotAllowed_ThenThrowsInvalidOperationException()
        {
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: null,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => service.CreateCredentialAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void CreateCredential_WhenOsLoginEnabledForInstanceButOsLoginNotAllowed_ThenThrowsInvalidOperationException()
        {
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => service.CreateCredentialAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void CreateCredential_WhenOsLoginWithSecurityKeyEnabledForInstance_ThenThrowsNotImplementedException()
        {
            var service = new PlatformCredentialFactory(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: true).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<OsLoginSkNotSupportedException>(
                () => service.CreateCredentialAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None).Wait());
        }
    }
}
