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
    public class TestProjectModelService
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

        private const string SampleProjectId = "project-1";

        private static Mock<IResourceManagerClient> CreateResourceManagerClientMock()
        {
            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = SampleProjectId,
                    Name = $"[{SampleProjectId}]"
                });
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id != SampleProjectId),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("inaccessible", new Exception()));
            return resourceManagerAdapter;
        }

        private static Mock<IComputeEngineClient> CreateComputeEngineClientMock(
            string projectId,
            params Instance[] instances)
        {
            var computeAdapter = new Mock<IComputeEngineClient>();
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == projectId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(instances);

            return computeAdapter;
        }

        private Mock<IProjectRepository> CreateProjectRepositoryMock(
            params string[] addedProjectIds)
        {
            var projectRepository = new Mock<IProjectRepository>();
            projectRepository.Setup(r => r.ListProjectsAsync())
                .ReturnsAsync(addedProjectIds.Select(id => new ProjectLocator(id)));

            return projectRepository;
        }


        //---------------------------------------------------------------------
        // AddProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectAdded_ThenProjectIsAddedToRepositoryAndEventIsRaised()
        {
            var projectRepository = new Mock<IProjectRepository>();
            var eventService = new Mock<IEventQueue>();

            var workspace = new ProjectWorkspace(
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                projectRepository.Object,
                eventService.Object);

            await workspace
                .AddProjectAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            projectRepository.Verify(p => p.AddProject(
                    It.Is<ProjectLocator>(id => id.Name == SampleProjectId)),
                Times.Once);
            eventService.Verify(s => s.PublishAsync<ProjectAddedEvent>(
                    It.Is<ProjectAddedEvent>(e => e.ProjectId == SampleProjectId)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // RemoveProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectRemoved_ThenProjectIsRemovedFromRepositoryAndEventIsRaised()
        {
            var projectRepository = new Mock<IProjectRepository>();
            var eventService = new Mock<IEventQueue>();

            var workspace = new ProjectWorkspace(
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                projectRepository.Object,
                eventService.Object);

            await workspace
                .RemoveProjectAsync(new ProjectLocator(SampleProjectId))
                .ConfigureAwait(true);

            projectRepository.Verify(p => p.RemoveProject(
                    It.Is<ProjectLocator>(id => id.Name == SampleProjectId)),
                Times.Once);
            eventService.Verify(s => s.PublishAsync<ProjectDeletedEvent>(
                    It.Is<ProjectDeletedEvent>(e => e.ProjectId == SampleProjectId)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // GetRootNodeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectsNotCached_ThenGetRootNodeLoadsProjects()
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
            Assert.AreEqual(SampleProjectId, model.Projects.First().Project.Name);
            Assert.AreEqual("[project-1]", model.Projects.First().DisplayName);

            resourceManagerClient.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            computeClient.Verify(a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenSomeProjectsInaccessible_ThenGetRootNodeLoadsOtherProjects()
        {
            var computeAdapter = new Mock<IComputeEngineClient>();
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == "accessible-project"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Instance[0]);
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == "inaccessible-project"),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("test", new Exception()));

            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "accessible-project"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Google.Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = "accessible-project",
                    Name = "accessible-project"
                });
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "inaccessible-project"),
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
                    "accessible-project",
                    "inaccessible-project").Object,
                new Mock<IEventQueue>().Object);

            var model = await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            CollectionAssert.AreEquivalent(
                new[] { "accessible-project", "inaccessible-project" },
                model.Projects.Select(p => p.Project.Name).ToList());

            // Only 1 or 2 projects should have been cached.
            Assert.AreEqual(1, workspace.CachedProjectsCount);
        }

        [Test]
        public async Task WhenProjectsCached_ThenGetRootNodeAsyncReturnsCachedProjects()
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
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            computeClient.Verify(a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenProjectsCachedButForceRefreshIsTrue_ThenGetRootNodeAsyncLoadsProjects()
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
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            computeClient.Verify(a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task WhenProjectInaccessible_ThenGetRootNodeAsyncLoadsRemainingProjects()
        {
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(
                    SampleProjectId,
                    "nonexisting-1").Object,
                new Mock<IEventQueue>().Object);

            var projects = (await workspace
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true))
                    .Projects
                    .ToList();

            Assert.AreEqual(2, projects.Count);

            Assert.AreEqual(SampleProjectId, projects[0].Project.Name);
            Assert.AreEqual("nonexisting-1", projects[1].Project.Name);
        }

        [Test]
        public void WhenLoadingDataCausesReauthError_ThenGetRootNodeAsyncPropagatesException()
        {
            var resourceManagerAdapter = new Mock<IResourceManagerClient>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
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
        // GetZoneNodesAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenZonesNotCached_ThenGetZoneNodesAsyncLoadsZones()
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
                    new ProjectLocator(SampleProjectId),
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
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenZonesCached_ThenGetZoneNodesAsyncReturnsCachedZones()
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
                    new ProjectLocator(SampleProjectId),
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);
            var zonesSecondLoad = await workspace
                .GetZoneNodesAsync(
                    new ProjectLocator(SampleProjectId),
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreSame(zones, zonesSecondLoad);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenZonesCachedButForceRefreshIsTrue_ThenGetZoneNodesAsyncLoadsZones()
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
                    new ProjectLocator(SampleProjectId),
                    true,
                    CancellationToken.None)
                .ConfigureAwait(true);
            var zonesSecondLoad = await workspace.GetZoneNodesAsync(
                    new ProjectLocator(SampleProjectId),
                    true,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreNotSame(zones, zonesSecondLoad);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task WhenInstanceHasNoDisk_ThenGetZoneNodesAsyncSkipsInstance()
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
                    new ProjectLocator(SampleProjectId),
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
        public async Task WhenInstanceIsTerminated_ThenGetZoneNodesAsyncMarksInstanceAsNotRunning()
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
                    new ProjectLocator(SampleProjectId),
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
        // GetNodeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLocatorOfUnknownType_ThenGetNodeAsyncThrowsArgumentException()
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
        public async Task WhenProjectLocatorValid_ThenGetNodeAsyncReturnsNode()
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
                    new ProjectLocator(SampleProjectId),
                    CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsInstanceOf(typeof(IProjectModelProjectNode), project);
            Assert.IsNotNull(project);
        }

        [Test]
        public async Task WhenZoneLocatorValid_ThenGetNodeAsyncReturnsNode()
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
        public async Task WhenInstanceLocatorValid_ThenGetNodeAsyncReturnsNode()
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
        public async Task WhenProjectNotAddedButZoneLocatorValid_ThenGetNodeAsyncReturnsNull()
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
        public async Task WhenProjectNotAddedButInstanceLocatorValid_ThenGetNodeAsyncReturnsNull()
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
        // GetActiveNodeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoActiveNodeSet_ThenGetActiveNodeAsyncReturnsRoot()
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

        [Test]
        public async Task WhenActiveNodeSet_ThenGetActiveNodeAsyncReturnsNode()
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
                    new ProjectLocator(SampleProjectId),
                    CancellationToken.None)
                .ConfigureAwait(true);

            var activeNode = await workspace
                .GetActiveNodeAsync(CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsNotNull(activeNode);
            Assert.IsInstanceOf(typeof(IProjectModelProjectNode), activeNode);
        }

        [Test]
        public async Task WhenActiveNodeSetToValidLocator_ThenEventIsFired()
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
        public async Task WhenActiveNodeSetToNull_ThenEventIsFired()
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
                .SetActiveNodeAsync((ResourceLocator?)null, CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.PublishAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode is IProjectModelCloudNode)),
                Times.Once);
        }

        [Test]
        public async Task WhenActiveNodeSetToNonexistingLocator_ThenEventIsFired()
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
        public async Task WhenCacheEmptyAndActiveNodeSetToNull_ThenNoEventIsFired()
        {
            var eventService = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                CreateComputeEngineClientMock(SampleProjectId).Object,
                CreateResourceManagerClientMock().Object,
                CreateProjectRepositoryMock(SampleProjectId).Object,
                eventService.Object);

            await workspace
                .SetActiveNodeAsync((ResourceLocator?)null, CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.PublishAsync<ActiveProjectChangedEvent>(
                    It.IsAny<ActiveProjectChangedEvent>()),
                Times.Never);
        }

        [Test]
        public void WhenLocatorIsInvalid_ThenSetActiveNodeAsyncRaisesArgumentException()
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
