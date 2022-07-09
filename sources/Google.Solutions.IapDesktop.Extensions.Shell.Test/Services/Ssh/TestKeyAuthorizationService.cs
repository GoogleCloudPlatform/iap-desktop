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
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Support.Nunit;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Ssh
{
    [TestFixture]
    public class TestKeyAuthorizationService : ShellFixtureBase
    {
        private const string SampleEmailAddress = "bob@example.com";
        private readonly static InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static Mock<IAuthorizationSource> CreateAuthorizationSourceMock()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns(SampleEmailAddress);

            var adapter = new Mock<IAuthorizationSource>();
            adapter
                .SetupGet(a => a.Authorization)
                .Returns(authorization.Object);

            return adapter;
        }

        private Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
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
                .Setup(s => s.AuthorizeKeyPairAsync(
                        It.IsAny<ProjectLocator>(),
                        It.Is((OsLoginSystemType os) => os == OsLoginSystemType.Linux),
                        It.IsAny<ISshKeyPair>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthorizedKeyPair.ForOsLoginAccount(
                    new Mock<ISshKeyPair>().Object,
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
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: null,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKeyPair>().Object,
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
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKeyPair>().Object,
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
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: false,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            var authorizedKey = await service
                .AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKeyPair>().Object,
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
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                osLoginEnabledForProject: true,
                osLoginEnabledForInstance: false,
                osLogin2fa: false,
                osLoginSk: false);
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                computeEngineAdapter.Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var authorizedKey = await service
                    .AuthorizeKeyAsync(
                        SampleLocator,
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.Is<InstanceLocator>(loc => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public void WhenOsLoginEnabledForProjectButOsLoginNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: true,
                    osLoginEnabledForInstance: null,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKeyPair>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenOsLoginEnabledForInstanceButOsLoginNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: false).Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKeyPair>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenOsLoginWithSecurityKeyEnabledForInstance_ThenAuthorizeKeyThrowsNotImplementedException()
        {
            var service = new KeyAuthorizationService(
                CreateAuthorizationSourceMock().Object,
                CreateComputeEngineAdapterMock(
                    osLoginEnabledForProject: null,
                    osLoginEnabledForInstance: true,
                    osLogin2fa: false,
                    osLoginSk: true).Object,
                new Mock<IResourceManagerAdapter>().Object,
                CreateOsLoginServiceMock().Object);

            ExceptionAssert.ThrowsAggregateException<NotImplementedException>(
                () => service.AuthorizeKeyAsync(
                    SampleLocator,
                    new Mock<ISshKeyPair>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None).Wait());
        }
    }
}
