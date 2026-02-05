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
using Google.Solutions.Apis.Crm;
using Google.Solutions.IapDesktop.Application.Windows.ProjectPicker;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.ProjectPicker
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

        private static Mock<IProjectPickerModel> CreateModelMock()
        {
            var model = new Mock<IProjectPickerModel>();

            model
                .Setup(a => a.ListProjectsAsync(
                    It.Is<string>(f => f == null),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilteredProjectList(
                    new[] { ProjectOne, ProjectTwo, ProjectThree },
                    true));
            model
                .Setup(a => a.ListProjectsAsync(
                    It.Is<string>(f => f != null && f.Contains("project-1")),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FilteredProjectList(
                    new[] { ProjectOne },
                    true));
            model
                .Setup(a => a.ListProjectsAsync(
                    It.Is<string>(f => f != null && f.Contains("fail")),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            return model;
        }

        //---------------------------------------------------------------------
        // Filter.
        //---------------------------------------------------------------------

        [Test]
        public async Task Filter_WhenFilterSetToNull_ThenListIsPopulatedWithAllProjects()
        {
            var modelMock = CreateModelMock();
            var viewModel = new ProjectPickerViewModel(modelMock.Object);

            CollectionAssert.IsEmpty(viewModel.FilteredProjects);

            await viewModel
                .FilterAsync(null)
                .ConfigureAwait(true);

            CollectionAssert.IsNotEmpty(viewModel.FilteredProjects);
            Assert.That(viewModel.FilteredProjects.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Filter_WhenFilterTooBroad_ThenStatusTextIndicatedTruncation()
        {
            var modelMock = CreateModelMock();
            var viewModel = new ProjectPickerViewModel(modelMock.Object);

            await viewModel
                .FilterAsync(null)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsStatusTextVisible.Value);
            StringAssert.Contains("Over 3", viewModel.StatusText.Value);
        }

        [Test]
        public async Task Filter_WhenFilterSpecificEnough_ThenStatusTextIndicatsNumberOfResults()
        {
            var modelMock = CreateModelMock();
            var viewModel = new ProjectPickerViewModel(modelMock.Object);

            await viewModel
                .FilterAsync("project-1")
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsStatusTextVisible.Value);
            StringAssert.Contains("1 project", viewModel.StatusText.Value);
        }

        [Test]
        public async Task Filter_WhenFilterUpdated_ThenSelectionIsCleared()
        {
            var modelMock = CreateModelMock();
            var viewModel = new ProjectPickerViewModel(modelMock.Object);

            await viewModel
                .FilterAsync(null)
                .ConfigureAwait(true);
            viewModel.SelectedProjects.Value = new[] { ProjectOne };

            Assert.IsTrue(viewModel.IsProjectSelected.Value);
            Assert.IsNotNull(viewModel.SelectedProjects.Value);

            await viewModel
                .FilterAsync("project-1")
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsProjectSelected.Value);
            Assert.IsNull(viewModel.SelectedProjects.Value);
        }

        [Test]
        public async Task Filter_WhenFilteringFails_ThenLoadingErrorContainsException()
        {
            var modelMock = CreateModelMock();
            var viewModel = new ProjectPickerViewModel(modelMock.Object);
            viewModel.SelectedProjects.Value = new[] { ProjectOne };

            Assert.IsTrue(viewModel.IsProjectSelected.Value);
            Assert.IsNotNull(viewModel.SelectedProjects.Value);

            await viewModel
                .FilterAsync("fail")
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsLoading.Value);
            Assert.IsNotNull(viewModel.LoadingError);
            Assert.That(viewModel.LoadingError.Value.Message, Is.EqualTo("mock"));
        }
    }
}
