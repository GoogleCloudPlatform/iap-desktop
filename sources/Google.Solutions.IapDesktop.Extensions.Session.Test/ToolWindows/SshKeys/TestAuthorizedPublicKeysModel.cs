//
// Copyright 2022 Google LLC
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
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.SshKeys;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.SshKeys
{
    [TestFixture]
    public class TestAuthorizedPublicKeysModel
    {
        private static Mock<IProjectModelProjectNode> CreateProjectNodeMock(
            string projectId,
            string displayName)
        {
            var nodeMock = new Mock<IProjectModelProjectNode>();
            nodeMock.SetupGet(n => n.DisplayName).Returns(displayName);
            nodeMock.SetupGet(n => n.Project).Returns(
                new ProjectLocator(projectId));

            return nodeMock;
        }

        private static Mock<IProjectModelInstanceNode> CreateInstanceNodeMock(
            string instanceName)
        {
            var nodeMock = new Mock<IProjectModelInstanceNode>();
            nodeMock.SetupGet(n => n.DisplayName).Returns(instanceName);
            nodeMock.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", instanceName));
            nodeMock.SetupGet(n => n.OperatingSystem)
                .Returns(OperatingSystems.Linux);

            return nodeMock;
        }

        private Mock<IComputeEngineClient> CreateComputeEngineAdapterMock(
            IDictionary<string, string>? projectMetadata,
            IDictionary<string, string>? instanceMetadata)
        {
            var adapter = new Mock<IComputeEngineClient>();
            adapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project()
                {
                    CommonInstanceMetadata = new Metadata()
                    {
                        Items = projectMetadata?
                            .Select(kvp => new Metadata.ItemsData()
                            {
                                Key = kvp.Key,
                                Value = kvp.Value
                            })
                            .ToList()
                    }
                });

            adapter.Setup(a => a.GetInstanceAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Instance()
                {
                    Metadata = new Metadata()
                    {
                        Items = instanceMetadata?
                            .Select(kvp => new Metadata.ItemsData()
                            {
                                Key = kvp.Key,
                                Value = kvp.Value
                            })
                            .ToList()
                    },
                });

            return adapter;
        }

        //---------------------------------------------------------------------
        // IsNodeSupported.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNodeIsCloudNode_ThenIsNodeSupportedReturnsFalse()
        {
            Assert.IsFalse(AuthorizedPublicKeysModel.IsNodeSupported(
                new Mock<IProjectModelCloudNode>().Object));
        }

        [Test]
        public void WhenNodeIsZoneNode_ThenIsNodeSupportedReturnsFalse()
        {
            Assert.IsFalse(AuthorizedPublicKeysModel.IsNodeSupported(
                new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public void WhenNodeIsProjectNode_ThenIsNodeSupportedReturnsFalse()
        {
            Assert.IsTrue(AuthorizedPublicKeysModel.IsNodeSupported(
                new Mock<IProjectModelProjectNode>().Object));
        }

        [Test]
        public void WhenNodeIsWindowsInstance_ThenIsNodeSupportedReturnsFalse()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.OperatingSystem)
                .Returns(OperatingSystems.Windows);

            Assert.IsFalse(AuthorizedPublicKeysModel.IsNodeSupported(node.Object));
        }

        [Test]
        public void WhenNodeIsLinuxInstance_ThenIsNodeSupportedReturnsFalse()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.OperatingSystem)
                .Returns(OperatingSystems.Linux);

            Assert.IsTrue(AuthorizedPublicKeysModel.IsNodeSupported(node.Object));
        }

        //---------------------------------------------------------------------
        // LoadAsync - project node.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenScopeIsCloud_ThenLoadReturnsNull()
        {
            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    new Mock<IComputeEngineClient>().Object,
                    new Mock<IResourceManagerClient>().Object,
                    new Mock<IOsLoginProfile>().Object,
                    new Mock<IProjectModelCloudNode>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(model);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginEnabled_ThenModelIncludesOsLoginKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { MetadataAuthorizedPublicKeyProcessor.EnableOsLoginFlag, "true" },
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateProjectNodeMock("project-1", "Project 1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model!.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginDisabled_ThenModelIncludesProjectKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateProjectNodeMock("project-1", "Project 1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model!.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, model.Items.ToList()[0].AuthorizationMethod);
            Assert.AreEqual("alice@gmail.com", model.Items.ToList()[0].Key.Email);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginDisabledAndProjectKeysBlocked_ThenModelIncludesNoKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { MetadataAuthorizedPublicKeyProcessor.BlockProjectSshKeysFlag, "true" },
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateProjectNodeMock("project-1", "Project 1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model!.DisplayName);

            CollectionAssert.IsEmpty(model.Items);
        }

        //---------------------------------------------------------------------
        // LoadAsync - instance node.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenScopeIsZone_ThenLoadReturnsNull()
        {
            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    new Mock<IComputeEngineClient>().Object,
                    new Mock<IResourceManagerClient>().Object,
                    new Mock<IOsLoginProfile>().Object,
                    new Mock<IProjectModelZoneNode>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(model);
        }


        [Test]
        public async Task WhenScopeIsInstanceAndOsLoginEnabled_ThenModelIncludesOsLoginKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { MetadataAuthorizedPublicKeyProcessor.EnableOsLoginFlag, "true" },
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                new Dictionary<string, string>
                {
                    { "ssh-keys", "bob:ssh-rsa BOBS-KEY bob@gmail.com" }
                });

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateInstanceNodeMock("instance-1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model!.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndOsLoginDisabled_ThenModelIncludesProjectAndInstanceKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                new Dictionary<string, string>
                {
                    { "ssh-keys", "bob:ssh-rsa BOBS-KEY bob@gmail.com" }
                });

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateInstanceNodeMock("instance-1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model!.DisplayName);

            Assert.AreEqual(2, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, model.Items.ToList()[0].AuthorizationMethod);
            Assert.AreEqual("alice@gmail.com", model.Items.ToList()[0].Key.Email);
            Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, model.Items.ToList()[1].AuthorizationMethod);
            Assert.AreEqual("bob@gmail.com", model.Items.ToList()[1].Key.Email);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndOsLoginDisabledAndProjectKeysBlocked_ThenModelIncludesInstanceKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                new Dictionary<string, string>
                {
                    { MetadataAuthorizedPublicKeyProcessor.BlockProjectSshKeysFlag, "true" },
                    { "ssh-keys", "bob:ssh-rsa BOBS-KEY bob@gmail.com" }
                });

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateInstanceNodeMock("instance-1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model!.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, model.Items.ToList()[0].AuthorizationMethod);
            Assert.AreEqual("bob@gmail.com", model.Items.ToList()[0].Key.Email);
        }

        //---------------------------------------------------------------------
        // DeleteFromOsLoginAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAuthorizationMethodIsInstanceMetadata_ThenDeleteFromOsLoginDoesNothing()
        {
            var osLoginServiceMock = new Mock<IOsLoginProfile>();

            await AuthorizedPublicKeysModel.DeleteFromOsLoginAsync(
                    osLoginServiceMock.Object,
                    new AuthorizedPublicKeysModel.Item(
                        new Mock<IAuthorizedPublicKey>().Object,
                        KeyAuthorizationMethods.InstanceMetadata),
                    CancellationToken.None)
                .ConfigureAwait(false);

            osLoginServiceMock.Verify(s => s.DeleteAuthorizedKeyAsync(
                It.IsAny<IAuthorizedPublicKey>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenAuthorizationMethodIsOslogin_ThenDeleteFromOsLoginDeletesKey()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();

            await AuthorizedPublicKeysModel.DeleteFromOsLoginAsync(
                    osLoginServiceMock.Object,
                    new AuthorizedPublicKeysModel.Item(
                        new Mock<IAuthorizedPublicKey>().Object,
                        KeyAuthorizationMethods.Oslogin),
                    CancellationToken.None)
                .ConfigureAwait(false);

            osLoginServiceMock.Verify(s => s.DeleteAuthorizedKeyAsync(
                It.IsAny<IAuthorizedPublicKey>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // DeleteFromMetadataAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAuthorizationMethodIsOslogin_ThenDeleteFromMetadataDoesNothing()
        {
            var computeEngineMock = new Mock<IComputeEngineClient>();

            await AuthorizedPublicKeysModel.DeleteFromMetadataAsync(
                    computeEngineMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    new Mock<IProjectModelInstanceNode>().Object,
                    new AuthorizedPublicKeysModel.Item(
                        MetadataAuthorizedPublicKey.Parse("login:ssh-rsa key user"),
                        KeyAuthorizationMethods.Oslogin),
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeEngineMock.Verify(s => s.UpdateMetadataAsync(
                It.IsAny<InstanceLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);

            computeEngineMock.Verify(s => s.UpdateCommonInstanceMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenAuthorizationMethodIsInstanceMetadataAndNodeIsInstance_ThenDeleteFromOsLoginDeletesKeyDeletesInstanceMetadata()
        {
            var computeEngineMock = new Mock<IComputeEngineClient>();

            var instance = new Mock<IProjectModelInstanceNode>();
            instance
                .SetupGet(i => i.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            await AuthorizedPublicKeysModel.DeleteFromMetadataAsync(
                    computeEngineMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    instance.Object,
                    new AuthorizedPublicKeysModel.Item(
                        MetadataAuthorizedPublicKey.Parse("login:ssh-rsa key user"),
                        KeyAuthorizationMethods.InstanceMetadata),
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeEngineMock.Verify(s => s.UpdateMetadataAsync(
                It.IsAny<InstanceLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            computeEngineMock.Verify(s => s.UpdateCommonInstanceMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenAuthorizationMethodIsProjectMetadataAndNodeIsInstance_ThenDeleteFromOsLoginDeletesKeyDeletesProjectMetadata()
        {
            var computeEngineMock = new Mock<IComputeEngineClient>();
            computeEngineMock.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project());

            var instance = new Mock<IProjectModelInstanceNode>();
            instance
                .SetupGet(i => i.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            await AuthorizedPublicKeysModel.DeleteFromMetadataAsync(
                    computeEngineMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    instance.Object,
                    new AuthorizedPublicKeysModel.Item(
                        MetadataAuthorizedPublicKey.Parse("login:ssh-rsa key user"),
                        KeyAuthorizationMethods.ProjectMetadata),
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeEngineMock.Verify(s => s.UpdateMetadataAsync(
                It.IsAny<InstanceLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);

            computeEngineMock.Verify(s => s.UpdateCommonInstanceMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenAuthorizationMethodIsProjectMetadataAndNodeIsProject_ThenDeleteFromOsLoginDeletesKeyDeletesProjectMetadata()
        {
            var computeEngineMock = new Mock<IComputeEngineClient>();
            computeEngineMock.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project());

            var project = new Mock<IProjectModelProjectNode>();
            project
                .SetupGet(i => i.Project)
                .Returns(new ProjectLocator("project-1"));

            await AuthorizedPublicKeysModel.DeleteFromMetadataAsync(
                    computeEngineMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    project.Object,
                    new AuthorizedPublicKeysModel.Item(
                        MetadataAuthorizedPublicKey.Parse("login:ssh-rsa key user"),
                        KeyAuthorizationMethods.ProjectMetadata),
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeEngineMock.Verify(s => s.UpdateMetadataAsync(
                It.IsAny<InstanceLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);

            computeEngineMock.Verify(s => s.UpdateCommonInstanceMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
