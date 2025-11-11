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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestProjectMetadata
    {
        private static readonly ProjectLocator SampleLocator
            = new ProjectLocator("project-1");

        private static Mock<IComputeEngineClient> CreateComputeEngineClientMock(
            ProjectLocator project,
            Metadata projectMetadata)
        {
            var adapter = new Mock<IComputeEngineClient>();
            adapter
                .Setup(a => a.GetProjectAsync(
                    project,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project()
                {
                    CommonInstanceMetadata = projectMetadata,
                    Name = project.Name
                });
            return adapter;
        }

        //---------------------------------------------------------------------
        // IsOsLoginEnabled.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsOsLoginEnabled_WhenValueIsTruthy(
            [Values("Y", "y\n", "True ", " 1 ")] string truthyValue)
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(
                        SampleLocator,
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
                        }).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(processor.IsOsLoginEnabled);
        }

        [Test]
        public async Task IsOsLoginEnabled_WhenValueIsNotTruthy(
            [Values("N", " no\n", "FALSE", " 0 ", null, "", "junk")] string truthyValue)
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(
                        SampleLocator,
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
                        }).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginEnabled);
        }

        //---------------------------------------------------------------------
        // IsOsLoginWithSecurityKeyEnabled.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsOsLoginWithSecurityKeyEnabled_WhenValueIsTruthy(
            [Values("Y", "y\n", "True ", " 1 ")] string truthyValue)
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(
                        SampleLocator,
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
                        }).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(processor.IsOsLoginWithSecurityKeyEnabled);
        }

        [Test]
        public async Task IsOsLoginWithSecurityKeyEnabled_WhenValueIsNotTruthy(
            [Values("N", " no\n", "FALSE", " 0 ", null, "", "junk")] string truthyValue)
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(
                        SampleLocator,
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
                        }).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginWithSecurityKeyEnabled);
        }
        [Test]
        public async Task IsOsLoginWithSecurityKeyEnabled_WhenValueIsMissing()
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(SampleLocator, new Metadata()).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(processor.IsOsLoginWithSecurityKeyEnabled);
        }

        //---------------------------------------------------------------------
        // ListAuthorizedKeys.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListAuthorizedKeys_WhenMetadataIsEmpty()
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(SampleLocator, new Metadata()).Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.All);
            Assert.IsNotNull(keys);
            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task ListAuthorizedKeys_WhenMetadataItemIsEmpty()
        {
            var processor = await ProjectMetadata.GetAsync(
                    CreateComputeEngineClientMock(
                        SampleLocator,
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
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.ProjectMetadata);
            Assert.IsNotNull(keys);
            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task ListAuthorizedKeys_WhenMetadataContainsKeys()
        {
            var metadata = new Metadata();
            metadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        "KEY-BOB",
                        "bob@example.com"))
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "alice",
                        "ssh-rsa",
                        "KEY-ALICE",
                        "alice@example.com"))
                    .ToString());

            var processor = await ProjectMetadata.GetAsync(
                CreateComputeEngineClientMock(SampleLocator, metadata).Object,
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

        [Test]
        public async Task ListAuthorizedKeys_WhenMetadataContainsButMethodDoesNotMatch()
        {
            var metadata = new Metadata();
            metadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "bob",
                        "ssh-rsa",
                        "KEY-BOB",
                        "bob@example.com"))
                    .Add(new UnmanagedMetadataAuthorizedPublicKey(
                        "alice",
                        "ssh-rsa",
                        "KEY-ALICE",
                        "alice@example.com"))
                    .ToString());

            var processor = await ProjectMetadata.GetAsync(
                CreateComputeEngineClientMock(SampleLocator, metadata).Object,
                SampleLocator,
                CancellationToken.None)
                .ConfigureAwait(false);

            var keys = processor.ListAuthorizedKeys(KeyAuthorizationMethods.InstanceMetadata);
            Assert.IsNotNull(keys);
            CollectionAssert.IsEmpty(keys);
        }

        //---------------------------------------------------------------------
        // RemoveAuthorizedKey.
        //---------------------------------------------------------------------

        [Test]
        public async Task RemoveAuthorizedKey_WhenKeyFound()
        {
            var bobsKey = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "KEY-BOB",
                "bob@example.com");
            var metadata = new Metadata();
            metadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(bobsKey)
                    .ToString());

            var computeClient = CreateComputeEngineClientMock(SampleLocator, metadata);
            var processor = await ProjectMetadata.GetAsync(
                    computeClient.Object,
                    SampleLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            await processor
                .RemoveAuthorizedKeyAsync(
                    bobsKey,
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeClient.Verify(a => a.UpdateCommonInstanceMetadataAsync(
                It.IsAny<ProjectLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
