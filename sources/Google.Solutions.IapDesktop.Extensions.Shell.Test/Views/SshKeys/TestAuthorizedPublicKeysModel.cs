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
                KeyAuthorizationMethods.All,
                CancellationToken.None);

            Assert.IsNull(model);
        }

        [Test]
        public async Task WhenScopeIsProjectAndNoMethodsAllowed_ThenLoadReturnsEmptyModel()
        {
            var model = await AuthorizedPublicKeysModel.LoadAsync(
                new Mock<IComputeEngineAdapter>().Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                (KeyAuthorizationMethods)0,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);
            CollectionAssert.IsEmpty(model.Items);
            CollectionAssert.IsEmpty(model.Warnings);
        }

        [Test]
        public async Task WhenScopeIsProjectAndOsLoginSelected_ThenLoadReturnsModel()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;

            var osLoginServiceMock = new Mock<IOsLoginService>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                new Mock<IComputeEngineAdapter>().Object,
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                KeyAuthorizationMethods.Oslogin,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);
            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task WhenScopeIsProjectAndProjectMetadataSelected_ThenLoadReturnsModel()
        {
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { "ssh-keys", "bob:ssh-rsa KEY bob@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                computeEngineAdapterMock.Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                KeyAuthorizationMethods.ProjectMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);
            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, model.Items.First().AuthorizationMethod);
            Assert.AreEqual("KEY", model.Items.First().Key.PublicKey);
        }

        [Test]
        public async Task WhenScopeIsProjectAndProjectKeysBlocked_ThenLoadReturnsEmptyModelWithWarning()
        {
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { "ssh-keys", "bob:ssh-rsa KEY bob@gmail.com" },
                    { MetadataAuthorizedPublicKeyProcessor.BlockProjectSshKeysFlag, "true" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                computeEngineAdapterMock.Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                KeyAuthorizationMethods.ProjectMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);
            CollectionAssert.IsEmpty(model.Items);
            Assert.AreEqual(1, model.Warnings.Count());
        }

        [Test]
        public async Task WhenScopeIsProjectOsLoginEnabled_ThenLoadReturnsEmptyModelWithWarning()
        {
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { "ssh-keys", "bob:ssh-rsa KEY bob@gmail.com" },
                    { MetadataAuthorizedPublicKeyProcessor.EnableOsLoginFlag, "true" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                computeEngineAdapterMock.Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateProjectNodeMock("project-1", "Project 1").Object,
                KeyAuthorizationMethods.ProjectMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("Project 1", model.DisplayName);
            CollectionAssert.IsEmpty(model.Items);
            Assert.AreEqual(1, model.Warnings.Count());
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
                KeyAuthorizationMethods.All,
                CancellationToken.None);

            Assert.IsNull(model);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndNoMethodsAllowed_ThenLoadReturnsEmptyModel()
        {
            var model = await AuthorizedPublicKeysModel.LoadAsync(
                new Mock<IComputeEngineAdapter>().Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateInstanceNodeMock("instance-1").Object,
                (KeyAuthorizationMethods)0,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);
            CollectionAssert.IsEmpty(model.Items);
            CollectionAssert.IsEmpty(model.Warnings);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndOsLoginSelected_ThenLoadReturnsModel()
        {
            var osLoginKey = new Mock<IAuthorizedPublicKey>().Object;

            var osLoginServiceMock = new Mock<IOsLoginService>();
            osLoginServiceMock.Setup(s => s.ListAuthorizedKeysAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { osLoginKey });

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                new Mock<IComputeEngineAdapter>().Object,
                new Mock<IResourceManagerAdapter>().Object,
                osLoginServiceMock.Object,
                CreateInstanceNodeMock("instance-1").Object,
                KeyAuthorizationMethods.Oslogin,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);
            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreEqual(KeyAuthorizationMethods.Oslogin, model.Items.First().AuthorizationMethod);
            Assert.AreSame(osLoginKey, model.Items.First().Key);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndInstanceMetadataSelected_ThenLoadReturnsModel()
        {
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
                new Mock<IOsLoginService>().Object,
                CreateInstanceNodeMock("instance-1").Object,
                KeyAuthorizationMethods.InstanceMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);
            Assert.AreEqual(1, model.Items.Count());
            Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, model.Items.First().AuthorizationMethod);
            Assert.AreEqual("BOBS-KEY", model.Items.First().Key.PublicKey);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndProjectAndInstanceMetadataSelected_ThenLoadReturnsModel()
        {
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
                new Mock<IOsLoginService>().Object,
                CreateInstanceNodeMock("instance-1").Object,
                KeyAuthorizationMethods.InstanceMetadata | KeyAuthorizationMethods.ProjectMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);
            Assert.AreEqual(2, model.Items.Count());

            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, model.Items.First().AuthorizationMethod);
            Assert.AreEqual("ALICES-KEY", model.Items.First().Key.PublicKey);

            Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, model.Items.Last().AuthorizationMethod);
            Assert.AreEqual("BOBS-KEY", model.Items.Last().Key.PublicKey);
        }

        [Test]
        public async Task WhenScopeIsInstanceAndProjectAndInstanceMetadataSelectedButProjectKeysBlocked_ThenLoadReturnsEmptyModelWithWarning()
        {
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { MetadataAuthorizedPublicKeyProcessor.BlockProjectSshKeysFlag, "true" },
                    { "ssh-keys", "bob:ssh-rsa BOBS-KEY bob@gmail.com" }
                },
                null);

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                computeEngineAdapterMock.Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateInstanceNodeMock("instance-1").Object,
                KeyAuthorizationMethods.InstanceMetadata | KeyAuthorizationMethods.ProjectMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);
            CollectionAssert.IsEmpty(model.Items);
            Assert.AreEqual(1, model.Warnings.Count());
        }

        [Test]
        public async Task WhenScopeIsInstanceAndProjectAndInstanceMetadataSelectedAndOsLoginEnabled_ThenLoadReturnsEmptyModelWithWarning()
        {
            var computeEngineAdapterMock = CreateComputeEngineAdapterMock(
                new Dictionary<string, string>
                {
                    { MetadataAuthorizedPublicKeyProcessor.EnableOsLoginFlag, "true" }
                },
                new Dictionary<string, string>
                {
                    { "ssh-keys", "bob:ssh-rsa BOBS-KEY bob@gmail.com" }
                });

            var model = await AuthorizedPublicKeysModel.LoadAsync(
                computeEngineAdapterMock.Object,
                new Mock<IResourceManagerAdapter>().Object,
                new Mock<IOsLoginService>().Object,
                CreateInstanceNodeMock("instance-1").Object,
                KeyAuthorizationMethods.InstanceMetadata | KeyAuthorizationMethods.ProjectMetadata,
                CancellationToken.None);

            Assert.IsNotNull(model);
            Assert.AreEqual("instance-1", model.DisplayName);
            CollectionAssert.IsEmpty(model.Items);
            Assert.AreEqual(1, model.Warnings.Count());
        }
    }
}
