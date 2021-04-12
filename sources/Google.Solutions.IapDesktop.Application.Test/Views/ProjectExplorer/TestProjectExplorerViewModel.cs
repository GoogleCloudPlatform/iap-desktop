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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
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

        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ApplicationSettingsRepository settingsRepository;
        private ProjectRepository projectRepository;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.settingsRepository = new ApplicationSettingsRepository(hkcu.CreateSubKey(TestKeyPath));
            this.projectRepository = new ProjectRepository(
                hkcu.CreateSubKey(TestKeyPath),
                new Mock<IEventService>().Object);
        }

        private ProjectExplorerViewModel CreateViewModel()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton<IProjectRepository>(this.projectRepository);

            var eventServiceMock = serviceRegistry.AddMock<IEventService>();

            var computeAdapter = serviceRegistry.AddMock<IComputeEngineAdapter>();
            computeAdapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.Compute.v1.Data.Project()
                {
                    Name = "project-1",
                    Description = $"[project-1]"
                });
            computeAdapter.Setup(a => a.ListInstancesAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SampleWindowsInstanceInZone1 });

            return new ProjectExplorerViewModel(
                new Control(),
                this.settingsRepository,
                new SynchrounousJobService(),
                new ProjectModelService(serviceRegistry));
        }

        private class SynchrounousJobService : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
                => jobFunc(CancellationToken.None);
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

        //---------------------------------------------------------------------
        // Instance filter.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceFilterChanged_ThenViewModelIsUpdated()
        {
            // set to name
            // reset

            Assert.Fail();
        }

        //---------------------------------------------------------------------
        // ExpandRootAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoProjectsAdded_ThenExpandRootAsyncReturnsEmptySet()
        {
            Assert.Fail();
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
        public void WhenRefreshingRoot_ThenProjectsAreReloaded()
        {
            Assert.Fail();
        }

        [Test]
        public void WhenRefreshingProject_ThenProjectIsReloaded()
        {
            Assert.Fail();
        }
        [Test]
        public void WhenRefreshingZone_ThenProjectIsReloaded()
        {
            Assert.Fail();
        }
        [Test]
        public void WhenRefreshingInstance_ThenProjectIsReloaded()
        {
            Assert.Fail();
        }
    }
}
