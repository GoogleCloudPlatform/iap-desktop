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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ProjectModel
{
    [TestFixture]
    public class TestProjectWorkspace
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

        private static readonly Instance SampleLinuxInstanceInZone2 = new Instance()
        {
            Id = 3u,
            Name = "linux-zone-2",
            Disks = new[]
            {
                new AttachedDisk()
                {
                }
            },
            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-2",
            Status = "RUNNING"
        };

        private static readonly Instance SampleLinuxInstanceWithoutDiskInZone1 = new Instance()
        {
            Id = 4u,
            Name = "linux-nodisk-zone-1",
            Disks = Array.Empty<AttachedDisk>(),
            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1"
        };

        private static readonly Instance SampleTerminatedLinuxInstanceInZone1 = new Instance()
        {
            Id = 5u,
            Name = "linux-terminated-zone-1",
            Disks = new[]
            {
                new AttachedDisk()
                {
                }
            },
            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1",
            Status = "TERMINATED"
        };

        private static readonly ProjectLocator SampleProjectId = new ProjectLocator("project-1");

        private static Mock<IResourceManagerClient> CreateResourceManagerClientMock()
        {
            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter
                .Setup(a => a.GetProjectAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = SampleProjectId.Name,
                    Name = $"[{SampleProjectId.Name}]"
                });
            resourceManagerAdapter
                .Setup(a => a.GetProjectAsync(
                    It.Is<ProjectLocator>(id => id != SampleProjectId),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("inaccessible", new Exception()));
            return resourceManagerAdapter;
        }

        private static Mock<IComputeEngineClient> CreateComputeEngineClientMock(
            ProjectLocator projectId,
            params Instance[] instances)
        {
            var computeAdapter = new Mock<IComputeEngineClient>();
            computeAdapter
                .Setup(a => a.ListInstancesAsync(
                    projectId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(instances);

            return computeAdapter;
        }

        private static Mock<IProjectRepository> CreateProjectRepositoryMock(
            params ProjectLocator[] addedProjectIds)
        {
            var projectRepository = new Mock<IProjectRepository>();
            projectRepository
                .Setup(r => r.ListProjectsAsync())
                .ReturnsAsync(addedProjectIds);

            return projectRepository;
        }


        //---------------------------------------------------------------------
        // AddProject
        //---------------------------------------------------------------------

        [Test]
        public async Task AddProject()
        {
            var projectRepository = new Mock<IProjectRepository>();
            var eventService = new Mock<IEventQueue>();

            var workspace = new ProjectWorkspace(
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                projectRepository.Object,
                eventService.Object);

            await workspace
                .AddProjectAsync(SampleProjectId)
                .ConfigureAwait(true);

            projectRepository.Verify(p => p.AddProject(SampleProjectId), Times.Once);
            eventService.Verify(s => s.PublishAsync<ProjectAddedEvent>(
                    It.Is<ProjectAddedEvent>(e => e.ProjectId == SampleProjectId.Name)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // RemoveProject.
        //---------------------------------------------------------------------

        [Test]
        public async Task RemoveProject()
        {
            var projectRepository = new Mock<IProjectRepository>();
            var eventService = new Mock<IEventQueue>();

            var workspace = new ProjectWorkspace(
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                projectRepository.Object,
                eventService.Object);

            await workspace
                .RemoveProjectAsync(SampleProjectId)
                .ConfigureAwait(true);

            projectRepository.Verify(p => p.RemoveProject(SampleProjectId), Times.Once);
            eventService.Verify(s => s.PublishAsync<ProjectDeletedEvent>(
                    It.Is<ProjectDeletedEvent>(e => e.ProjectId == SampleProjectId.Name)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // GetRootNode.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetRootNode_WhenProjectsNotCached_ThenGetRootNodeLoadsProjects()
        {
            var computeClient = CreateComputeEngineClientMock(SampleProjectId);
            var resourceManagerClient = CreateResourceManagerClientMock();

            var workspace = new ProjectWorkspace(
                computeClient.Object,
                resourceManagerClient.Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreEqual(1, model.Projects.Count());
            Assert.AreEqual(SampleProjectId, model.Projects.First().Project);
            Assert.AreEqual("[project-1]", model.Projects.First().DisplayName);

            resourceManagerClient.Verify(a => a.GetProjectAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
            computeClient.Verify(a => a.ListInstancesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetRootNode_WhenSomeProjectsInaccessible_ThenGetRootNodeLoadsOtherProjects()
        {
            var accessibleProject = new ProjectLocator("accessible-project");
            var inaccessibleProject = new ProjectLocator("inaccessible-project");

            var computeAdapter = new Mock<IComputeEngineClient>();
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    accessibleProject,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Instance>());
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    inaccessibleProject,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("test", new Exception()));

            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    accessibleProject,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "accessible-project",
                    Name = "accessible-project"
                });
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    inaccessibleProject,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "inaccessible-project",
                    Name = "inaccessible-project"
                });

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                resourceManagerAdapter.Object,
                CreateProjectRepositoryMock(
                    accessibleProject,
                    inaccessibleProject).Object,
                new Mock<IEventQueue>().Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            CollectionAssert.AreEquivalent(
                new[] { "accessible-project", "inaccessible-project" },
                model.Projects.Select(p => p.Project.Name).ToList());

            Assert.AreEqual(2, workspace.CachedProjectsCount);
        }

        [Test]
        public async Task GetRootNode_WhenSomeProjectsNotFound_ThenGetRootNodeLoadsOtherProjects()
        {
            var accessibleProject = new ProjectLocator("accessible-project");
            var nonexistingProject = new ProjectLocator("nonexisting-project");

            var computeAdapter = new Mock<IComputeEngineClient>();
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    accessibleProject,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Instance>());
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    nonexistingProject,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("test", new Exception()));

            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    accessibleProject,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "accessible-project",
                    Name = "accessible-project"
                });
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    nonexistingProject,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "nonexisting-project",
                    Name = "nonexisting-project"
                });

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                resourceManagerAdapter.Object,
                CreateProjectRepositoryMock(
                    accessibleProject,
                    nonexistingProject).Object,
                new Mock<IEventQueue>().Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            CollectionAssert.AreEquivalent(
                new[] { "accessible-project", "nonexisting-project" },
                model.Projects.Select(p => p.Project.Name).ToList());

            Assert.AreEqual(2, workspace.CachedProjectsCount);
        }

        [Test]
        public async Task GetRootNode_WhenProjectsCached_ThenGetRootNodeAsyncReturnsCachedProjects()
        {
            var computeClient = CreateComputeEngineClientMock(SampleProjectId);
            var resourceManagerAdapter = CreateResourceManagerClientMock();

            var workspace = new ProjectWorkspace(
                computeClient.Object,
                resourceManagerAdapter.Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);
            var modelSecondLoad = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreSame(model, modelSecondLoad);

            resourceManagerAdapter.Verify(a => a.GetProjectAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
            computeClient.Verify(a => a.ListInstancesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetRootNode_WhenProjectsCachedButForceRefreshIsTrue_ThenGetRootNodeAsyncLoadsProjects()
        {
            var computeClient = CreateComputeEngineClientMock(SampleProjectId);
            var resourceManagerAdapter = CreateResourceManagerClientMock();

            var workspace = new ProjectWorkspace(
                computeClient.Object,
                resourceManagerAdapter.Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);
            var modelSecondLoad = await workspace
                .GetRootNodeAsync(true, CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreNotSame(model, modelSecondLoad);

            resourceManagerAdapter.Verify(a => a.GetProjectAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            computeClient.Verify(a => a.ListInstancesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task GetRootNode_WhenProjectInaccessible_ThenGetRootNodeAsyncLoadsRemainingProjects()
        {
            var nonexistingProjectId = new ProjectLocator("nonexisting-1");
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(
                    SampleProjectId,
                    nonexistingProjectId).Object,
                new Mock<IEventQueue>().Object);

            var projects = (await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true))
                    .Projects
                    .ToList();

            Assert.AreEqual(2, projects.Count);

            Assert.AreEqual(SampleProjectId, projects[0].Project);
            Assert.AreEqual(nonexistingProjectId, projects[1].Project);
        }

        [Test]
        public void GetRootNode_WhenLoadingDataCausesReauthError_ThenGetRootNodeAsyncPropagatesException()
        {
            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                resourceManagerAdapter.Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                () => workspace.GetRootNodeAsync(false, CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // GetZoneNodes.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetZoneNodes_WhenZonesNotCached_ThenGetZoneNodesAsyncLoadsZones()
        {
            var computeAdapter = CreateComputeEngineClientMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceInZone2);

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var zones = await workspace
                .GetZoneNodesAsync(
                    SampleProjectId,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var zone1 = zones.First();
            var zone2 = zones.Last();

            Assert.AreEqual(new ZoneLocator(SampleProjectId, "zone-1"), zone1.Zone);
            Assert.AreEqual(new ZoneLocator(SampleProjectId, "zone-2"), zone2.Zone);

            Assert.AreEqual(1, zone1.Instances.Count());
            Assert.AreEqual(1, zone2.Instances.Count());

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetZoneNodes_WhenZonesCached_ThenGetZoneNodesAsyncReturnsCachedZones()
        {
            var computeAdapter = CreateComputeEngineClientMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceInZone2);

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var zones = await workspace
                .GetZoneNodesAsync(
                    SampleProjectId,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);
            var zonesSecondLoad = await workspace
                .GetZoneNodesAsync(
                    SampleProjectId,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreSame(zones, zonesSecondLoad);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetZoneNodes_WhenZonesCachedButForceRefreshIsTrue_ThenGetZoneNodesAsyncLoadsZones()
        {
            var computeAdapter = CreateComputeEngineClientMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceInZone2);

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var zones = await workspace.GetZoneNodesAsync(
                    SampleProjectId,
                    true,
                    CancellationToken.None)
                .ConfigureAwait(true);
            var zonesSecondLoad = await workspace.GetZoneNodesAsync(
                    SampleProjectId,
                    true,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreNotSame(zones, zonesSecondLoad);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    SampleProjectId,
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task GetZoneNodes_WhenInstanceHasNoDisk_ThenGetZoneNodesAsyncSkipsInstance()
        {
            var computeAdapter = CreateComputeEngineClientMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceWithoutDiskInZone1);

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var zones = await workspace.GetZoneNodesAsync(
                    SampleProjectId,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreEqual(1, zones.Count);
            var zone1 = zones.First();

            Assert.AreEqual(1, zone1.Instances.Count());
            Assert.AreEqual(
                SampleLinuxInstanceInZone1.Name,
                zone1.Instances.First().DisplayName);
        }

        [Test]
        public async Task GetZoneNodes_WhenInstanceIsTerminated_ThenGetZoneNodesAsyncMarksInstanceAsNotRunning()
        {
            var computeAdapter = CreateComputeEngineClientMock(
                SampleProjectId,
                SampleTerminatedLinuxInstanceInZone1,
                SampleLinuxInstanceInZone1);

            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var zones = await workspace.GetZoneNodesAsync(
                    SampleProjectId,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreEqual(1, zones.Count);
            var zone1 = zones.First();

            Assert.AreEqual(2, zone1.Instances.Count());

            var terminated = zone1.Instances.First();
            var running = zone1.Instances.Last();

            Assert.AreEqual(SampleTerminatedLinuxInstanceInZone1.Name, terminated.DisplayName);
            Assert.IsFalse(terminated.IsRunning);

            Assert.AreEqual(SampleLinuxInstanceInZone1.Name, running.DisplayName);
            Assert.IsTrue(running.IsRunning);
        }

        //---------------------------------------------------------------------
        // GetNode.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetNode_WhenLocatorOfUnknownType_ThenGetNodeAsyncThrowsArgumentException()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => workspace.GetNodeAsync(
                new DiskTypeLocator(SampleProjectId, "zone-1", "type-1"),
                CancellationToken.None).Wait());
        }

        [Test]
        public async Task GetNode_WhenProjectLocatorValid_ThenGetNodeAsyncReturnsNode()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsNull(await workspace.GetNodeAsync(
                    new ProjectLocator("nonexisting-1"),
                    CancellationToken.None)
                .ConfigureAwait(true));

            var project = await workspace.GetNodeAsync(
                    SampleProjectId,
                    CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsInstanceOf(typeof(IProjectModelProjectNode), project);
            Assert.IsNotNull(project);
        }

        [Test]
        public async Task GetNode_WhenZoneLocatorValid_ThenGetNodeAsyncReturnsNode()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(
                    SampleProjectId,
                    SampleWindowsInstanceInZone1).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            Assert.IsNull(await workspace
                .GetNodeAsync(
                    new ZoneLocator("nonexisting-1", "zone-1"),
                    CancellationToken.None)
                .ConfigureAwait(true));

            var zone = await workspace
                .GetNodeAsync(
                    new ZoneLocator(SampleProjectId, "zone-1"),
                    CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsInstanceOf(typeof(IProjectModelZoneNode), zone);
            Assert.IsNotNull(zone);
        }

        [Test]
        public async Task GetNode_WhenInstanceLocatorValid_ThenGetNodeAsyncReturnsNode()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(
                    SampleProjectId,
                    SampleWindowsInstanceInZone1).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            Assert.IsNull(await workspace
                .GetNodeAsync(
                    new InstanceLocator("nonexisting-1", "zone-1", SampleWindowsInstanceInZone1.Name),
                    CancellationToken.None)
                .ConfigureAwait(true));

            var instance = await workspace
                .GetNodeAsync(
                    new InstanceLocator(SampleProjectId, "zone-1", SampleWindowsInstanceInZone1.Name),
                    CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsInstanceOf(typeof(IProjectModelInstanceNode), instance);
            Assert.IsNotNull(instance);
        }

        [Test]
        public async Task GetNode_WhenProjectNotAddedButZoneLocatorValid_ThenGetNodeAsyncReturnsNull()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(
                    SampleProjectId,
                    SampleWindowsInstanceInZone1).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock().Object,
                new Mock<IEventQueue>().Object);

            Assert.IsNull(await workspace
                .GetNodeAsync(
                    new ZoneLocator(SampleProjectId, "zone-1"),
                    CancellationToken.None)
                .ConfigureAwait(true));
        }

        [Test]
        public async Task GetNode_WhenProjectNotAddedButInstanceLocatorValid_ThenGetNodeAsyncReturnsNull()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(
                    SampleProjectId,
                    SampleWindowsInstanceInZone1).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock().Object,
                new Mock<IEventQueue>().Object);

            Assert.IsNull(await workspace
                .GetNodeAsync(
                    new InstanceLocator(SampleProjectId, "zone-1", SampleWindowsInstanceInZone1.Name),
                    CancellationToken.None)
                .ConfigureAwait(true));
        }

        //---------------------------------------------------------------------
        // GetActiveNode.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetActiveNode_WhenNoActiveNodeSet_ThenGetActiveNodeAsyncReturnsRoot()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            var activeNode = await workspace
                .GetActiveNodeAsync(CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsNotNull(activeNode);
            Assert.AreSame(
                await workspace
                    .GetRootNodeAsync(false, CancellationToken.None)
                    .ConfigureAwait(true),
                activeNode);
        }

        //---------------------------------------------------------------------
        // SetActiveNode.
        //---------------------------------------------------------------------

        [Test]
        public async Task SetActiveNode_WhenActiveNodeSet_ThenGetActiveNodeAsyncReturnsNode()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(
                    SampleProjectId,
                    SampleWindowsInstanceInZone1).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            await workspace
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await workspace
                .SetActiveNodeAsync(
                    SampleProjectId,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var activeNode = await workspace
                .GetActiveNodeAsync(CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsNotNull(activeNode);
            Assert.IsInstanceOf(typeof(IProjectModelProjectNode), activeNode);
        }

        [Test]
        public async Task SetActiveNode_WhenActiveNodeSetToValidLocator_ThenEventIsFired()
        {
            var eventService = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                eventService.Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            var root = await workspace
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await workspace
                .SetActiveNodeAsync(
                    root.Projects.First(),
                    CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.PublishAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode == root.Projects.First())),
                Times.Once);
        }

        [Test]
        public async Task SetActiveNode_WhenActiveNodeSetToNull_ThenEventIsFired()
        {
            var eventService = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                eventService.Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            // Pre-warm cache.
            await workspace
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await workspace
                .SetActiveNodeAsync((ComputeEngineLocator?)null, CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.PublishAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode is IProjectModelCloudNode)),
                Times.Once);
        }

        [Test]
        public async Task SetActiveNode_WhenActiveNodeSetToNonexistingLocator_ThenEventIsFired()
        {
            var eventService = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                eventService.Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            // Pre-warm cache.
            await workspace
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await workspace
                .SetActiveNodeAsync(
                    new ProjectLocator("nonexisting-1"),
                    CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.PublishAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode is IProjectModelCloudNode)),
                Times.Once);
        }

        [Test]
        public async Task SetActiveNode_WhenCacheEmptyAndActiveNodeSetToNull_ThenNoEventIsFired()
        {
            var eventService = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                eventService.Object);

            await workspace
                .SetActiveNodeAsync((ComputeEngineLocator?)null, CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.PublishAsync<ActiveProjectChangedEvent>(
                    It.IsAny<ActiveProjectChangedEvent>()),
                Times.Never);
        }

        [Test]
        public void SetActiveNode_WhenLocatorIsInvalid_ThenSetActiveNodeAsyncRaisesArgumentException()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                new Mock<IEventQueue>().Object);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => workspace.SetActiveNodeAsync(
                    new DiskTypeLocator(SampleProjectId, "zone-1", "type-1"),
                    CancellationToken.None).Wait());
        }
    }
}
