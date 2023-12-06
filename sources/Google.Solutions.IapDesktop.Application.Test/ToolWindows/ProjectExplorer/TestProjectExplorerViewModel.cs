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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
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

        private ApplicationSettingsRepository settingsRepository;
        private ProjectRepository projectRepository;

        private Mock<IComputeEngineClient> computeClientMock;
        private Mock<IResourceManagerClient> resourceManagerAdapterMock;
        private Mock<ICloudConsoleClient> cloudConsoleServiceMock;
        private Mock<IEventQueue> eventServiceMock;
        private Mock<IGlobalSessionBroker> sessionBrokerMock;
        private IProjectExplorerSettings projectExplorerSettings;

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.settingsRepository = new ApplicationSettingsRepository(
                this.hkcu.CreateSubKey(TestKeyPath),
                null,
                null);
            this.projectRepository = new ProjectRepository(
                this.hkcu.CreateSubKey(TestKeyPath));
            this.projectExplorerSettings = new ProjectExplorerSettings(
                this.settingsRepository, false);

            this.resourceManagerAdapterMock = new Mock<IResourceManagerClient>();
            this.resourceManagerAdapterMock.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = SampleProjectId,
                    Name = $"[{SampleProjectId}]"
                });

            this.computeClientMock = new Mock<IComputeEngineClient>();
            this.computeClientMock.Setup(a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    SampleWindowsInstanceInZone1,
                    SampleLinuxInstanceInZone1
                });

            this.cloudConsoleServiceMock = new Mock<ICloudConsoleClient>();
            this.eventServiceMock = new Mock<IEventQueue>();
            this.sessionBrokerMock = new Mock<IGlobalSessionBroker>();
        }

        private ProjectExplorerViewModel CreateViewModel()
        {
            var workspace = new ProjectWorkspace(
                this.computeClientMock.Object,
                this.resourceManagerAdapterMock.Object,
                this.projectRepository,
                new Mock<IEventQueue>().Object);

            return new ProjectExplorerViewModel(
                this.projectExplorerSettings,
                new SynchrounousJobService(),
                this.eventServiceMock.Object,
                this.sessionBrokerMock.Object,
                workspace,
                this.cloudConsoleServiceMock.Object)
            {
                View = new Control()
            };
        }

        private class SynchrounousJobService : IJobService
        {
            public Task<T> RunAsync<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
                => jobFunc(CancellationToken.None);
        }

        private async Task<ObservableCollection<ProjectExplorerViewModel.ViewModelNode>> GetInstancesAsync(
            ProjectExplorerViewModel viewModel)
        {
            var projects = await viewModel.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false);

            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false);

            return await zones[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false);
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
        public async Task WhenOsFilterChanged_ThenViewModelIsUpdated()
        {
            var viewModel = CreateViewModel();
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
            this.computeClientMock.Verify(
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
                },
                new Mock<IBindingContext>().Object);

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
                },
                new Mock<IBindingContext>().Object);

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
                },
                new Mock<IBindingContext>().Object);

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
            this.computeClientMock.Verify(
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
                },
                new Mock<IBindingContext>().Object);

            initialViewModel.InstanceFilter = "test";
            Assert.AreEqual(1, eventCount);
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoProjectsAdded_ThenProjectsIsEmpty()
        {
            var viewModel = CreateViewModel();

            CollectionAssert.IsEmpty(viewModel.Projects);
        }

        [Test]
        public async Task WhenProjectAdded_ThenProjectsIsNotEmpty()
        {
            var viewModel = CreateViewModel();

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(viewModel.Projects);
        }

        //---------------------------------------------------------------------
        // Add/RemoveProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectAdded_ThenViewModelIsUpdated()
        {
            var viewModel = CreateViewModel();
            var initialProjectsList = await viewModel
                .ExpandRootAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(initialProjectsList.Any());

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var updatedProjectsList = await viewModel.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false);

            Assert.AreEqual(1, updatedProjectsList.Count);
        }

        [Test]
        public async Task WhenProjectRemoved_ThenViewModelIsUpdated()
        {
            this.projectRepository.AddProject(new ProjectLocator(SampleProjectId));

            var viewModel = CreateViewModel();
            var initialProjectsList = await viewModel
                .ExpandRootAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, initialProjectsList.Count());

            await viewModel
                .RemoveProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var updatedProjectsList = await viewModel.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false);

            Assert.IsFalse(updatedProjectsList.Any());
        }

        //---------------------------------------------------------------------
        // Project ordering.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenMultipleProjectsAdded_ThenProjectsAreOrderedByDisplayName()
        {
            this.resourceManagerAdapterMock.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "project-2"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "project-2",
                    Name = "project-2"  // Same as id
                });
            this.resourceManagerAdapterMock.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "inaccessible-1"),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("inaccessible", null));

            this.projectRepository.AddProject(new ProjectLocator("project-2"));
            this.projectRepository.AddProject(new ProjectLocator("inaccessible-1"));
            this.projectRepository.AddProject(new ProjectLocator(SampleProjectId));

            var viewModel = CreateViewModel();

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
        public async Task WhenReloadProjectsIsTrue_ThenRefreshReloadsProjects()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task WhenReloadProjectsIsFalse_ThenRefreshReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsNull_ThenRefreshSelectedNodeAsyncReloadsProjects()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsRoot_ThenRefreshSelectedNodeAsyncReloadsProjects()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenRefreshSelectedNodeAsyncReloadsZones()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            var nofifications = 0;
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var instances = await zones[0]
                .GetFilteredNodesAsync(false)
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
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
            Assert.AreEqual(1, (await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true)).Count);
        }

        //---------------------------------------------------------------------
        // IsConnected tracking.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceConnected_ThenIsConnectedIsTrue()
        {
            this.sessionBrokerMock.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(true);

            var viewModel = CreateViewModel();
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
        public async Task WhenSessionEventFired_ThenIsConnectedIsUpdated()
        {
            // Capture event handlers that the view model will register.
            Func<SessionStartedEvent, Task> sessionStartedEventHandler = null;
            this.eventServiceMock.Setup(e => e.Subscribe(
                    It.IsAny<Func<SessionStartedEvent, Task>>()))
                .Callback<Func<SessionStartedEvent, Task>>(e => sessionStartedEventHandler = e);

            Func<SessionEndedEvent, Task> sessionEndedEventHandler = null;
            this.eventServiceMock.Setup(e => e.Subscribe(
                    It.IsAny<Func<SessionEndedEvent, Task>>()))
                .Callback<Func<SessionEndedEvent, Task>>(e => sessionEndedEventHandler = e);

            var viewModel = CreateViewModel();
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

            await sessionStartedEventHandler(
                    new SessionStartedEvent((InstanceLocator)instances[0].Locator))
                .ConfigureAwait(true);

            Assert.IsTrue(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);

            await sessionEndedEventHandler(
                    new SessionEndedEvent((InstanceLocator)instances[0].Locator))
                .ConfigureAwait(true);

            Assert.IsFalse(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);

            await sessionStartedEventHandler(
                    new SessionStartedEvent(new InstanceLocator(SampleProjectId, "zone-1", "unknown-1")))
                .ConfigureAwait(true);

            Assert.IsFalse(instances[0].IsConnected);
            Assert.IsFalse(instances[1].IsConnected);
        }

        //---------------------------------------------------------------------
        // IsConnected tracking.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceConnected_ThenNodeUsesRightIcon()
        {
            this.sessionBrokerMock.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(true);

            var viewModel = CreateViewModel();
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
        public async Task WhenInstanceDisonnected_ThenNodeUsesRightIcon()
        {
            this.sessionBrokerMock.Setup(b => b.IsConnected(
                    It.IsAny<InstanceLocator>()))
                .Returns(false);

            var viewModel = CreateViewModel();
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
        public async Task WhenInstanceStateChanges_ThenProjectIsReloaded()
        {
            Func<InstanceStateChangedEvent, Task> eventHandler = null;
            this.eventServiceMock.Setup(e => e.Subscribe(
                    It.IsAny<Func<InstanceStateChangedEvent, Task>>()))
                .Callback<Func<InstanceStateChangedEvent, Task>>(e => eventHandler = e);

            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            Assert.IsNotNull(eventHandler);

            await eventHandler(new InstanceStateChangedEvent(
                new InstanceLocator(
                    SampleProjectId,
                    SampleLinuxInstanceInZone1.Zone,
                    SampleLinuxInstanceInZone1.Name),
                true));

            // Event must not cause reload.
            this.computeClientMock.Verify(
                a => a.ListInstancesAsync(
                    It.Is<string>(p => p == SampleProjectId),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // UnloadSelectedProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenUnloadSelectedProjectAsyncUnloadsProject()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];

            Assert.AreEqual(1, projects.Count);
            await viewModel
                .UnloadSelectedProjectAsync()
                .ConfigureAwait(true);
            Assert.AreEqual(0, projects.Count);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenUnloadSelectedProjectAsyncDoesNothing()
        {
            var viewModel = CreateViewModel();
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
        public async Task WhenSelectedNodeIsProject_ThenOpenInCloudConsoleOpensInstancesList()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];
            viewModel.OpenInCloudConsole();

            this.cloudConsoleServiceMock.Verify(c => c.OpenInstanceList(
                It.Is<ProjectLocator>(l => l.Name == SampleProjectId)),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenUnloadSelectedProjectOpensInstancesList()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

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
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(true);

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
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];
            viewModel.ConfigureIapAccess();

            this.cloudConsoleServiceMock.Verify(c => c.OpenIapSecurity(
                It.Is<string>(id => id == SampleProjectId)),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenConfigureIapAccessOpensProjectConfig()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = zones[0];
            viewModel.ConfigureIapAccess();

            this.cloudConsoleServiceMock.Verify(c => c.OpenIapSecurity(
                It.Is<string>(id => id == SampleProjectId)),
                Times.Once);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenConfigureIapAccessOpensProjectConfig()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var instances = await GetInstancesAsync(viewModel)
                .ConfigureAwait(true);

            viewModel.SelectedNode = instances[0];
            viewModel.ConfigureIapAccess();

            this.cloudConsoleServiceMock.Verify(c => c.OpenIapSecurity(
                It.Is<string>(id => id == SampleProjectId)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // Command visibility.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSelectedNodeIsRoot_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel();
            viewModel.SelectedNode = viewModel.RootNode;

            Assert.IsFalse(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsCloudConsoleCommandVisible);
        }

        [Test]
        public async Task WhenSelectedNodeIsProject_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = projects[0];

            Assert.IsTrue(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsCloudConsoleCommandVisible);
        }

        [Test]
        public async Task WhenSelectedNodeIsZone_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel();
            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);
            var projects = await viewModel
                .RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);
            var zones = await projects[0]
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true);

            viewModel.SelectedNode = zones[0];

            Assert.IsFalse(viewModel.IsUnloadProjectCommandVisible);
            Assert.IsTrue(viewModel.IsRefreshProjectsCommandVisible);
            Assert.IsFalse(viewModel.IsRefreshAllProjectsCommandVisible);
            Assert.IsTrue(viewModel.IsCloudConsoleCommandVisible);
        }

        [Test]
        public async Task WhenSelectedNodeIsInstance_ThenCommandVisiblityIsUpdated()
        {
            var viewModel = CreateViewModel();
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
        public async Task WhenProjectAddedAndNotSavedAsCollapsed_ThenProjectIsExpanded()
        {
            var viewModel = CreateViewModel();

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var projectViewModelNodes = (await viewModel.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false))
                .Cast<ProjectExplorerViewModel.ProjectViewModelNode>()
                .ToList();

            Assert.AreEqual(1, projectViewModelNodes.Count);
            Assert.IsTrue(projectViewModelNodes.First().IsExpanded);
        }

        [Test]
        public async Task WhenProjectAddedAndSavedAsCollapsed_ThenProjectIsCollapsed()
        {
            this.projectExplorerSettings.CollapsedProjects.Add(new ProjectLocator(SampleProjectId));
            var viewModel = CreateViewModel();

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var projectViewModelNodes = (await viewModel.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false))
                .Cast<ProjectExplorerViewModel.ProjectViewModelNode>()
                .ToList();

            Assert.AreEqual(1, projectViewModelNodes.Count);
            Assert.IsFalse(projectViewModelNodes.First().IsExpanded);
        }

        [Test]
        public async Task WhenProjectRemoved_ThenProjectIsRemovedFromSettings()
        {
            this.projectExplorerSettings.CollapsedProjects.Add(new ProjectLocator(SampleProjectId));
            var viewModel = CreateViewModel();

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            await viewModel
                .RemoveProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            CollectionAssert.DoesNotContain(
                this.projectExplorerSettings.CollapsedProjects,
                new ProjectLocator(SampleProjectId));
        }

        [Test]
        public async Task WhenProjectExpandedOrCollapsed_ThenSettingsAreUpdated()
        {
            var viewModel = CreateViewModel();

            await viewModel
                .AddProjectsAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(false);

            var projectViewModelNodes = (await viewModel.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(false))
                .Cast<ProjectExplorerViewModel.ProjectViewModelNode>()
                .ToList();

            Assert.AreEqual(1, projectViewModelNodes.Count);
            var project = projectViewModelNodes.First();

            project.IsExpanded = false;
            project.IsExpanded = false;
            project.IsExpanded = false;

            CollectionAssert.Contains(
                this.projectExplorerSettings.CollapsedProjects,
                project.ProjectNode.Project);

            project.IsExpanded = true;
            project.IsExpanded = true;
            project.IsExpanded = true;

            CollectionAssert.DoesNotContain(
                this.projectExplorerSettings.CollapsedProjects,
                project.ProjectNode.Project);
        }
    }
}
