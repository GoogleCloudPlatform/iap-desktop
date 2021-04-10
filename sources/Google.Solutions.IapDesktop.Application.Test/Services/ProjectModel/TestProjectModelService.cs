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
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
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
            await modelService.AddProjectAsync(new ProjectLocator("project-1"));

            projectRepository.Verify(p => p.AddProjectAsync(
                    It.Is<string>(id => id == "project-1")),
                Times.Once);
            eventService.Verify(s => s.FireAsync<ProjectAddedEvent>(
                    It.Is<ProjectAddedEvent>(e => e.ProjectId == "project-1")),
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
            await modelService.RemoveProjectAsync(new ProjectLocator("project-1"));

            projectRepository.Verify(p => p.DeleteProjectAsync(
                    It.Is<string>(id => id == "project-1")),
                Times.Once);
            eventService.Verify(s => s.FireAsync<ProjectDeletedEvent>(
                    It.Is<ProjectDeletedEvent>(e => e.ProjectId == "project-1")),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // GetModelAsync.
        //---------------------------------------------------------------------

        private static Mock<IComputeEngineAdapter> CreateComputeEngineAdapterMock(
            string projectId)
        {
            var computeAdapter = new Mock<IComputeEngineAdapter>();
            computeAdapter.Setup(a => a.GetProjectAsync(
                    It.Is<string>(id => id == projectId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.Compute.v1.Data.Project()
                {
                    Name = projectId,
                    Description = $"[{projectId}]"
                });
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == projectId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    SampleWindowsInstanceInZone1
                });

            return computeAdapter;
        }

        private Mock<IProjectRepository> CreateProjectRepositoryMock(
            params string[] addedProjectIds)
        {
            var projectRepository = new Mock<IProjectRepository>();
            projectRepository.Setup(r => r.ListProjectsAsync())
                .ReturnsAsync(addedProjectIds.Select(id => new Application.Services.Settings.Project(id)));

            return projectRepository;
        }

        [Test]
        public async Task WhenModelNotCached_ThenGetModelAsyncLoadsModel()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);

            var computeAdapter = CreateComputeEngineAdapterMock("project-1");
            serviceRegistry.AddSingleton(computeAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.AreEqual(1, model.Projects.Count());
            Assert.AreEqual(0, model.InaccessibleProjects.Count());
            Assert.AreEqual("project-1", model.Projects.First().Project.Name);
            Assert.AreEqual("[project-1]", model.Projects.First().DisplayName);

            computeAdapter.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "project-1"),
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == "project-1"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenModelCached_ThenGetModelAsyncReturnsCachedModel()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);

            var computeAdapter = CreateComputeEngineAdapterMock("project-1");
            serviceRegistry.AddSingleton(computeAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);
            var modelSecondLoad = await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.AreSame(model, modelSecondLoad);

            computeAdapter.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "project-1"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == "project-1"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenModelCachedButForceRefreshIsTrue_ThenGetModelAsyncLoadsModel()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);

            var computeAdapter = CreateComputeEngineAdapterMock("project-1");
            serviceRegistry.AddSingleton(computeAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);
            var modelSecondLoad = await modelService.GetModelAsync(true, CancellationToken.None);

            Assert.AreNotSame(model, modelSecondLoad);

            computeAdapter.Verify(a => a.GetProjectAsync(
                    It.Is<string>(id => id == "project-1"),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            computeAdapter.Verify(a => a.ListInstancesAsync(
                    It.Is<string>(id => id == "project-1"),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task WhenProjectInaccessible_ThenOtherProjectsAreLoaded()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock(
                "project-1", 
                "nonexisting-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.AreEqual(1, model.Projects.Count());
            Assert.AreEqual(1, model.InaccessibleProjects.Count());

            Assert.AreEqual("project-1", model.Projects.First().Project.Name);
            Assert.AreEqual("nonexisting-1", model.InaccessibleProjects.First().Name);
        }

        [Test]
        public void WhenLoadingDataCausesReauthError_ThenExceptionIsPropagated()
        {
            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);

            var computeAdapter = serviceRegistry.AddMock<IComputeEngineAdapter>();
            computeAdapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            serviceRegistry.AddSingleton(computeAdapter.Object);

            var modelService = new ProjectModelService(serviceRegistry);

            AssertEx.ThrowsAggregateException<TokenResponseException>(
                () => modelService.GetModelAsync(false, CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // TryFindNode.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLocatorOfUnknownType_ThenTryFindNodeThrowsArgumentException()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.Throws<ArgumentException>(() => modelService.TryFindNode(
                new DiskTypeLocator("project-1", "zone-1", "type-1")));
        }

        [Test]
        public void WhenModelNotCached_ThenTryFindNodeReturnsNull()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);

            Assert.IsNull(modelService.TryFindNode(new ProjectLocator("project-1")));
        }

        [Test]
        public async Task WhenModelCachedAndProjectLocatorValid_ThenTryFindNodeReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.IsNull(modelService.TryFindNode(new ProjectLocator("nonexisting-1")));

            var project = modelService.TryFindNode(new ProjectLocator("project-1"));
            Assert.IsInstanceOf(typeof(IProjectExplorerProjectNode), project);
            Assert.IsNotNull(project);
        }

        [Test]
        public async Task WhenModelCachedAndZoneLocatorValid_ThenTryFindNodeReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.IsNull(modelService.TryFindNode(new ZoneLocator("nonexisting-1", "zone-1")));

            var zone = modelService.TryFindNode(new ZoneLocator("project-1", "zone-1"));
            Assert.IsInstanceOf(typeof(IProjectExplorerZoneNode), zone);
            Assert.IsNotNull(zone);
        }

        [Test]
        public async Task WhenModelCachedAndInstanceLocatorValid_ThenTryFindNodeReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.IsNull(modelService.TryFindNode(
                new InstanceLocator("nonexisting-1", "zone-1", SampleWindowsInstanceInZone1.Name)));

            var instance = modelService.TryFindNode(
                new InstanceLocator("project-1", "zone-1", SampleWindowsInstanceInZone1.Name));
            Assert.IsInstanceOf(typeof(IProjectExplorerInstanceNode), instance);
            Assert.IsNotNull(instance);
        }

        //---------------------------------------------------------------------
        // Active node.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoActiveNodeSetAndModelNotCached_ThenActiveNodeReturnsRoot()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);

            Assert.IsNull(modelService.ActiveNode);
        }

        [Test]
        public async Task WhenNoActiveNodeSetAndModelCached_ThenActiveNodeReturnsRoot()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);

            Assert.IsNotNull(modelService.ActiveNode);
            Assert.AreSame(model, modelService.ActiveNode);
        }

        [Test]
        public async Task WhenActiveNodeGone_ThenActiveNodeReturnsRoot()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);

            var goneZone = new Mock<IProjectExplorerZoneNode>();
            goneZone.SetupGet(z => z.Zone).Returns(new ZoneLocator("project-1", "zone-gone"));

            await modelService.SetActiveNodeAsync(goneZone.Object);

            Assert.IsNotNull(modelService.ActiveNode);
            Assert.AreSame(model, modelService.ActiveNode);
        }

        [Test]
        public async Task WhenActiveNodeSet_ThenActiveNodeReturnsNode()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            await modelService.GetModelAsync(false, CancellationToken.None);

            var instance = modelService.TryFindNode(
                new InstanceLocator("project-1", "zone-1", SampleWindowsInstanceInZone1.Name));
            Assert.IsNotNull(instance);

            await modelService.SetActiveNodeAsync(instance);

            Assert.IsNotNull(modelService.ActiveNode);
            Assert.AreSame(instance, modelService.ActiveNode);
        }

        [Test]
        public async Task WhenActiveNodeSet_ThenEventIsFired()
        {

            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);

            var project1 = model.Projects.First();
            await modelService.SetActiveNodeAsync(project1);

            eventService.Verify(s => s.FireAsync<ProjectExplorerNodeSelectedEvent>(
                    It.Is<ProjectExplorerNodeSelectedEvent>(e => e.SelectedNode == project1)),
                Times.Once);
        }

        [Test]
        public async Task WhenActiveNodeSetToNull_ThenNoEventIsFired()
        {

            var serviceRegistry = new ServiceRegistry();
            var eventService = serviceRegistry.AddMock<IEventService>();
            serviceRegistry.AddSingleton(CreateProjectRepositoryMock("project-1").Object);
            serviceRegistry.AddSingleton(CreateComputeEngineAdapterMock("project-1").Object);

            var modelService = new ProjectModelService(serviceRegistry);
            var model = await modelService.GetModelAsync(false, CancellationToken.None);

            await modelService.SetActiveNodeAsync(null);

            eventService.Verify(s => s.FireAsync<ProjectExplorerNodeSelectedEvent>(
                    It.IsAny<ProjectExplorerNodeSelectedEvent>()),
                Times.Never);
        }
    }
}
