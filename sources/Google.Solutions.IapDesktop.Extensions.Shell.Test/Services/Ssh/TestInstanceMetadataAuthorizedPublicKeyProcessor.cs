﻿//
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
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Ssh
{
    [TestFixture]
    public class TestInstanceMetadataAuthorizedPublicKeyProcessor : ShellFixtureBase
    {
        private const string SampleEmailAddress = "bob@example.com";
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static Mock<IAuthorization> CreateAuthorizationMock()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns(SampleEmailAddress);

            return authorization;
        }

        private static Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
            Metadata projectMetadata,
            Metadata instanceMetadata)
        {
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

        private static Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
           bool legacySshKeyPresent,
           bool projectWideKeysBlockedForProject,
           bool projectWideKeysBlockedForInstance,
           MetadataAuthorizedPublicKeySet existingProjectKeySet = null,
           MetadataAuthorizedPublicKeySet existingInstanceKeySet = null)
        {
            var projectMetadata = new Metadata();

            if (projectWideKeysBlockedForProject)
            {
                projectMetadata.Add("block-project-ssh-keys", "true");
            }

            if (existingProjectKeySet != null)
            {
                projectMetadata.Add(
                    MetadataAuthorizedPublicKeySet.MetadataKey,
                    existingProjectKeySet.ToString());
            }

            var instanceMetadata = new Metadata();

            if (legacySshKeyPresent)
            {
                instanceMetadata.Add("sshKeys", "somedata");
            }

            if (projectWideKeysBlockedForInstance)
            {
                instanceMetadata.Add("block-project-ssh-keys", "true");
            }

            if (existingInstanceKeySet != null)
            {
                instanceMetadata.Add(
                    MetadataAuthorizedPublicKeySet.MetadataKey,
                    existingInstanceKeySet.ToString());
            }

            return CreateComputeEngineAdapterMock(projectMetadata, instanceMetadata);
        }

        private static Mock<IResourceManagerAdapter> CreateResourceManagerAdapterMock(
            bool allowSetCommonInstanceMetadata)
        {
            var adapter = new Mock<IResourceManagerAdapter>();
            adapter
                .Setup(a => a.IsGrantedPermissionAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(allowSetCommonInstanceMetadata);

            return adapter;
        }

        //---------------------------------------------------------------------
        // IsOsLoginEnabled.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenValueIsTruthy_ThenIsOsLoginEnabledReturnsTrue(
            [Values("Y", "y\n", "True ", " 1 ")] string truthyValue)
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin",
                                Value = truthyValue
                            }
                        }
                    },
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(processor.IsOsLoginEnabled);
        }

        [Test]
        public async Task WhenValueIsNotTruthy_ThenIsOsLoginEnabledReturnsFalse(
            [Values("N", " no\n", "FALSE", " 0 ", null, "", "junk")] string truthyValue)
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin",
                                Value = truthyValue
                            }
                        }
                    },
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginEnabled);
        }

        [Test]
        public async Task WhenOsLoginEnabledForProjectButDisabledForInstance_ThenIsOsLoginEnabledReturnsFalse()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin",
                                Value = "true"
                            }
                        }
                    },
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin",
                                Value = "false"
                            }
                        }
                    }).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginEnabled);
        }

        //---------------------------------------------------------------------
        // IsOsLoginWithSecurityKeyEnabled.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenValueIsTruthy_ThenIsOsLoginWithSecurityKeyEnabledReturnsTrue(
            [Values("Y", "y\n", "True ", " 1 ")] string truthyValue)
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin-SK",
                                Value = truthyValue
                            }
                        }
                    },
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(processor.IsOsLoginWithSecurityKeyEnabled);
        }

        [Test]
        public async Task WhenValueIsNotTruthy_ThenIsOsLoginWithSecurityKeyEnabledReturnsFalse(
            [Values("N", " no\n", "FALSE", " 0 ", null, "", "junk")] string truthyValue)
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin-sk",
                                Value = truthyValue
                            }
                        }
                    },
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginWithSecurityKeyEnabled);
        }

        [Test]
        public async Task WhenValueIsMissing_ThenIsOsLoginWithSecurityKeyEnabledReturnsFalse()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata(),
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginWithSecurityKeyEnabled);
        }

        [Test]
        public async Task WhenOsLoginWithSecurityKeyEnabledForProjectButDisabledForInstance_ThenIsOsLoginWithSecurityKeyEnabledReturnsFalse()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin-sk",
                                Value = "true"
                            }
                        }
                    },
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Enable-OsLogin-sK",
                                Value = "false"
                            }
                        }
                    }).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginWithSecurityKeyEnabled);
        }

        //---------------------------------------------------------------------
        // AreProjectSshKeysBlocked.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenValueIsTruthy_ThenAreProjectSshKeysBlockedReturnsTrue(
            [Values("Y", "y\n", "True ", " 1 ")] string truthyValue)
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "Block-Project-SSH-keys",
                                Value = truthyValue
                            }
                        }
                    },
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(processor.AreProjectSshKeysBlocked);
        }

        [Test]
        public async Task WhenValueIsNotTruthy_ThenAreProjectSshKeysBlockedReturnsFalse(
            [Values("N", " no\n", "FALSE", " 0 ", null, "", "junk")] string truthyValue)
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "BLOCK-project-ssh-KEYS",
                                Value = truthyValue
                            }
                        }
                    },
                    new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.AreProjectSshKeysBlocked);
        }

        [Test]
        public async Task WhenProjectSshKeysBlockedForProjectButNotForInstance_ThenAreProjectSshKeysBlockedReturnsFalse()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "BLOCK-project-ssh-KEYS",
                                Value = "true"
                            }
                        }
                    },
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "BLOCK-project-ssh-KEYS",
                                Value = "false"
                            }
                        }
                    }).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginEnabled);
        }

        //---------------------------------------------------------------------
        // AuthorizeKeyAsync, using existing keys.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLegacySshKeyPresent_ThenAuthorizeKeyAsyncThrowsUnsupportedLegacySshKeyEncounteredException()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    CreateComputeEngineAdapterMock(
                        legacySshKeyPresent: true,
                        projectWideKeysBlockedForProject: false,
                        projectWideKeysBlockedForInstance: false).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            ExceptionAssert.ThrowsAggregateException<UnsupportedLegacySshKeyEncounteredException>(
                () => processor.AuthorizeKeyPairAsync(
                    new Mock<ISshKeyPair>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.All,
                    CreateAuthorizationMock().Object,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenExistingUnmanagedKeyFound_ThenKeyIsNotPushedAgain()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var existingProjectKeySet = MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        key.PublicKeyString,
                        SampleEmailAddress));

                var computeEngineAdapter = CreateComputeEngineAdapterMock(
                    legacySshKeyPresent: false,
                    projectWideKeysBlockedForProject: false,
                    projectWideKeysBlockedForInstance: false,
                    existingProjectKeySet: existingProjectKeySet,
                    existingInstanceKeySet: null);

                var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        computeEngineAdapter.Object,
                        CreateResourceManagerAdapterMock(true).Object,
                        SampleLocator,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        "bob",
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Never);

                computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public async Task WhenExistingValidManagedKeyFound_ThenKeyIsNotPushedAgain()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var existingProjectKeySet = MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new ManagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        key.PublicKeyString,
                        new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(SampleEmailAddress, DateTime.UtcNow.AddMinutes(5))));

                var computeEngineAdapter = CreateComputeEngineAdapterMock(
                    legacySshKeyPresent: false,
                    projectWideKeysBlockedForProject: false,
                    projectWideKeysBlockedForInstance: false,
                    existingProjectKeySet: existingProjectKeySet,
                    existingInstanceKeySet: null);

                var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        computeEngineAdapter.Object,
                        CreateResourceManagerAdapterMock(true).Object,
                        SampleLocator,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        "bob",
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Never);

                computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public async Task WhenExistingManagedKeyOfDifferentTypeFound_ThenNewKeyIsPushed()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var existingProjectKeySet = MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new ManagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ecdsa-sha2-nistp384",
                        key.PublicKeyString,
                        new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                            SampleEmailAddress,
                            DateTime.UtcNow.AddMinutes(5))));

                var computeEngineAdapter = CreateComputeEngineAdapterMock(
                    legacySshKeyPresent: false,
                    projectWideKeysBlockedForProject: false,
                    projectWideKeysBlockedForInstance: false,
                    existingProjectKeySet: existingProjectKeySet,
                    existingInstanceKeySet: null);

                var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        computeEngineAdapter.Object,
                        CreateResourceManagerAdapterMock(true).Object,
                        SampleLocator,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        "bob",
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Never);

                computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task WhenExpiredManagedKeyFound_ThenNewKeyIsPushed()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var existingProjectKeySet = MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new ManagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        key.PublicKeyString,
                        new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                            SampleEmailAddress,
                            DateTime.UtcNow.AddMinutes(-5))));

                var computeEngineAdapter = CreateComputeEngineAdapterMock(
                    legacySshKeyPresent: false,
                    projectWideKeysBlockedForProject: false,
                    projectWideKeysBlockedForInstance: false,
                    existingProjectKeySet: existingProjectKeySet,
                    existingInstanceKeySet: null);

                var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        computeEngineAdapter.Object,
                        CreateResourceManagerAdapterMock(true).Object,
                        SampleLocator,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        "bob",
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Never);

                computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        //---------------------------------------------------------------------
        // AuthorizeKeyAsync, pushing new keys.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectWideSshKeysBlockedInProject_ThenAuthorizeKeyAsyncPushesKeyToInstanceMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: true,
                projectWideKeysBlockedForInstance: false);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.Is((InstanceLocator loc) => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task WhenProjectWideSshKeysBlockedInInstance_ThenAuthorizeKeyAsyncPushesKeyToInstanceMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: false,
                projectWideKeysBlockedForInstance: true);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.Is((InstanceLocator loc) => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task WhenProjectMetadataNotWritable_ThenAuthorizeKeyAsyncPushesKeyToInstanceMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: false,
                projectWideKeysBlockedForInstance: false);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(false).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                    It.Is((InstanceLocator loc) => loc == SampleLocator),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task WhenProjectWideSshKeysBlockedButInstanceMetadataNotAllowed_ThenAuthorizeKeyThrowsInvalidOperationException()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: true,
                projectWideKeysBlockedForInstance: false);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => processor.AuthorizeKeyPairAsync(
                    new Mock<ISshKeyPair>().Object,
                    TimeSpan.FromMinutes(1),
                    null,
                    KeyAuthorizationMethods.Oslogin | KeyAuthorizationMethods.ProjectMetadata,
                    CreateAuthorizationMock().Object,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectMetadataNotAllowed_ThenAuthorizeKeyAsyncPushesKeyToInstanceMetadata()
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: false,
                projectWideKeysBlockedForInstance: false);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.InstanceMetadata,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
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
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: false,
                projectWideKeysBlockedForInstance: false);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                var authorizedKey = await processor
                    .AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(authorizedKey);
                Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
                Assert.AreEqual("bob", authorizedKey.Username);

                computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task WhenMetadataUpdateFails_ThenAuthorizeKeyAsyncThrowsSshKeyPushFailedException(
            [Values(
                HttpStatusCode.Forbidden,
                HttpStatusCode.BadRequest)] HttpStatusCode httpStatus)
        {
            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                legacySshKeyPresent: false,
                projectWideKeysBlockedForProject: false,
                projectWideKeysBlockedForInstance: false);
            computeEngineAdapter
                .Setup(a => a.UpdateCommonInstanceMetadataAsync(
                    It.IsAny<string>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new GoogleApiException("GCE", "mock-error")
                {
                    HttpStatusCode = httpStatus
                });


            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                ExceptionAssert.ThrowsAggregateException<SshKeyPushFailedException>(
                    () => processor.AuthorizeKeyPairAsync(
                        key,
                        TimeSpan.FromMinutes(1),
                        null,
                        KeyAuthorizationMethods.All,
                        CreateAuthorizationMock().Object,
                        CancellationToken.None).Wait());
            }
        }

        //---------------------------------------------------------------------
        // ListAuthorizedKeys.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenMetadataIsEmpty_ThenListAuthorizedKeysReturnsEmptyList()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    CreateComputeEngineAdapterMock(
                        new Metadata(),
                        new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.All);
            Assert.IsNotNull(keys);
            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task WhenMetadataItemIsEmpty_ThenListAuthorizedKeysReturnsEmptyList()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                CreateComputeEngineAdapterMock(
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "ssh-keys",
                            }
                        }
                    },
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "ssh-keys",
                            }
                        }
                    }).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.All);
            Assert.IsNotNull(keys);
            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task WhenMethodIsNeitherProjectNorInstance_ThenListAuthorizedKeysReturnsEmptyList()
        {
            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    CreateComputeEngineAdapterMock(
                        new Metadata(),
                        new Metadata()).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.Oslogin);
            Assert.IsNotNull(keys);
            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task WhenMethodIsInstance_ThenListAuthorizedKeysReturnsInstanceKeys()
        {
            var projectMetadata = new Metadata();
            projectMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "alice",
                        "ssh-rsa",
                        "KEY-ALICE",
                        "alice@example.com"))
                    .ToString());

            var instanceMetadata = new Metadata();
            instanceMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        "KEY-BOB",
                        "bob@example.com"))
                    .ToString());

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    CreateComputeEngineAdapterMock(
                        projectMetadata,
                        instanceMetadata).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.InstanceMetadata);
            Assert.IsNotNull(keys);
            Assert.AreEqual(1, keys.Count());
            Assert.AreEqual("bob", keys.First().PosixUsername);
        }

        [Test]
        public async Task WhenMethodIsProject_ThenListAuthorizedKeysReturnsInstanceKeys()
        {
            var projectMetadata = new Metadata();
            projectMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "alice",
                        "ssh-rsa",
                        "KEY-ALICE",
                        "alice@example.com"))
                    .ToString());

            var instanceMetadata = new Metadata();
            instanceMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        "KEY-BOB",
                        "bob@example.com"))
                    .ToString());

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    CreateComputeEngineAdapterMock(
                        projectMetadata,
                        instanceMetadata).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.ProjectMetadata);
            Assert.IsNotNull(keys);
            Assert.AreEqual(1, keys.Count());
            Assert.AreEqual("alice", keys.First().PosixUsername);
        }

        [Test]
        public async Task WhenMethodIsAll_ThenListAuthorizedKeysReturnsAllKeys()
        {
            var projectMetadata = new Metadata();
            projectMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "alice",
                        "ssh-rsa",
                        "KEY-ALICE",
                        "alice@example.com"))
                    .ToString());

            var instanceMetadata = new Metadata();
            instanceMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        "KEY-BOB",
                        "bob@example.com"))
                    .ToString());

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    CreateComputeEngineAdapterMock(
                        projectMetadata,
                        instanceMetadata).Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.All);
            Assert.IsNotNull(keys);
            Assert.AreEqual(2, keys.Count());
            CollectionAssert.AreEquivalent(
                new[] { "alice", "bob" },
                keys.Select(k => k.PosixUsername));
        }

        //---------------------------------------------------------------------
        // RemoveAuthorizedKeyAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenMethodIsAll_ThenRemoveAuthorizedKeyUpdatesProjectAndInstanceMetadata()
        {
            var bobsKey = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "KEY-BOB",
                "bob@example.com");

            var projectMetadata = new Metadata();
            projectMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(bobsKey)
                    .ToString());

            var instanceMetadata = new Metadata();
            instanceMetadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(bobsKey)
                    .ToString());

            var computeEngineAdapter = CreateComputeEngineAdapterMock(
                projectMetadata,
                instanceMetadata);

            var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                    computeEngineAdapter.Object,
                    CreateResourceManagerAdapterMock(true).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            await processor.RemoveAuthorizedKeyAsync(
                    bobsKey,
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeEngineAdapter.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            computeEngineAdapter.Verify(a => a.UpdateMetadataAsync(
                It.IsAny<InstanceLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
