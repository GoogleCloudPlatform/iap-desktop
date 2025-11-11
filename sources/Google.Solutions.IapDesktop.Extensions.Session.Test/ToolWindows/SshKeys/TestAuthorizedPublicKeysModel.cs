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
            ProjectLocator project,
            string displayName)
        {
            var nodeMock = new Mock<IProjectModelProjectNode>();
            nodeMock.SetupGet(n => n.DisplayName).Returns(displayName);
            nodeMock.SetupGet(n => n.Project).Returns(project);

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

        private static Mock<IComputeEngineClient> CreateComputeEngineAdapterMock(
            ProjectLocator project,
            IDictionary<string, string>? projectMetadata,
            IDictionary<string, string>? instanceMetadata)
        {
            var adapter = new Mock<IComputeEngineClient>();
            adapter
                .Setup(a => a.GetProjectAsync(
                    project,
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
                    },
                    Name = project.Name
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
        public void IsNodeSupported_WhenNodeIsCloudNode()
        {
            Assert.IsFalse(AuthorizedPublicKeysModel.IsNodeSupported(
                new Mock<IProjectModelCloudNode>().Object));
        }

        [Test]
        public void IsNodeSupported_WhenNodeIsZoneNode()
        {
            Assert.IsFalse(AuthorizedPublicKeysModel.IsNodeSupported(
                new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public void IsNodeSupported_WhenNodeIsProjectNode()
        {
            Assert.IsTrue(AuthorizedPublicKeysModel.IsNodeSupported(
                new Mock<IProjectModelProjectNode>().Object));
        }

        [Test]
        public void IsNodeSupported_WhenNodeIsWindowsInstance()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.OperatingSystem)
                .Returns(OperatingSystems.Windows);

            Assert.IsFalse(AuthorizedPublicKeysModel.IsNodeSupported(node.Object));
        }

        [Test]
        public void IsNodeSupported_WhenNodeIsLinuxInstance()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.OperatingSystem)
                .Returns(OperatingSystems.Linux);

            Assert.IsTrue(AuthorizedPublicKeysModel.IsNodeSupported(node.Object));
        }

        //---------------------------------------------------------------------
        // Load - project node.
        //---------------------------------------------------------------------

        [Test]
        public async Task Load_WhenScopeIsCloud()
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
        public async Task Load_WhenScopeIsProjectAndOsLoginEnabled()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var project = new ProjectLocator("project-1");
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                project,
                new Dictionary<string, string>
                {
                    { ComputeMetadata.EnableOsLoginFlag, "true" },
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateProjectNodeMock(project, "Project 1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model!.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task Load_WhenScopeIsProjectAndOsLoginDisabled()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var project = new ProjectLocator("project-1");
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                project,
                new Dictionary<string, string>
                {
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateProjectNodeMock(project, "Project 1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model!.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, model.Items.ToList()[0].AuthorizationMethod);
            Assert.AreEqual("alice@gmail.com", model.Items.ToList()[0].Key.Email);
        }

        [Test]
        public async Task Load_WhenScopeIsProjectAndOsLoginDisabledAndProjectKeysBlocked()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var project = new ProjectLocator("project-1");
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                project,
                new Dictionary<string, string>
                {
                    { ComputeMetadata.BlockProjectSshKeysFlag, "true" },
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                    computeEngineAdapterMock.Object,
                    new Mock<IResourceManagerClient>().Object,
                    osLoginServiceMock.Object,
                    CreateProjectNodeMock(project, "Project 1").Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model!.DisplayName);

            CollectionAssert.IsEmpty(model.Items);
        }

        //---------------------------------------------------------------------
        // Load - instance node.
        //---------------------------------------------------------------------

        [Test]
        public async Task Load_WhenScopeIsZone()
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
        public async Task Load_WhenScopeIsInstanceAndOsLoginEnabled()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var project = new ProjectLocator("project-1");
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                project,
                new Dictionary<string, string>
                {
                    { ComputeMetadata.EnableOsLoginFlag, "true" },
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
        public async Task Load_WhenScopeIsInstanceAndOsLoginDisabled()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var project = new ProjectLocator("project-1");
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                project,
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
        public async Task Load_WhenScopeIsInstanceAndOsLoginDisabledAndProjectKeysBlocked()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginProfile>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var project = new ProjectLocator("project-1");
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                project,
                new Dictionary<string, string>
                {
                    { "ssh-keys", "alice:ssh-rsa ALICES-KEY alice@gmail.com" }
                },
                new Dictionary<string, string>
                {
                    { ComputeMetadata.BlockProjectSshKeysFlag, "true" },
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
        // DeleteFromOsLogin.
        //---------------------------------------------------------------------

        [Test]
        public async Task DeleteFromOsLogin_WhenAuthorizationMethodIsInstanceMetadata()
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
        public async Task DeleteFromOsLogin_WhenAuthorizationMethodIsOslogin()
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
        // DeleteFromMetadata.
        //---------------------------------------------------------------------

        [Test]
        public async Task DeleteFromMetadata_WhenAuthorizationMethodIsOslogin()
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
                It.IsAny<ProjectLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task DeleteFromMetadata_WhenAuthorizationMethodIsInstanceMetadataAndNodeIsInstance()
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
                It.IsAny<ProjectLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task DeleteFromMetadata_WhenAuthorizationMethodIsProjectMetadataAndNodeIsInstance()
        {
            var projectId = new ProjectLocator("project-1");
            var computeEngineMock = new Mock<IComputeEngineClient>();
            computeEngineMock.Setup(a => a.GetProjectAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project()
                {
                    Name = projectId.Name
                });

            var instance = new Mock<IProjectModelInstanceNode>();
            instance
                .SetupGet(i => i.Instance)
                .Returns(new InstanceLocator(projectId, "zone-1", "instance-1"));

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
                It.IsAny<ProjectLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteFromMetadata_WhenAuthorizationMethodIsProjectMetadataAndNodeIsProject()
        {
            var projectId = new ProjectLocator("project-1");
            var computeEngineMock = new Mock<IComputeEngineClient>();
            computeEngineMock.Setup(a => a.GetProjectAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project()
                {
                    Name = projectId.Name
                });

            var project = new Mock<IProjectModelProjectNode>();
            project
                .SetupGet(i => i.Project)
                .Returns(projectId);

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
                It.IsAny<ProjectLocator>(),
                It.IsAny<Action<Metadata>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
