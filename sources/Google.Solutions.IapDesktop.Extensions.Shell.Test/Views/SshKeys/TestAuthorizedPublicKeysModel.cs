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
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshKeys
{
    [TestFixture]
    public class TestAuthorizedPublicKeysModel : ApplicationFixtureBase
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

        private Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
            IDictionary<string, string> projectMetadata,
            IDictionary<string, string> instanceMetadata)
        {
            var adapter = new Mock<IComputeEngineAdapter>();
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
                new Mock<IComputeEngineAdapter>().Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                new Mock<IProjectModelCloudNode>().Object,
                CancellationToken.None);

            Assert.IsNull(model);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginEnabled_ThenModelIncludesOsLoginKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginService>();
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
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginDisabled_ThenModelIncludesProjectKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginService>();
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
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, model.Items.ToList()[0].AuthorizationMethod);
            Assert.AreEqual("alice@gmail.com", model.Items.ToList()[0].Key.Email);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginDisabledAndProjectKeysBlocked_ThenModelIncludesNoKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginService>();
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
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);

            CollectionAssert.IsEmpty(model.Items);
        }

        //---------------------------------------------------------------------
        // LoadAsync - instance node.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenScopeIsZone_ThenLoadReturnsNull()
        {
            var model = await AuthorizedPublicKeysModel.LoadAsync(
                new Mock<IComputeEngineAdapter>().Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                new Mock<IProjectModelZoneNode>().Object,
                CancellationToken.None);

            Assert.IsNull(model);
        }


        [Test]
        public async Task WhenScopeIsInstanceAndOsLoginEnabled_ThenModelIncludesOsLoginKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginService>();
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
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateInstanceNodeMock("instance-1").Object,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndOsLoginDisabled_ThenModelIncludesProjectAndInstanceKeys()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;
            var osLoginServiceMock = new Mock<IOsLoginService>();
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
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateInstanceNodeMock("instance-1").Object,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);

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
            var osLoginServiceMock = new Mock<IOsLoginService>();
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
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateInstanceNodeMock("instance-1").Object,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);

            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, model.Items.ToList()[0].AuthorizationMethod);
            Assert.AreEqual("bob@gmail.com", model.Items.ToList()[0].Key.Email);
        }
    }
}
