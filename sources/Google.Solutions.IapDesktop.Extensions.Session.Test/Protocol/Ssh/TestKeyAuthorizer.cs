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

using Google.Apis.CloudOSLogin.v1.Data;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh;
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
    public class TestKeyAuthorizer
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
                    It.IsAny<string>(),
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
                        It.Is((OsLoginSystemType os) => os == OsLoginSystemType.Linux),
                        It.IsAny<IAsymmetricKeySigner>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SshAuthorizedKeyCredential(
                    new Mock<IAsymmetricKeySigner>().Object,
                    KeyAuthorizationMethods.Oslogin,
                    "bob"));
            return osLoginService;
        }

        //---------------------------------------------------------------------
        // CreateUsernameForMetadata.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPreferredUsernameIsInvalid_ThenCreateUsernameForMetadataThrowsException(
            [Values("", " ", "!user")] string username)
        {
            var authorizer = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.Throws<ArgumentException>(
                () => authorizer.CreateUsernameForMetadata(username));
        }


        [Test]
        public void WhenPreferredUsernameIsValid_ThenCreateUsernameForMetadataReturnsPreferredUsername()
        {
            var authorizer = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.AreEqual(
                "user",
                authorizer.CreateUsernameForMetadata("user"));
        }

        [Test]
        public void WhenPreferredUsernameNullButSessionUsernameValid_ThenCreateUsernameForMetadataGeneratesUsername()
        {
            var authorizer = new KeyAuthorizer(
                CreateAuthorizationMock("j@ex.ample").Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.AreEqual(
                "j",
                authorizer.CreateUsernameForMetadata(null));
        }

        [Test]
        public void WhenPreferredUsernameNullAndSessionUsernameTooLong_ThenCreateUsernameForMetadataStripsUsername()
        {
            var authorizer = new KeyAuthorizer(
                CreateAuthorizationMock("ABCDEFGHIJKLMNOPQRSTUVWXYZabcxyz0@ex.ample").Object,
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IOsLoginProfile>().Object);

            Assert.AreEqual(
                "abcdefghijklmnopqrstuvwxyzabcxyz",
                authorizer.CreateUsernameForMetadata(null));
        }

        //---------------------------------------------------------------------
        // Os Login.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenOsLoginEnabledForProject_ThenAuthorizeKeyAsyncUsesOsLogin()
        {
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: null,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .AuthorizeKeyAsync(
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
        public async Task WhenOsLoginEnabledForInstance_ThenAuthorizeKeyAsyncUsesOsLogin()
        {
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .AuthorizeKeyAsync(
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
        public async Task WhenOsLoginDisabledForProjectButEnabledForInstance_ThenAuthorizeKeyAsyncUsesOsLogin()
        {
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: false,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .AuthorizeKeyAsync(
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
        public async Task WhenOsLoginEnabledForProjectButDisabledForInstance_ThenAuthorizeKeyAsyncPushesKeyToMetadata()
        {
            var computeClient = CreateComputeEngineClientMock(
                osLoginEnabledForProject: true,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                osLoginSk: false);
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                computeClient.Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            using (var signer = AsymmetricKeySigner.CreateEphemeral(SshKeyType.Rsa3072))
            {
                var authorizedKey = await service
                    .AuthorizeKeyAsync(
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
        public void WhenOsLoginEnabledForProjectButOsLoginNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: null,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenOsLoginEnabledForInstanceButOsLoginNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenOsLoginWithSecurityKeyEnabledForInstance_ThenAuthorizeKeyThrowsNotImplementedException()
        {
            var service = new KeyAuthorizer(
                CreateAuthorizationMock(SampleEmailAddress).Object,
                CreateComputeEngineClientMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: true).Object,
                new Mock<IResourceManagerClient>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<OsLoginSkNotSupportedException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<IAsymmetricKeySigner>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None).Wait());
        }
    }
}
