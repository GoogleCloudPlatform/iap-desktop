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
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Os.Services.InstanceDetails;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Services.InstanceDetails
{
    [TestFixture]
    public class TestInstanceDetailsViewModel : FixtureBase
    {
        private class JobServiceMock : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc) 
                => jobFunc(CancellationToken.None);
        }

        private static InstanceDetailsViewModel CreateInstanceDetailsViewModel(bool throwOnLoad)
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

            return new InstanceDetailsViewModel(registry);
        }
        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel(false);

            var node = new Mock<IProjectExplorerCloudNode>();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstanceDetailsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel(false);

            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstanceDetailsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel(false);

            var node = new Mock<IProjectExplorerZoneNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstanceDetailsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenGridIsPopulated()
        {
            var viewModel = CreateInstanceDetailsViewModel(false);

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsNotNull(viewModel.InspectedObject);
            StringAssert.Contains(InstanceDetailsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("instance-1", viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNodeAndLoadFails_ThenGridIsDisabled()
        {
            var viewModel = CreateInstanceDetailsViewModel(true);

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch to denied node.
            var deniedNode = new Mock<IProjectExplorerVmInstanceNode>();
            deniedNode.SetupGet(n => n.ProjectId).Returns("project-1");
            deniedNode.SetupGet(n => n.ZoneId).Returns("zone-1");
            deniedNode.SetupGet(n => n.InstanceName).Returns("denied-1");

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => viewModel.SwitchToModelAsync(deniedNode.Object).Wait());

            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.IsNull(viewModel.InspectedObject);
            Assert.AreEqual(InstanceDetailsViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }
    }
}
