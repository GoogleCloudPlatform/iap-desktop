//
// Copyright 2021 Google LLC
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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Views.ProjectExplorer
{
    [TestFixture]
    public class TestProjectExplorerViewModel : ApplicationFixtureBase
    {
        private static readonly Instance SampleWindowsInstanceInZone1 = new Instance()
        {
            Id = 1u,
            Name = "windows-1",
            Disks = new[]
            {
                new AttachedDisk()
                {
                    GuestOsFeatures = new []
                    {
                        new GuestOsFeature()
                        {
                            Type = "WINDOWS"
                        }
                    }
                }
            },
            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1",
            Status = "RUNNING"
        };

        private static readonly Instance SampleLinuxInstanceInZone1 = new Instance()
        {
            Id = 2u,
            Name = "linux-zone-1",
            Disks = new[]
            {
                new AttachedDisk()
                {
                }
            },
            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1",
            Status = "RUNNING"
        };

        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ApplicationSettingsRepository settingsRepository;
        private ProjectRepository projectRepository;

        private Mock<IComputeEngineAdapter> computeEngineAdapterMock;
        private Mock<IResourceManagerAdapter> resourceManagerAdapterMock;
        private Mock<ICloudConsoleService> cloudConsoleServiceMock;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.settingsRepository = new ApplicationSettingsRepository(hkcu.CreateSubKey(TestKeyPath));
            this.projectRepository = new ProjectRepository(
                hkcu.CreateSubKey(TestKeyPath),
                new Mock<IEventService>().Object);

            this.resourceManagerAdapterMock = new Mock<IResourceManagerAdapter>();
            this.resourceManagerAdapterMock.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "project-1",
                    Name = $"[project-1]"
                });

            this.computeEngineAdapterMock = new Mock<IComputeEngineAdapter>();
            this.computeEngineAdapterMock.Setup(a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    SampleWindowsInstanceInZone1,
                    SampleLinuxInstanceInZone1
                });

            this.cloudConsoleServiceMock = new Mock<ICloudConsoleService>();
        }

        private ProjectExplorerViewModel CreateViewModel()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton<IProjectRepository>(this.projectRepository);
            serviceRegistry.AddSingleton(this.computeEngineAdapterMock.Object);
            serviceRegistry.AddSingleton(this.resourceManagerAdapterMock.Object);
            serviceRegistry.AddMock<IEventService>();

            return new ProjectExplorerViewModel(
                new Control(),
                this.settingsRepository,
                new SynchrounousJobService(),
                new ProjectModelService(serviceRegistry),
                this.cloudConsoleServiceMock.Object);
        }

        private class SynchrounousJobService : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
                => jobFunc(CancellationToken.None);
        }

        private async Task<ObservableCollection<ProjectExplorerViewModel.ViewModelNode>> GetInstancesAsync(
            ProjectExplorerViewModel viewModel)
        {
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);
            return await zones[0].GetFilteredNodesAsync(false);
        }

        //---------------------------------------------------------------------
        // OS filter.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsingDefaultSettings_ThenWindowsAndLinuxIsIncluded()
        {
            var viewModel = CreateViewModel();

            Assert.IsTrue(viewModel.IsWindowsIncluded);
            Assert.IsTrue(viewModel.IsLinuxIncluded);
        }

        [Test]
        public void WhenAllOsEnabledInSettings_ThenAllOsAreIncluded()
        {
            // Write settings.
            var initialViewModel = CreateViewModel();
            initialViewModel.IsWindowsIncluded = true;
            initialViewModel.IsLinuxIncluded = true;

            // Read again.
            var viewModel = CreateViewModel();
            Assert.IsTrue(viewModel.IsWindowsIncluded);
            Assert.IsTrue(viewModel.IsLinuxIncluded);
        }

        [Test]
        public void WhenAllOsDisabledInSettings_ThenNoOsAreIncluded()
        {
            // Write settings.
            var initialViewModel = CreateViewModel();
            initialViewModel.IsWindowsIncluded = false;
            initialViewModel.IsLinuxIncluded = false;

            // Read again.
            var viewModel = CreateViewModel();
            Assert.IsFalse(viewModel.IsWindowsIncluded);
            Assert.IsFalse(viewModel.IsLinuxIncluded);
        }

        [Test]
        public async Task WhenOsFilterChanged_ThenViewModelIsUpdated()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            var instances = await GetInstancesAsync(viewModel);
            Assert.AreEqual(2, instances.Count);

            viewModel.OperatingSystemsFilter = OperatingSystems.Linux;
            instances = await GetInstancesAsync(viewModel);
            Assert.AreEqual(1, instances.Count);

            viewModel.OperatingSystemsFilter = OperatingSystems.All;
            instances = await GetInstancesAsync(viewModel);
            Assert.AreEqual(2, instances.Count);

            // Reapplying filter must not cause reload.
            this.computeEngineAdapterMock.Verify(
                a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void WhenIsWindowsIncludedChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel();
            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.IsWindowsIncluded,
                v =>
                {
                    Assert.IsTrue(v);
                    eventCount++;
                });

            initialViewModel.IsWindowsIncluded = true;
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void WhenIsLinuxIncludedChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel();
            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.IsLinuxIncluded,
                v =>
                {
                    Assert.IsTrue(v);
                    eventCount++;
                });

            initialViewModel.IsLinuxIncluded = true;
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void WhenOperatingSystemFilterChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel();
            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.OperatingSystemsFilter,
                v =>
                {
                    Assert.AreEqual(OperatingSystems.Linux, v);
                    eventCount++;
                });

            initialViewModel.OperatingSystemsFilter = OperatingSystems.Linux;
            Assert.AreEqual(1, eventCount);
        }

        //---------------------------------------------------------------------
        // Instance filter.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceFilterChanged_ThenViewModelIsUpdated()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            var instances = await GetInstancesAsync(viewModel);
            Assert.AreEqual(2, instances.Count);

            viewModel.InstanceFilter = SampleLinuxInstanceInZone1.Name.Substring(4);
            instances = await GetInstancesAsync(viewModel);
            Assert.AreEqual(1, instances.Count);

            viewModel.InstanceFilter = null;
            instances = await GetInstancesAsync(viewModel);
            Assert.AreEqual(2, instances.Count);

            // Reapplying filter must not cause reload.
            this.computeEngineAdapterMock.Verify(
                a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void WhenInstanceFilterChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel();
            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.InstanceFilter,
                v =>
                {
                    Assert.AreEqual("test", v);
                    eventCount++;
                });

            initialViewModel.InstanceFilter = "test";
            Assert.AreEqual(1, eventCount);
        }

        //---------------------------------------------------------------------
        // Add/RemoveProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectAdded_ThenViewModelIsUpdated()
        {
            var viewModel = CreateViewModel();
            var initialProjectsList = await viewModel.ExpandRootAsync();

            Assert.IsFalse(initialProjectsList.Any());

            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            var updatedProjectsList = await viewModel.RootNode.GetFilteredNodesAsync(false);
            Assert.AreEqual(1, updatedProjectsList.Count);
        }

        [Test]
        public async Task WhenProjectRemoved_ThenViewModelIsUpdated()
        {
            await this.projectRepository.AddProjectAsync("project-1");

            var viewModel = CreateViewModel();
            var initialProjectsList = await viewModel.ExpandRootAsync();

            Assert.AreEqual(1, initialProjectsList.Count());

            await viewModel.RemoveProjectAsync(new ProjectLocator("project-1"));

            var updatedProjectsList = await viewModel.RootNode.GetFilteredNodesAsync(false);
            Assert.IsFalse(updatedProjectsList.Any());
        }

        //---------------------------------------------------------------------
        // Refresh.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenReloadProjectsIsTrue_ThenRefreshReloadsProjects()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            await viewModel.RefreshAsync(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
        }

        [Test]
        public async Task WhenReloadProjectsIsFalse_ThenRefreshReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.Fail("Projects should not be reloaded");
            };
            zones.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            await viewModel.RefreshAsync(false);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
            Assert.AreEqual(1, (await projects[0].GetFilteredNodesAsync(false)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsNull_ThenRefreshSelectedNodeAsyncReloadsProjects()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = null;
            await viewModel.RefreshSelectedNodeAsync();

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsRoot_ThenRefreshSelectedNodeAsyncReloadsProjects()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = viewModel.RootNode;
            await viewModel.RefreshSelectedNodeAsync();

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.Fail("Projects should not be reloaded");
            };
            zones.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = projects[0];
            await viewModel.RefreshSelectedNodeAsync();

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
            Assert.AreEqual(1, (await projects[0].GetFilteredNodesAsync(false)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.Fail("Projects should not be reloaded");
            };
            zones.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = zones[0];
            await viewModel.RefreshSelectedNodeAsync();

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
            Assert.AreEqual(1, (await projects[0].GetFilteredNodesAsync(false)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));

            int nofifications = 0;
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);
            var instances = await zones[0].GetFilteredNodesAsync(false);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.Fail("Projects should not be reloaded");
            };
            zones.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = instances[0];
            await viewModel.RefreshSelectedNodeAsync();

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel.RootNode.GetFilteredNodesAsync(false)).Count);
            Assert.AreEqual(1, (await projects[0].GetFilteredNodesAsync(false)).Count);
        }

        //---------------------------------------------------------------------
        // UnloadSelectedProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenUnloadSelectedProjectAsyncUnloadsProject()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);

            viewModel.SelectedNode = projects[0];

            Assert.AreEqual(1, projects.Count);
            await viewModel.UnloadSelectedProjectAsync();
            Assert.AreEqual(0, projects.Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenUnloadSelectedProjectAsyncDoesNothing()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var instances = await GetInstancesAsync(viewModel);

            viewModel.SelectedNode = instances[0];

            Assert.AreEqual(2, instances.Count);
            await viewModel.UnloadSelectedProjectAsync();
            Assert.AreEqual(2, instances.Count);
        }

        //---------------------------------------------------------------------
        // OpenInCloudConsole.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenOpenInCloudConsoleOpensInstancesList()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);

            viewModel.SelectedNode = projects[0];
            viewModel.OpenInCloudConsole();

            this.cloudConsoleServiceMock.Verify(c => c.OpenInstanceList(
                It.Is<ProjectLocator>(l => l.Name == "project-1")),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenUnloadSelectedProjectOpensInstancesList()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);

            viewModel.SelectedNode = zones[0];
            viewModel.OpenInCloudConsole();

            this.cloudConsoleServiceMock.Verify(c => c.OpenInstanceList(
                It.Is<ZoneLocator>(l => l.Name == "zone-1")),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenUnloadSelectedProjectOpensInstanceDetails()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var instances = await GetInstancesAsync(viewModel);

            viewModel.SelectedNode = instances[0];
            viewModel.OpenInCloudConsole();

            this.cloudConsoleServiceMock.Verify(c => c.OpenInstanceDetails(
                It.IsAny<InstanceLocator>()),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ConfigureIapAccess.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenConfigureIapAccessOpensProjectConfig()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);

            viewModel.SelectedNode = projects[0];
            viewModel.ConfigureIapAccess();

            this.cloudConsoleServiceMock.Verify(c => c.ConfigureIapAccess(
                It.Is<string>(id => id == "project-1")),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenConfigureIapAccessOpensProjectConfig()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var projects = await viewModel.RootNode.GetFilteredNodesAsync(false);
            var zones = await projects[0].GetFilteredNodesAsync(false);

            viewModel.SelectedNode = zones[0];
            viewModel.ConfigureIapAccess();

            this.cloudConsoleServiceMock.Verify(c => c.ConfigureIapAccess(
                It.Is<string>(id => id == "project-1")),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenConfigureIapAccessOpensProjectConfig()
        {
            var viewModel = CreateViewModel();
            await viewModel.AddProjectAsync(new ProjectLocator("project-1"));
            var instances = await GetInstancesAsync(viewModel);

            viewModel.SelectedNode = instances[0];
            viewModel.ConfigureIapAccess();

            this.cloudConsoleServiceMock.Verify(c => c.ConfigureIapAccess(
                It.Is<string>(id => id == "project-1")),
                Times.Once);
        }
    }
}
