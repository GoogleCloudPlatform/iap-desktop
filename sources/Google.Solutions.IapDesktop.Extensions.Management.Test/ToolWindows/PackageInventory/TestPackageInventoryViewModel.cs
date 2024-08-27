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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.PackageInventory
{
    [TestFixture]
    public class TestPackageInventoryViewModel : ApplicationFixtureBase
    {
        private static GuestOsInfo CreateGuestOsInfo(
            InstanceLocator locator,
            PackageInventoryType type,
            IList<Package>? packages)
        {
            var packageInfo = packages == null ? null : new GuestPackages(
                packages,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

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
                type == PackageInventoryType.InstalledPackages ? packageInfo : null,
                type == PackageInventoryType.AvailablePackages ? packageInfo : null);
        }

        private static PackageInventoryViewModel CreateViewModel(PackageInventoryType type)
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<IJobService>(new SynchronousJobService());

            var packageInventory = new Mock<IGuestOsInventory>();
            packageInventory.Setup(s => s.GetInstanceInventoryAsync(
                        It.Is<InstanceLocator>(loc => loc.Name == "instance-1"),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateGuestOsInfo(
                    new InstanceLocator("project-1", "zone-1", "instance-1"),
                    type,
                    new Package[] {
                        new Package("package-1", "arch-1", "ver-1"),
                        new Package("package-2", "arch-1", "ver-2")
                    }));

            packageInventory.Setup(s => s.GetInstanceInventoryAsync(
                        It.Is<InstanceLocator>(loc => loc.Name == "instance-3"),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((GuestOsInfo?)null);

            packageInventory.Setup(s => s.ListZoneInventoryAsync(
                        It.IsAny<ZoneLocator>(),
                        It.IsAny<OperatingSystems>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new GuestOsInfo[]
                    {
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-1", "instance-1"),
                            type,
                            new Package[] {
                                new Package("package-1", "arch-1", "ver-1"),
                                new Package("package-2", "arch-1", "ver-2")
                            })
                    });

            packageInventory.Setup(s => s.ListProjectInventoryAsync(
                        It.IsAny<string>(),
                        It.IsAny<OperatingSystems>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new GuestOsInfo[]
                    {
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-1", "instance-1"),
                            type,
                            new Package[] {
                                new Package("package-1", "ARCH-1", "ver-1"),
                                new Package("package-2", "ARCH-1", "ver-2")
                            }),
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-2", "instance-2"),
                            type,
                            new Package[] {
                                new Package("package-3", "ARCH-1", "ver-1"),
                                new Package("package-4", "ARCH-2", "ver-3")
                            }),
                        CreateGuestOsInfo(
                            new InstanceLocator("project-1", "zone-2", "instance-3"),
                            type,
                            null)
                    });

            registry.AddSingleton<IGuestOsInventory>(packageInventory.Object);

            return new PackageInventoryViewModel(registry)
            {
                InventoryType = type
            };
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenListIsDisabled()
        {
            var viewModel = CreateViewModel(PackageInventoryType.InstalledPackages);

            var node = new Mock<IProjectModelCloudNode>();
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsPackageListEnabled.Value);
            Assert.IsNull(viewModel.InformationText.Value);
            Assert.AreEqual("Installed packages", viewModel.WindowTitle.Value);
            Assert.IsFalse(viewModel.AllPackages.Any());
            Assert.IsFalse(viewModel.FilteredPackages.Any());
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenListIsPopulated(
            [Values(
                PackageInventoryType.AvailablePackages,
                PackageInventoryType.InstalledPackages)]  PackageInventoryType type)
        {

            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel(type);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsPackageListEnabled.Value);
            Assert.IsNull(viewModel.InformationText.Value);
            StringAssert.Contains("project-1", viewModel.WindowTitle.Value);

            Assert.AreEqual(4, viewModel.AllPackages.Count);
            Assert.AreEqual(4, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenListIsPopulated(
            [Values(
                PackageInventoryType.AvailablePackages,
                PackageInventoryType.InstalledPackages)]  PackageInventoryType type)
        {
            var node = new Mock<IProjectModelZoneNode>();
            node.SetupGet(n => n.Zone).Returns(new ZoneLocator("project-1", "zone-1"));
            node.SetupGet(n => n.DisplayName).Returns("zone-1");

            var viewModel = CreateViewModel(type);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsPackageListEnabled.Value);
            Assert.IsNull(viewModel.InformationText.Value);
            StringAssert.Contains("zone-1", viewModel.WindowTitle.Value);

            Assert.AreEqual(2, viewModel.AllPackages.Count);
            Assert.AreEqual(2, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNodeWithInventory_ThenListIsPopulated(
            [Values(
                PackageInventoryType.AvailablePackages,
                PackageInventoryType.InstalledPackages)]  PackageInventoryType type)
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.DisplayName).Returns("instance-1");
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var viewModel = CreateViewModel(type);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsPackageListEnabled.Value);
            Assert.IsNull(viewModel.InformationText.Value);
            StringAssert.Contains("instance-1", viewModel.WindowTitle.Value);

            Assert.AreEqual(2, viewModel.AllPackages.Count);
            Assert.AreEqual(2, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNodeWithoutInventory_ThenListIsPopulated(
            [Values(
                PackageInventoryType.AvailablePackages,
                PackageInventoryType.InstalledPackages)] PackageInventoryType type)
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.DisplayName).Returns("instance-3");
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-3"));

            var viewModel = CreateViewModel(type);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsPackageListEnabled.Value);
            Assert.AreEqual(
                PackageInventoryViewModel.OsInventoryNotAvailableWarning,
                viewModel.InformationText.Value);
            StringAssert.Contains("instance-3", viewModel.WindowTitle.Value);

            Assert.AreEqual(0, viewModel.AllPackages.Count);
            Assert.AreEqual(0, viewModel.FilteredPackages.Count);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLoaded_ThenFilteredPackagesContainsAllPackages()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));

            var viewModel = CreateViewModel(PackageInventoryType.InstalledPackages);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.AreEqual(4, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenFilterHasMultipleTerms_ThenFilteredPackagesContainsPackagesThatMatchAllTerms()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));

            var viewModel = CreateViewModel(PackageInventoryType.InstalledPackages);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            viewModel.Filter = "PACKAGE \t Arch-2   ver-3";

            Assert.AreEqual(1, viewModel.FilteredPackages.Count);
        }

        [Test]
        public async Task WhenFilterIsReset_ThenFilteredPackagesContainsAllPackages()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));

            var viewModel = CreateViewModel(PackageInventoryType.InstalledPackages);
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            viewModel.Filter = "   PACKAGE-3   ";
            Assert.AreEqual(1, viewModel.FilteredPackages.Count);

            viewModel.Filter = string.Empty;
            Assert.AreEqual(4, viewModel.FilteredPackages.Count);
        }
    }
}
