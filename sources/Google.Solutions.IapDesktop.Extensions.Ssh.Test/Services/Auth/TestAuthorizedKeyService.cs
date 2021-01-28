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
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth;
using Google.Solutions.Ssh;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Test.Services.Auth
{
    [TestFixture]
    public class TestAuthorizedKeyService : ApplicationFixtureBase
    {
        private readonly static InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static Mock<IAuthorizationAdapter> CreateAuthorizationAdapterMock()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var adapter = new Mock<IAuthorizationAdapter>();
            adapter
                .SetupGet(a => a.Authorization)
                .Returns(authorization.Object);

            return adapter;
        }

        private Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
            bool osLoginEnabledForProject,
            bool osLoginEnabledForInstance,
            bool osLogin2fa,
            bool legacySshKeyPresent,
            bool projectWideKeysBlocked)
        {
            var projectMetadata = new Metadata();
            if (osLoginEnabledForProject)
            {
                projectMetadata.Add("enable-oslogin", "true");
            }

            if (osLoginEnabledForProject && osLogin2fa)
            {
                projectMetadata.Add("enable-oslogin-2fa", "true");
            }

            if (projectWideKeysBlocked)
            {
                projectMetadata.Add("block-project-ssh-keys", "true");
            }


            var instanceMetadata = new Metadata();
            if (osLoginEnabledForInstance)
            {
                instanceMetadata.Add("enable-oslogin", "true");
            }

            if (osLoginEnabledForInstance && osLogin2fa)
            {
                instanceMetadata.Add("enable-oslogin-2fa", "true");
            }

            if (legacySshKeyPresent)
            {
                instanceMetadata.Add("sshKeys", "somedata");
            }

            var adapter = new Mock<IComputeEngineAdapter>();
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

        private static Mock<IOsLoginService> CreateOsLoginServiceMock()
        {
            var osLoginService = new Mock<IOsLoginService>();
            osLoginService
                .Setup(s => s.AuthorizeKeyAsync(
                        It.IsAny<string>(),
                        It.Is((OsLoginSystemType os) => os == OsLoginSystemType.Linux),
                        It.IsAny<ISshKey>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthorizedKey.ForOsLoginAccount(
                    new Mock<ISshKey>().Object,
                    new PosixAccount()
                    {
                        Username = "bob"
                    }));
            return osLoginService;
        }

        //---------------------------------------------------------------------
        // Os Login.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenOsLoginEnabledForProject_ThenAuthorizeKeyAsyncUsesOsLogin()
        {
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: false,
                    osLogin2fa: false,
                    legacySshKeyPresent: true,
                    projectWideKeysBlocked: false).Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service.AuthorizeKeyAsync(
                SampleLocator,
                new Mock<ISshKey>().Object,
                TimeSpan.FromMinutes(1),
                null,
                AuthorizeKeyMethods.All,
                CancellationToken.None);

            Assert.IsNotNull(authorizedKey);
            Assert.AreEqual(AuthorizeKeyMethods.Oslogin, authorizedKey.AuthorizationMethod);
            Assert.AreEqual("bob", authorizedKey.Username);
        }

        [Test]
        public async Task WhenOsLoginEnabledForInstance_ThenAuthorizeKeyAsyncUsesOsLogin()
        {
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: false,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    legacySshKeyPresent: true,
                    projectWideKeysBlocked: false).Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service.AuthorizeKeyAsync(
                SampleLocator,
                new Mock<ISshKey>().Object,
                TimeSpan.FromMinutes(1),
                null,
                AuthorizeKeyMethods.All,
                CancellationToken.None);

            Assert.IsNotNull(authorizedKey);
            Assert.AreEqual(AuthorizeKeyMethods.Oslogin, authorizedKey.AuthorizationMethod);
            Assert.AreEqual("bob", authorizedKey.Username);
        }

        [Test]
        public void WhenOsLoginEnabledForProjectButOsLoginNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: false,
                    osLogin2fa: false,
                    legacySshKeyPresent: false,
                    projectWideKeysBlocked: false).Object,
                CreateOsLoginServiceMock().Object);

            AssertEx.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenOsLoginEnabledForInstanceButOsLoginNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: false,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    legacySshKeyPresent: false,
                    projectWideKeysBlocked: false).Object,
                CreateOsLoginServiceMock().Object);

            AssertEx.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenOsLogin2faEnabled_ThenAuthorizeKeyThrowsNotImplementedException()
        {
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: true,
                    legacySshKeyPresent: false,
                    projectWideKeysBlocked: false).Object,
                CreateOsLoginServiceMock().Object);

            AssertEx.ThrowsAggregateException<NotImplementedException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.All,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLegacySshKeyPresent_ThenAuthorizeKeyAsyncThrowsUnsupportedLegacySshKeyEncounteredException()
        {
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: false,
                    osLoginEnabledForInstance: false,
                    osLogin2fa: false,
                    legacySshKeyPresent: true,
                    projectWideKeysBlocked: false).Object,
                CreateOsLoginServiceMock().Object);

            AssertEx.ThrowsAggregateException<UnsupportedLegacySshKeyEncounteredException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.All,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectWideSshKeysBlocked_ThenAuthorizeKeyAsyncPushesKeyToInstanceMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                osLoginEnabledForProject: false,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                legacySshKeyPresent: false,
                projectWideKeysBlocked: true);
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                computeEngineAdapter.Object,
                CreateOsLoginServiceMock().Object);

            using (var key = RsaSshKey.NewEphemeralKey())
            {
                var authorizedKey = await service.AuthorizeKeyAsync(
                    SampleLocator,
                    key,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.All,
                    CancellationToken.None);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(AuthorizeKeyMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.Is((InstanceLocator loc) => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public void WhenProjectWideSshKeysBlockedButInstanceMetadataNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                osLoginEnabledForProject: false,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                legacySshKeyPresent: false,
                projectWideKeysBlocked: true);
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                computeEngineAdapter.Object,
                CreateOsLoginServiceMock().Object);

            AssertEx.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.Oslogin | AuthorizeKeyMethods.ProjectMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectMetadataNotAllowed_ThenAuthorizeKeyAsyncPushesKeyToInstanceMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                osLoginEnabledForProject: false,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                legacySshKeyPresent: false,
                projectWideKeysBlocked: false);
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                computeEngineAdapter.Object,
                CreateOsLoginServiceMock().Object);

            using (var key = RsaSshKey.NewEphemeralKey())
            {
                var authorizedKey = await service.AuthorizeKeyAsync(
                    SampleLocator,
                    key,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.InstanceMetadata,
                    CancellationToken.None);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(AuthorizeKeyMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.Is((InstanceLocator loc) => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task WhenProjectAndInstanceMetadataAllowed_ThenAuthorizeKeyAsyncPushesKeyToProjectMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                osLoginEnabledForProject: false,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                legacySshKeyPresent: false,
                projectWideKeysBlocked: false);
            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                computeEngineAdapter.Object,
                CreateOsLoginServiceMock().Object);

            using (var key = RsaSshKey.NewEphemeralKey())
            {
                var authorizedKey = await service.AuthorizeKeyAsync(
                    SampleLocator,
                    key,
                    TimeSpan.FromMinutes(1),
                    null,
                    AuthorizeKeyMethods.All,
                    CancellationToken.None);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(AuthorizeKeyMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public void WhenMetadataUpdatesFails_ThenAuthorizeKeyAsyncThrowsSshKeyPushFailedException(
            [Values(
                HttpStatusCode.Forbidden, 
            HttpStatusCode.BadRequest)] HttpStatusCode httpStatus)
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                osLoginEnabledForProject: false,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                legacySshKeyPresent: false,
                projectWideKeysBlocked: false);
            computeEngineAdapter
                .Setup(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new GoogleApiException("GCE", "mock-error")
                {
                    HttpStatusCode = httpStatus
                });

            var service = new AuthorizedKeyService(
                CreateAuthorizationAdapterMock().Object,
                computeEngineAdapter.Object,
                CreateOsLoginServiceMock().Object);

            using (var key = RsaSshKey.NewEphemeralKey())
            {
                AssertEx.ThrowsAggregateException<SshKeyPushFailedException>(
                    () => service.AuthorizeKeyAsync(
                        SampleLocator,
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        AuthorizeKeyMethods.All,
                        CancellationToken.None).Wait());
            }
        }
    }
}
