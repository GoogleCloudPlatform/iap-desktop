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
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.ProjectModel
{
    [TestFixture]
    public class TestProjectModelService : ApplicationFixtureBase
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

        private static Mock<IResourceManagerAdapter> CreateResourceManagerAdapterMock(
)
        {
            var resourceManagerAdapter = new Mock<IResourceManagerAdapter>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.CloudResourceManager.v1.Data.Project()
                {
                    ProjectId = SampleProjectId,
                    Name = $"[{SampleProjectId}]"
                });
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id != SampleProjectId),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("inaccessible", null));
            return resourceManagerAdapter;
        }

        private static Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
            string projectId,
            params Instance[] instances)
        {
            var computeAdapter = new Mock<IComputeEngineAdapter>();
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
            var serviceRegistry = new ServiceRegistry();
            var projectRepository = serviceRegistry.AddMock<IProjectRepository>();
            var eventService = serviceRegistry.AddMock<IEventService>();

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.AddProjectAsync(new ProjectLocator(SampleProjectId));

            projectRepository.Verify(p => p.AddProject(
                    It.Is<ProjectLocator>(id => id.Name == SampleProjectId)),
                Times.Once);
            eventService.Verify(s => s.FireAsync<ProjectAddedEvent>(
                    It.Is<ProjectAddedEvent>(e => e.ProjectId == SampleProjectId)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // RemoveProjectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectRemoved_ThenProjectIsRemovedFromRepositoryAndEventIsRaised()
        {
            var serviceRegistry = new ServiceRegistry();
            var projectRepository = serviceRegistry.AddMock<IProjectRepository>();
            var eventService = serviceRegistry.AddMock<IEventService>();

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.RemoveProjectAsync(new ProjectLocator(SampleProjectId));

            projectRepository.Verify(p => p.RemoveProject(
                    It.Is<ProjectLocator>(id => id.Name == SampleProjectId)),
                Times.Once);
            eventService.Verify(s => s.FireAsync<ProjectDeletedEvent>(
                    It.Is<ProjectDeletedEvent>(e => e.ProjectId == SampleProjectId)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // GetRootNodeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectsNotCached_ThenGetRootNodeLoadsProjects()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            
            var resourceManagerAdapter = CreateResourceManagerAdapterMock();
            serviceRegistry.AddSingleton(resourceManagerAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetRootNodeAsync(false, CancellationToken.None);

            Assert.AreEqual(1, model.Projects.Count());
            Assert.AreEqual(SampleProjectId, model.Projects.First().Project.Name);
            Assert.AreEqual("[project-1]", model.Projects.First().DisplayName);

            resourceManagerAdapter.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Test]
        public async Task WhenProjectsCached_ThenGetRootNodeAsyncReturnsCachedProjects()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);

            var resourceManagerAdapter = CreateResourceManagerAdapterMock();
            serviceRegistry.AddSingleton(resourceManagerAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetRootNodeAsync(false, CancellationToken.None);
            var modelSecondLoad = await modelService.GetRootNodeAsync(false, CancellationToken.None);

            Assert.AreSame(model, modelSecondLoad);

            resourceManagerAdapter.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenProjectsCachedButForceRefreshIsTrue_ThenGetRootNodeAsyncLoadsProjects()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);

            var resourceManagerAdapter = CreateResourceManagerAdapterMock();
            serviceRegistry.AddSingleton(resourceManagerAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetRootNodeAsync(false, CancellationToken.None);
            var modelSecondLoad = await modelService.GetRootNodeAsync(true, CancellationToken.None);

            Assert.AreNotSame(model, modelSecondLoad);

            resourceManagerAdapter.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task WhenProjectInaccessible_ThenGetRootNodeAsyncLoadsRemainingProjects()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(
                SampleProjectId, 
                "nonexisting-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var projects = (await modelService.GetRootNodeAsync(false, CancellationToken.None))
                .Projects
                .ToList();

            Assert.AreEqual(2, projects.Count);

            Assert.AreEqual(SampleProjectId, projects[0].Project.Name);
            Assert.AreEqual("nonexisting-1", projects[1].Project.Name);
        }

        [Test]
        public void WhenLoadingDataCausesReauthError_ThenGetRootNodeAsyncPropagatesException()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            var resourceManagerAdapter = serviceRegistry.AddMock<IResourceManagerAdapter>();
            resourceManagerAdapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            serviceRegistry.AddSingleton(resourceManagerAdapter.Object);
            

            var modelService = new ProjectModelService(serviceRegistry);

            AssertEx.ThrowsAggregateException<TokenResponseException>(
                () => modelService.GetRootNodeAsync(false, CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // GetZoneNodesAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenZonesNotCached_ThenGetZoneNodesAsyncLoadsZones()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);

            var computeAdapter = CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceInZone2);
            serviceRegistry.AddSingleton(computeAdapter.Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var zones = await modelService.GetZoneNodesAsync(
                new ProjectLocator(SampleProjectId),
                false,
                CancellationToken.None);

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
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);

            var computeAdapter = CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceInZone2);
            serviceRegistry.AddSingleton(computeAdapter.Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var zones = await modelService.GetZoneNodesAsync(
                new ProjectLocator(SampleProjectId),
                false,
                CancellationToken.None);
            var zonesSecondLoad = await modelService.GetZoneNodesAsync(
                new ProjectLocator(SampleProjectId),
                false,
                CancellationToken.None);

            Assert.AreSame(zones, zonesSecondLoad);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == SampleProjectId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenZonesCachedButForceRefreshIsTrue_ThenGetZoneNodesAsyncLoadsZones()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);

            var computeAdapter = CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1,
                SampleLinuxInstanceInZone2);
            serviceRegistry.AddSingleton(computeAdapter.Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var zones = await modelService.GetZoneNodesAsync(
                    new ProjectLocator(SampleProjectId),
                    true,
                    CancellationToken.None)
                .ConfigureAwait(true);
            var zonesSecondLoad = await modelService.GetZoneNodesAsync(
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
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);

            var computeAdapter = CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleLinuxInstanceInZone1, 
                SampleLinuxInstanceWithoutDiskInZone1);
            serviceRegistry.AddSingleton(computeAdapter.Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var zones = await modelService.GetZoneNodesAsync(
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
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);

            var computeAdapter = CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleTerminatedLinuxInstanceInZone1,
                SampleLinuxInstanceInZone1);
            serviceRegistry.AddSingleton(computeAdapter.Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var zones = await modelService.GetZoneNodesAsync(
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
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            AssertEx.ThrowsAggregateException<ArgumentException>(() => modelService.GetNodeAsync(
                new DiskTypeLocator(SampleProjectId, "zone-1", "type-1"),
                CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectLocatorValid_ThenGetNodeAsyncReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsNull(await modelService.GetNodeAsync(
                    new ProjectLocator("nonexisting-1"),
                    CancellationToken.None)
                .ConfigureAwait(true));

            var project = await modelService.GetNodeAsync(
                    new ProjectLocator(SampleProjectId),
                    CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsInstanceOf(typeof(IProjectModelProjectNode), project);
            Assert.IsNotNull(project);
        }

        [Test]
        public async Task WhenZoneLocatorValid_ThenGetNodeAsyncReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleWindowsInstanceInZone1).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            Assert.IsNull(await modelService
                .GetNodeAsync(
                    new ZoneLocator("nonexisting-1", "zone-1"),
                    CancellationToken.None)
                .ConfigureAwait(true));

            var zone = await modelService
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
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleWindowsInstanceInZone1).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            Assert.IsNull(await modelService
                .GetNodeAsync(
                    new InstanceLocator("nonexisting-1", "zone-1", SampleWindowsInstanceInZone1.Name),
                    CancellationToken.None)
                .ConfigureAwait(true));

            var instance = await modelService
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
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock().Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleWindowsInstanceInZone1).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            Assert.IsNull(await modelService
                .GetNodeAsync(
                    new ZoneLocator(SampleProjectId, "zone-1"),
                    CancellationToken.None)
                .ConfigureAwait(true));
        }

        [Test]
        public async Task WhenProjectNotAddedButInstanceLocatorValid_ThenGetNodeAsyncReturnsNull()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock().Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleWindowsInstanceInZone1).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            Assert.IsNull(await modelService
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
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var activeNode = await modelService
                .GetActiveNodeAsync(CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsNotNull(activeNode);
            Assert.AreSame(
                await modelService
                    .GetRootNodeAsync(false, CancellationToken.None)
                    .ConfigureAwait(true),
                activeNode);
        }

        [Test]
        public async Task WhenActiveNodeSet_ThenGetActiveNodeAsyncReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(
                SampleProjectId,
                SampleWindowsInstanceInZone1).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            await modelService
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await modelService
                .SetActiveNodeAsync(
                    new ProjectLocator(SampleProjectId),
                    CancellationToken.None)
                .ConfigureAwait(true);

            var activeNode = await modelService
                .GetActiveNodeAsync(CancellationToken.None)
                .ConfigureAwait(true);
            Assert.IsNotNull(activeNode);
            Assert.IsInstanceOf(typeof(IProjectModelProjectNode), activeNode);
        }

        [Test]
        public async Task WhenActiveNodeSetToValidLocator_ThenEventIsFired()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetRootNodeAsync(false, CancellationToken.None);

            var root = await modelService
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await modelService
                .SetActiveNodeAsync(
                    root.Projects.First(),
                    CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.FireAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode == root.Projects.First())),
                Times.Once);
        }

        [Test]
        public async Task WhenActiveNodeSetToNull_ThenEventIsFired()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            // Pre-warm cache.
            await modelService
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await modelService
                .SetActiveNodeAsync((ResourceLocator)null, CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.FireAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode is IProjectModelCloudNode)),
                Times.Once);
        }

        [Test]
        public async Task WhenActiveNodeSetToNonexistingLocator_ThenEventIsFired()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService
                .GetRootNodeAsync(false, CancellationToken.None)
                .ConfigureAwait(true);

            // Pre-warm cache.
            await modelService
                .GetRootNodeAsync(
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            await modelService
                .SetActiveNodeAsync(
                    new ProjectLocator("nonexisting-1"), 
                    CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.FireAsync<ActiveProjectChangedEvent>(
                    It.Is<ActiveProjectChangedEvent>(e => e.ActiveNode is IProjectModelCloudNode)),
                Times.Once);
        }

        [Test]
        public async Task WhenCacheEmptyAndActiveNodeSetToNull_ThenNoEventIsFired()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            await modelService
                .SetActiveNodeAsync((ResourceLocator)null, CancellationToken.None)
                .ConfigureAwait(true);

            eventService.Verify(s => s.FireAsync<ActiveProjectChangedEvent>(
                    It.IsAny<ActiveProjectChangedEvent>()),
                Times.Never);
        }

        [Test]
        public void WhenLocatorIsInvalid_ThenSetActiveNodeAsyncRaisesArgumentException()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock(SampleProjectId).Object);
            serviceRegistry.AddSingleton(CreateResourceManagerAdapterMock().Object);

            var modelService = new ProjectModelService(serviceRegistry);

            AssertEx.ThrowsAggregateException<ArgumentException>(
                () => modelService.SetActiveNodeAsync(
                    new DiskTypeLocator(SampleProjectId, "zone-1", "type-1"),
                    CancellationToken.None).Wait());
        }
    }
}
