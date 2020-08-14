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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Views.PackageInventory
{
    [TestFixture]
    public class TestPackageInventoryViewModel
    {
        private static GuestOsInfo CreateGuestOsInfo(
            InstanceLocator locator,
            IList<Package> installedPackages)
        {
            return new GuestOsInfo(
                locator,
                null,
                null,
                null,
                null,
                null,
                new Version(),
                null,
                null,
                installedPackages == null ? null : new GuestPackages(
                    installedPackages,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                null);
        }

        private class JobServiceMock : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
                => jobFunc(CancellationToken.None);
        }

        private static PackageInventoryViewModel CreateViewModel()
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<IJobService>(new JobServiceMock());

            var inventoryService = new Mock<IInventoryService>();
            inventoryService.Setup(s => s.GetInstanceInventoryAsync(
                        It.Is<InstanceLocator>(loc => loc.Name == "instance-1"),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateGuestOsInfo(
                    new InstanceLocator("project-1", "zone-1", "instance-1"),
                    new Package[] {
                        new Package("package-1", "arch-1", "ver-1"),
                        new Package("package-2", "arch-1", "ver-2")
                    }));

            inventoryService.Setup(s => s.GetInstanceInventoryAsync(
                        It.Is<InstanceLocator>(loc => loc.Name == "instance-3"),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((GuestOsInfo)null);

            inventoryService.Setup(s => s.ListZoneInventoryAsync(
                        It.IsAny<ZoneLocator>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new GuestOsInfo[]
                    {
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-1", "instance-1"),
                            new Package[] {
                                new Package("package-1", "arch-1", "ver-1"),
                                new Package("package-2", "arch-1", "ver-2")
                            })
                    });

            inventoryService.Setup(s => s.ListProjectInventoryAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new GuestOsInfo[]
                    {
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-1", "instance-1"),
                            new Package[] {
                                new Package("package-1", "arch-1", "ver-1"),
                                new Package("package-2", "arch-1", "ver-2")
                            }),
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-2", "instance-2"),
                            new Package[] {
                                new Package("package-3", "arch-1", "ver-1"),
                                new Package("package-4", "arch-2", "ver-3")
                            }),
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-2", "instance-3"),
                            null)
                    });

            registry.AddSingleton<IInventoryService>(inventoryService.Object);

            return new PackageInventoryViewModel(registry, PackageInventoryType.InstalledPackages);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenListIsDisabled()
        {
            var viewModel = CreateViewModel();

            var node = new Mock<IProjectExplorerCloudNode>();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(CommandState.Unavailable, PackageInventoryViewModel.GetCommandState(node.Object));
            Assert.IsFalse(viewModel.IsPackageListEnabled);
            Assert.AreEqual(PackageInventoryViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            Assert.IsFalse(viewModel.AllPackages.Any());
            Assert.IsFalse(viewModel.FilteredPackages.Any());
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(CommandState.Enabled, PackageInventoryViewModel.GetCommandState(node.Object));
            Assert.IsTrue(viewModel.IsPackageListEnabled);
            StringAssert.Contains(PackageInventoryViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("project-1", viewModel.WindowTitle);

            Assert.AreEqual(4, viewModel.AllPackages.Count);
            Assert.AreEqual(4, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerZoneNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.DisplayName).Returns("zone-1");

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(CommandState.Enabled, PackageInventoryViewModel.GetCommandState(node.Object));
            Assert.IsTrue(viewModel.IsPackageListEnabled);
            StringAssert.Contains(PackageInventoryViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("zone-1", viewModel.WindowTitle);

            Assert.AreEqual(2, viewModel.AllPackages.Count);
            Assert.AreEqual(2, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNodeWithInventory_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");
            node.SetupGet(n => n.DisplayName).Returns("instance-1");
            node.SetupGet(n => n.Reference).Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(CommandState.Enabled, PackageInventoryViewModel.GetCommandState(node.Object));
            Assert.IsTrue(viewModel.IsPackageListEnabled);
            StringAssert.Contains(PackageInventoryViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("instance-1", viewModel.WindowTitle);

            Assert.AreEqual(2, viewModel.AllPackages.Count);
            Assert.AreEqual(2, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNodeWithoutInventory_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-3");
            node.SetupGet(n => n.DisplayName).Returns("instance-3");
            node.SetupGet(n => n.Reference).Returns(new InstanceLocator("project-1", "zone-1", "instance-3"));

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(CommandState.Enabled, PackageInventoryViewModel.GetCommandState(node.Object));
            Assert.IsTrue(viewModel.IsPackageListEnabled);
            StringAssert.Contains(PackageInventoryViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains("instance-3", viewModel.WindowTitle);

            Assert.AreEqual(0, viewModel.AllPackages.Count);
            Assert.AreEqual(0, viewModel.FilteredPackages.Count);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLoaded_ThenFilteredPackagesContainsAllPackages()
        {
            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(4, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenFilterHasMultipleTerms_ThenFilteredPackagesContainsPackagesThatMatchAllTerms()
        {
            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);
            
            viewModel.Filter = "PACKAGE \t Arch-2   ver-3";
            
            Assert.AreEqual(1, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenFilterIsReset_ThenFilteredPackagesContainsAllPackages()
        {
            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");

            var viewModel = CreateViewModel();
            await viewModel.SwitchToModelAsync(node.Object);

            viewModel.Filter = "   PACKAGE-3   ";
            Assert.AreEqual(1, viewModel.FilteredPackages.Count);

            viewModel.Filter = null;
            Assert.AreEqual(4, viewModel.FilteredPackages.Count);
        }
    }
}
