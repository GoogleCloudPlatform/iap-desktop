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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using Google.Solutions.IapDesktop.Extensions.Os.Views.InstanceProperties;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Views.InstanceProperties
{
    [TestFixture]
    public class TestInstancePropertiesInspectorViewModel : CommonFixtureBase
    {
        private class JobServiceMock : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
                => jobFunc(CancellationToken.None);
        }

        private static InstancePropertiesInspectorViewModel CreateInstanceDetailsViewModel()
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<IJobService>(new JobServiceMock());

            var gceAdapter = new Mock<IComputeEngineAdapter>();

            gceAdapter.Setup(a => a.GetInstanceAsync(
                It.Is((InstanceLocator loc) => loc.Name == "denied-1"),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("mock exception", null));
            gceAdapter.Setup(a => a.GetInstanceAsync(
                It.Is((InstanceLocator loc) => loc.Name == "instance-1"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.Compute.v1.Data.Instance()
                {
                    Name = "instance-1"
                });

            registry.AddSingleton<IComputeEngineAdapter>(gceAdapter.Object);
            registry.AddSingleton<IInventoryService>(new InventoryService(gceAdapter.Object));

            return new InstancePropertiesInspectorViewModel(registry);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel();

            var node = new Mock<IProjectExplorerCloudNode>();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstancePropertiesInspectorViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel();

            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstancePropertiesInspectorViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel();

            var node = new Mock<IProjectExplorerZoneNode>();
            node.SetupGet(n => n.Zone).Returns(new ZoneLocator("project-1", "zone-1"));
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstancePropertiesInspectorViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenGridIsPopulated()
        {
            var viewModel = CreateInstanceDetailsViewModel();

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.Reference).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsNotNull(viewModel.InspectedObject);
            StringAssert.Contains(InstancePropertiesInspectorViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("instance-1", viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNodeAndLoadFails_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel();

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.Reference).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch to denied node.
            var deniedNode = new Mock<IProjectExplorerVmInstanceNode>();
            deniedNode.SetupGet(n => n.Reference).Returns(
                new InstanceLocator("project-1", "zone-1", "denied-1"));

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => viewModel.SwitchToModelAsync(deniedNode.Object).Wait());

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstancePropertiesInspectorViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }
    }
}
