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
using Google.Solutions.Apis;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.Test;
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

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.ProjectExplorer
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

        private const string SampleProjectId = "project-1";

        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        private ProjectRepository CreateProjectRepository()
        {
            return new ProjectRepository(this.hkcu.CreateSubKey(TestKeyPath));
        }

        private static Mock<IResourceManagerClient> CreateResourceManagerClient()
        {
            var client = new Mock<IResourceManagerClient>();
            client
                .Setup(a => a.GetProjectAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = SampleProjectId,
                    Name = $"[{SampleProjectId}]"
                });
            return client;
        }

        private ProjectExplorerSettings CreateProjectExplorerSettings()
        {
            return new ProjectExplorerSettings(
                new ApplicationSettingsRepository(
                    this.hkcu.CreateSubKey(TestKeyPath),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current),
                false);
        }

        private static Mock<IComputeEngineClient> CreateComputeEngineClient()
        {
            var client = new Mock<IComputeEngineClient>();
            client
                .Setup(a => a.ListInstancesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    SampleWindowsInstanceInZone1,
                    SampleLinuxInstanceInZone1
                });
            return client;
        }

        private static ProjectExplorerViewModel CreateViewModel(
            IComputeEngineClient computeClient,
            IResourceManagerClient resourceManagerClient,
            IProjectRepository projectRepository,
            IProjectExplorerSettings projectExplorerSettings,
            IEventQueue eventQueue,
            ISessionBroker sessionBroker,
            ICloudConsoleClient cloudConsoleClient)
        {
            var workspace = new ProjectWorkspace(
                computeClient,
                resourceManagerClient,
                projectRepository,
                eventQueue);

            return new ProjectExplorerViewModel(
                projectExplorerSettings,
                new SynchronousJobService(),
                eventQueue,
                sessionBroker,
                workspace,
                cloudConsoleClient)
            {
                View = new Control()
            };
        }

        private static async Task<ObservableCollection<ProjectExplorerViewModel.ViewModelNode>> GetInstancesAsync(
            ProjectExplorerViewModel viewModel)
        {
            var projects = await viewModel.RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false);

            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false);

            return await zones[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // OS filter.
        //---------------------------------------------------------------------

        [Test]
        public void IsXxxIncluded_WhenUsingDefaultSettings_ThenWindowsAndLinuxIsIncluded()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            Assert.IsTrue(viewModel.IsWindowsIncluded);
            Assert.IsTrue(viewModel.IsLinuxIncluded);
        }

        [Test]
        public async Task IsXxxIncluded_WhenOsFilterChanged_ThenViewModelIsUpdated()
        {
            var computeClient = CreateComputeEngineClient();
            var viewModel = CreateViewModel(
                computeClient.Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(false);
            Assert.AreEqual(2, instances.Count);

            viewModel.OperatingSystemsFilter = OperatingSystems.Linux;
            instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(false);
            Assert.AreEqual(1, instances.Count);

            viewModel.OperatingSystemsFilter = OperatingSystems.All;
            instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(false);
            Assert.AreEqual(2, instances.Count);

            // Reapplying filter must not cause reload.
            computeClient.Verify(
                a => a.ListInstancesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void IsXxxIncluded_WhenIsWindowsIncludedChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.IsWindowsIncluded,
                v =>
                {
                    Assert.IsTrue(v);
                    eventCount++;
                },
                new Mock<IBindingContext>().Object);

            initialViewModel.IsWindowsIncluded = true;
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void IsXxxIncluded_WhenIsLinuxIncludedChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);
            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.IsLinuxIncluded,
                v =>
                {
                    Assert.IsTrue(v);
                    eventCount++;
                },
                new Mock<IBindingContext>().Object);

            initialViewModel.IsLinuxIncluded = true;
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void IsXxxIncluded_WhenOperatingSystemFilterChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);
            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.OperatingSystemsFilter,
                v =>
                {
                    Assert.AreEqual(OperatingSystems.Linux, v);
                    eventCount++;
                },
                new Mock<IBindingContext>().Object);

            initialViewModel.OperatingSystemsFilter = OperatingSystems.Linux;
            Assert.AreEqual(1, eventCount);
        }

        //---------------------------------------------------------------------
        // Instance filter.
        //---------------------------------------------------------------------

        [Test]
        public async Task InstanceFilter_WhenInstanceFilterChanged_ThenViewModelIsUpdated()
        {
            var computeClient = CreateComputeEngineClient();
            var viewModel = CreateViewModel(
                computeClient.Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var instances = await GetInstancesAsync(viewModel).ConfigureAwait(true);
            Assert.AreEqual(2, instances.Count);

            viewModel.InstanceFilter = SampleLinuxInstanceInZone1.Name.Substring(4);
            instances = await GetInstancesAsync(viewModel).ConfigureAwait(true);
            Assert.AreEqual(1, instances.Count);

            viewModel.InstanceFilter = null;
            instances = await GetInstancesAsync(viewModel).ConfigureAwait(true);
            Assert.AreEqual(2, instances.Count);

            // Reapplying filter must not cause reload.
            computeClient.Verify(
                a => a.ListInstancesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void InstanceFilter_WhenInstanceFilterChanged_ThenEventIsFired()
        {
            var initialViewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            var eventCount = 0;
            initialViewModel.OnPropertyChange(
                m => m.InstanceFilter,
                v =>
                {
                    Assert.AreEqual("test", v);
                    eventCount++;
                },
                new Mock<IBindingContext>().Object);

            initialViewModel.InstanceFilter = "test";
            Assert.AreEqual(1, eventCount);
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public void Projects_WhenNoProjectsAdded_ThenProjectsIsEmpty()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            CollectionAssert.IsEmpty(viewModel.Projects);
        }

        [Test]
        public async Task Projects_WhenProjectAdded_ThenProjectsIsNotEmpty()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(viewModel.Projects);
        }

        //---------------------------------------------------------------------
        // Add/RemoveProject.
        //---------------------------------------------------------------------

        [Test]
        public async Task AddProjects_UpdatesViewModel()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            var initialProjectsList = await viewModel
                .ExpandRootAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(initialProjectsList.Any());

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var updatedProjectsList = await viewModel.RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false);

            Assert.AreEqual(1, updatedProjectsList.Count);
        }

        [Test]
        public async Task RemoveProject_UpdatesViewModel()
        {
            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(new ProjectLocator(SampleProjectId));

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                projectRepository,
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            var initialProjectsList = await viewModel
                .ExpandRootAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, initialProjectsList.Count());

            await viewModel
                .RemoveProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var updatedProjectsList = await viewModel.RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false);

            Assert.IsFalse(updatedProjectsList.Any());
        }

        //---------------------------------------------------------------------
        // ExpandRoot - project ordering.
        //---------------------------------------------------------------------

        [Test]
        public async Task ExpandRoot_WhenMultipleProjectsAdded_ThenProjectsAreOrderedByDisplayName()
        {
            var project2 = new ProjectLocator("project-2");
            var inaccessible = new ProjectLocator("inaccessible-1");

            var resourceManagerClient = CreateResourceManagerClient();
            resourceManagerClient.Setup(a => a.GetProjectAsync(
                    project2,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "project-2",
                    Name = "project-2"  // Same as id
                });
            resourceManagerClient.Setup(a => a.GetProjectAsync(
                    inaccessible,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("inaccessible", new Exception()));

            var projectRepository = CreateProjectRepository();
            projectRepository.AddProject(project2);
            projectRepository.AddProject(inaccessible);
            projectRepository.AddProject(new ProjectLocator(SampleProjectId));

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                resourceManagerClient.Object,
                projectRepository,
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            var projects = (await viewModel
                .ExpandRootAsync()
                .ConfigureAwait(true)).ToList();

            Assert.AreEqual("[project-1] (project-1)", projects[0].Text);
            Assert.AreEqual("inaccessible project (inaccessible-1)", projects[1].Text);
            Assert.AreEqual("project-2", projects[2].Text);
        }

        //---------------------------------------------------------------------
        // Refresh.
        //---------------------------------------------------------------------

        [Test]
        public async Task Refresh_WhenReloadProjectsIsTrue_ThenRefreshReloadsProjects()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            await viewModel
                .RefreshAsync(true)
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task Refresh_WhenReloadProjectsIsFalse_ThenRefreshReloadsZones()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.Fail("Projects should not be reloaded");
            };
            zones.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            await viewModel
                .RefreshAsync(false)
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task Refresh_WhenSelectedNodeIsNull_ThenRefreshSelectedNodeAsyncReloadsProjects()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = null;
            await viewModel
                .RefreshSelectedNodeAsync()
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task Refresh_WhenSelectedNodeIsRoot_ThenRefreshSelectedNodeAsyncReloadsProjects()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            projects.CollectionChanged += (sender, args) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Reset, args.Action);
                nofifications++;
            };

            viewModel.SelectedNode = viewModel.RootNode;
            await viewModel
                .RefreshSelectedNodeAsync()
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task Refresh_WhenSelectedNodeIsProject_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
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
            await viewModel
                .RefreshSelectedNodeAsync()
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task Refresh_WhenSelectedNodeIsZone_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
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
            await viewModel
                .RefreshSelectedNodeAsync()
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task Refresh_WhenSelectedNodeIsInstance_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var instances = await zones[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
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
            await viewModel
                .RefreshSelectedNodeAsync()
                .ConfigureAwait(true);

            Assert.AreEqual(2, nofifications, "expecting 2 resets (Clear, AddRange)");
            Assert.AreEqual(1, (await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true)).Count);
        }

        //---------------------------------------------------------------------
        // IsConnected tracking.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsConnected_WhenInstanceConnected_ThenIsConnectedIsTrue()
        {
            var sessionBroker = new Mock<ISessionBroker>();
            sessionBroker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(true);

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                sessionBroker.Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = (await GetInstancesAsync(viewModel).ConfigureAwait(true))
                .Cast<ProjectExplorerViewModel.InstanceViewModelNode>()
                .ToList();

            Assert.IsTrue(instances[0].IsConnected);
            Assert.IsTrue(instances[1].IsConnected);
        }

        [Test]
        public async Task IsConnected_WhenSessionEventFired_ThenIsConnectedIsUpdated()
        {
            var eventQueue = new Mock<IEventQueue>();

            // Capture event handlers that the view model will register.
            Action<SessionStartedEvent>? sessionStartedEventHandler = null;
            eventQueue
                .Setup(e => e.Subscribe(
                    It.IsAny<Action<SessionStartedEvent>>(),
                    SubscriptionOptions.None))
                .Callback<Action<SessionStartedEvent>, SubscriptionOptions>((e, _) => sessionStartedEventHandler = e);

            Action<SessionEndedEvent>? sessionEndedEventHandler = null;
            eventQueue
                .Setup(e => e.Subscribe(
                    It.IsAny<Action<SessionEndedEvent>>(),
                    SubscriptionOptions.None))
                .Callback<Action<SessionEndedEvent>>(e => sessionEndedEventHandler = e);

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                eventQueue.Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = (await GetInstancesAsync(viewModel).ConfigureAwait(true))
                .Cast<ProjectExplorerViewModel.InstanceViewModelNode>()
                .ToList();

            Assert.IsNotNull(sessionStartedEventHandler);
            Assert.IsNotNull(sessionEndedEventHandler);

            Assert.IsFalse(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);

            sessionStartedEventHandler!(
                new SessionStartedEvent((InstanceLocator)instances[0].Locator!));

            Assert.IsTrue(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);

            sessionEndedEventHandler!(
                new SessionEndedEvent((InstanceLocator)instances[0].Locator!));

            Assert.IsFalse(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);

            sessionStartedEventHandler!(
                new SessionStartedEvent(new InstanceLocator(SampleProjectId, "zone-1", "unknown-1")));

            Assert.IsFalse(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);
        }

        //---------------------------------------------------------------------
        // ImageIndex.
        //---------------------------------------------------------------------

        [Test]
        public async Task ImageIndex_WhenInstanceConnected_ThenNodeUsesRightIcon()
        {
            var sessionBroker = new Mock<ISessionBroker>();
            sessionBroker.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(true);

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                sessionBroker.Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = (await GetInstancesAsync(viewModel).ConfigureAwait(true))
                .Cast<ProjectExplorerViewModel.InstanceViewModelNode>()
                .ToList();

            Assert.AreEqual(
                ProjectExplorerViewModel.InstanceViewModelNode.LinuxConnectedIconIndex,
                instances[0].ImageIndex);
            Assert.AreEqual(
                ProjectExplorerViewModel.InstanceViewModelNode.WindowsConnectedIconIndex,
                instances[1].ImageIndex);
        }

        [Test]
        public async Task ImageIndex_WhenInstanceDisonnected_ThenNodeUsesRightIcon()
        {
            new Mock<ISessionBroker>().Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = (await GetInstancesAsync(viewModel).ConfigureAwait(true))
                .Cast<ProjectExplorerViewModel.InstanceViewModelNode>()
                .ToList();

            Assert.AreEqual(
                ProjectExplorerViewModel.InstanceViewModelNode.LinuxDisconnectedIconIndex,
                instances[0].ImageIndex);
            Assert.AreEqual(
                ProjectExplorerViewModel.InstanceViewModelNode.WindowsDisconnectedIconIndex,
                instances[1].ImageIndex);
        }

        //---------------------------------------------------------------------
        // Instance state tracking.
        //---------------------------------------------------------------------

        [Test]
        public async Task InstanceStateChanged_WhenInstanceStateChanges_ThenProjectIsReloaded()
        {
            var eventQueue = new Mock<IEventQueue>();

            Func<InstanceStateChangedEvent, Task>? eventHandler = null;
            eventQueue
                .Setup(e => e.Subscribe(
                    It.IsAny<Func<InstanceStateChangedEvent, Task>>(),
                    SubscriptionOptions.None))
                .Callback<Func<InstanceStateChangedEvent, Task>, SubscriptionOptions>((e, _) => eventHandler = e);

            var computeClient = CreateComputeEngineClient();
            var viewModel = CreateViewModel(
                computeClient.Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                eventQueue.Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            Assert.IsNotNull(eventHandler);

            await eventHandler!(new InstanceStateChangedEvent(
                new InstanceLocator(
                    SampleProjectId,
                    SampleLinuxInstanceInZone1.Zone,
                    SampleLinuxInstanceInZone1.Name),
                true));

            // Event must not cause reload.
            computeClient.Verify(
                a => a.ListInstancesAsync(
                    new ProjectLocator(SampleProjectId),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // UnloadSelectedProject.
        //---------------------------------------------------------------------

        [Test]
        public async Task UnloadSelectedProject_WhenSelectedNodeIsProject_ThenUnloadSelectedProjectAsyncUnloadsProject()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];

            Assert.AreEqual(1, projects.Count);
            await viewModel
                .UnloadSelectedProjectAsync()
                .ConfigureAwait(true);
            Assert.AreEqual(0, projects.Count);
        }

        [Test]
        public async Task UnloadSelectedProject_WhenSelectedNodeIsInstance_ThenUnloadSelectedProjectAsyncDoesNothing()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(true);

            viewModel.SelectedNode = instances[0];

            Assert.AreEqual(2, instances.Count);
            await viewModel
                .UnloadSelectedProjectAsync()
                .ConfigureAwait(true);
            Assert.AreEqual(2, instances.Count);
        }

        //---------------------------------------------------------------------
        // OpenInCloudConsole.
        //---------------------------------------------------------------------

        [Test]
        public async Task OpenInCloudConsole_WhenSelectedNodeIsProject()
        {
            var consoleClient = new Mock<ICloudConsoleClient>();
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                consoleClient.Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];
            viewModel.OpenInCloudConsole();

            consoleClient.Verify(c => c.OpenInstanceList(
                It.Is<ProjectLocator>(l => l.Name == SampleProjectId)),
                Times.Once);
        }

        [Test]
        public async Task OpenInCloudConsole_WhenSelectedNodeIsZone()
        {
            var consoleClient = new Mock<ICloudConsoleClient>();
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                consoleClient.Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = zones[0];
            viewModel.OpenInCloudConsole();

            consoleClient.Verify(c => c.OpenInstanceList(
                It.Is<ZoneLocator>(l => l.Name == "zone-1")),
                Times.Once);
        }

        [Test]
        public async Task OpenInCloudConsole_WhenSelectedNodeIsInstance()
        {
            var consoleClient = new Mock<ICloudConsoleClient>();
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                consoleClient.Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(true);

            viewModel.SelectedNode = instances[0];
            viewModel.OpenInCloudConsole();

            consoleClient.Verify(c => c.OpenInstanceDetails(
                It.IsAny<InstanceLocator>()),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ConfigureIapAccess.
        //---------------------------------------------------------------------

        [Test]
        public async Task ConfigureIapAccess_WhenSelectedNodeIsProject()
        {
            var consoleClient = new Mock<ICloudConsoleClient>();
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                consoleClient.Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];
            viewModel.ConfigureIapAccess();

            consoleClient.Verify(c => c.OpenIapSecurity(
                It.Is<string>(id => id == SampleProjectId)),
                Times.Once);
        }

        [Test]
        public async Task ConfigureIapAccess_WhenSelectedNodeIsZone()
        {
            var consoleClient = new Mock<ICloudConsoleClient>();
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                consoleClient.Object);
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = zones[0];
            viewModel.ConfigureIapAccess();

            consoleClient.Verify(c => c.OpenIapSecurity(
                It.Is<string>(id => id == SampleProjectId)),
                Times.Once);
        }

        [Test]
        public async Task ConfigureIapAccess_WhenSelectedNodeIsInstance()
        {
            var consoleClient = new Mock<ICloudConsoleClient>();
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                consoleClient.Object);
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(true);

            viewModel.SelectedNode = instances[0];
            viewModel.ConfigureIapAccess();

            consoleClient.Verify(c => c.OpenIapSecurity(
                It.Is<string>(id => id == SampleProjectId)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // Command visibility.
        //---------------------------------------------------------------------

        [Test]
        public void IsXxxCommandVisible_WhenSelectedNodeIsRoot_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);
            viewModel.SelectedNode = viewModel.RootNode;

            Assert.IsFalse(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsCloudConsoleCommandVisible);
        }

        [Test]
        public async Task IsXxxCommandVisible_WhenSelectedNodeIsProject_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];

            Assert.IsTrue(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsCloudConsoleCommandVisible);
        }

        [Test]
        public async Task IsXxxCommandVisible_WhenSelectedNodeIsZone_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = zones[0];

            Assert.IsFalse(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsCloudConsoleCommandVisible);
        }

        [Test]
        public async Task IsXxxCommandVisible_WhenSelectedNodeIsInstance_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(true);

            viewModel.SelectedNode = instances[0];

            Assert.IsFalse(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsCloudConsoleCommandVisible);
        }

        //---------------------------------------------------------------------
        // Expand tracking
        //---------------------------------------------------------------------

        [Test]
        public async Task IsExpanded_WhenProjectAddedAndNotSavedAsCollapsed_ThenProjectIsExpanded()
        {
            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                CreateProjectExplorerSettings(),
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var projectViewModelNodes = (await viewModel.RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false))
                .Cast<ProjectExplorerViewModel.ProjectViewModelNode>()
                .ToList();

            Assert.AreEqual(1, projectViewModelNodes.Count);
            Assert.IsTrue(projectViewModelNodes.First().IsExpanded);
        }

        [Test]
        public async Task IsExpanded_WhenProjectAddedAndSavedAsCollapsed_ThenProjectIsCollapsed()
        {
            var projectExplorerSettings = CreateProjectExplorerSettings();
            projectExplorerSettings.CollapsedProjects.Add(new ProjectLocator(SampleProjectId));

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                projectExplorerSettings,
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var projectViewModelNodes = (await viewModel.RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false))
                .Cast<ProjectExplorerViewModel.ProjectViewModelNode>()
                .ToList();

            Assert.AreEqual(1, projectViewModelNodes.Count);
            Assert.IsFalse(projectViewModelNodes.First().IsExpanded);
        }

        [Test]
        public async Task IsExpanded_RemoveProjects_WhenProjectRemoved_ThenProjectIsRemovedFromSettings()
        {
            var projectExplorerSettings = CreateProjectExplorerSettings();
            projectExplorerSettings.CollapsedProjects.Add(new ProjectLocator(SampleProjectId));

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                projectExplorerSettings,
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            await viewModel
                .RemoveProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            CollectionAssert.DoesNotContain(
                projectExplorerSettings.CollapsedProjects,
                new ProjectLocator(SampleProjectId));
        }

        [Test]
        public async Task IsExpanded_WhenProjectExpandedOrCollapsed_ThenSettingsAreUpdated()
        {
            var projectExplorerSettings = CreateProjectExplorerSettings();

            var viewModel = CreateViewModel(
                CreateComputeEngineClient().Object,
                CreateResourceManagerClient().Object,
                CreateProjectRepository(),
                projectExplorerSettings,
                new Mock<IEventQueue>().Object,
                new Mock<ISessionBroker>().Object,
                new Mock<ICloudConsoleClient>().Object);

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var projectViewModelNodes = (await viewModel.RootNode
                .GetFilteredChildrenAsync(false)
                .ConfigureAwait(false))
                .Cast<ProjectExplorerViewModel.ProjectViewModelNode>()
                .ToList();

            Assert.AreEqual(1, projectViewModelNodes.Count);
            var project = projectViewModelNodes.First();

            project.IsExpanded = false;
            project.IsExpanded = false;
            project.IsExpanded = false;

            CollectionAssert.Contains(
                projectExplorerSettings.CollapsedProjects,
                project.ProjectNode.Project);

            project.IsExpanded = true;
            project.IsExpanded = true;
            project.IsExpanded = true;

            CollectionAssert.DoesNotContain(
                projectExplorerSettings.CollapsedProjects,
                project.ProjectNode.Project);
        }
    }
}
