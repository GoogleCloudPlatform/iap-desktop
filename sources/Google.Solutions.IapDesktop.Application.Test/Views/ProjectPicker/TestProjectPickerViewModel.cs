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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.ProjectPicker;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Views.ProjectPicker
{
    [TestFixture]
    public class TestProjectPickerViewModel : ApplicationFixtureBase
    {
        private static readonly Project ProjectOne = new Project()
        {
            Name = "Project #1",
            ProjectId = "project-1"
        };

        private static readonly Project ProjectTwo = new Project()
        {
            Name = "Project #2",
            ProjectId = "project-2"
        };

        private static readonly Project ProjectThree = new Project()
        {
            Name = "Project #3",
            ProjectId = "project-3"
        };

        private Mock<IResourceManagerAdapter> CreateResourceManagerAdapterMock()
        {
            var resourceManagerMock = new Mock<IResourceManagerAdapter>();

            resourceManagerMock
                .Setup(a => a.ListProjectsAsync(
                    It.Is<ProjectFilter>(f => f == null),
                    It.IsAny<Nullable<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilteredProjectList(
                    new[] { ProjectOne, ProjectTwo, ProjectThree },
                    true));
            resourceManagerMock
                .Setup(a => a.ListProjectsAsync(
                    It.Is<ProjectFilter>(f => f != null && f.ToString().Contains("project-1")),
                    It.IsAny<Nullable<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilteredProjectList(
                    new[] { ProjectOne },
                    true));
            resourceManagerMock
                .Setup(a => a.ListProjectsAsync(
                    It.Is<ProjectFilter>(f => f != null && f.ToString().Contains("fail")),
                    It.IsAny<Nullable<int>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            return resourceManagerMock;
        }

        //---------------------------------------------------------------------
        // Filter.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFilterSetToNull_ThenListIsPopulatedWithAllProjects()
        {
            var resourceManagerMock = CreateResourceManagerAdapterMock();
            var viewModel = new ProjectPickerViewModel(resourceManagerMock.Object);

            CollectionAssert.IsEmpty(viewModel.FilteredProjects);

            await viewModel
                .FilterAsync(null)
                .ConfigureAwait(true);

            CollectionAssert.IsNotEmpty(viewModel.FilteredProjects);
            Assert.AreEqual(3, viewModel.FilteredProjects.Count);
        }

        [Test]
        public async Task WhenFilterTooBroad_ThenStatusTextIndicatedTruncation()
        {
            var resourceManagerMock = CreateResourceManagerAdapterMock();
            var viewModel = new ProjectPickerViewModel(resourceManagerMock.Object);

            await viewModel
                .FilterAsync(null)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsStatusTextVisible);
            StringAssert.Contains("Over 3", viewModel.StatusText);
        }

        [Test]
        public async Task WhenFilterSpecificEnough_ThenStatusTextIndicatsNumberOfResults()
        {
            var resourceManagerMock = CreateResourceManagerAdapterMock();
            var viewModel = new ProjectPickerViewModel(resourceManagerMock.Object);

            await viewModel
                .FilterAsync("project-1")
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsStatusTextVisible);
            StringAssert.Contains("1 project", viewModel.StatusText);
        }

        [Test]
        public async Task WhenFilterUpdated_ThenSelectionIsCleared()
        {
            var resourceManagerMock = CreateResourceManagerAdapterMock();
            var viewModel = new ProjectPickerViewModel(resourceManagerMock.Object);

            await viewModel
                .FilterAsync(null)
                .ConfigureAwait(true);
            viewModel.SelectedProjects = new[] { ProjectOne };

            Assert.IsTrue(viewModel.IsProjectSelected);
            Assert.IsNotNull(viewModel.SelectedProjects);

            await viewModel
                .FilterAsync("project-1")
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsProjectSelected);
            Assert.IsNull(viewModel.SelectedProjects);
        }

        [Test]
        public async Task WhenFilteringFails_ThenLoadingErrorContainsException()
        {
            var resourceManagerMock = CreateResourceManagerAdapterMock();
            var viewModel = new ProjectPickerViewModel(resourceManagerMock.Object)
            {
                SelectedProjects = new[] { ProjectOne }
            };

            Assert.IsTrue(viewModel.IsProjectSelected);
            Assert.IsNotNull(viewModel.SelectedProjects);

            await viewModel
                .FilterAsync("fail")
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsLoading);
            Assert.IsNotNull(viewModel.LoadingError);
            Assert.AreEqual("mock", viewModel.LoadingError.Message);
        }
    }
}
