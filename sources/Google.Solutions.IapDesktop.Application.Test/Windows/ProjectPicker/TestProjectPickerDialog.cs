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
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.ProjectPicker
{
    [TestFixture]
    public class TestProjectPickerDialog
    {
        //---------------------------------------------------------------------
        // CloudModel.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListProjects_WhenCloudModelFilterIsNull_ThenProjectFilterIsNull()
        {
            var resourceManager = new Mock<IResourceManagerClient>();
            var model = new Application.Windows.ProjectPicker.ProjectPickerDialog.CloudModel(resourceManager.Object);

            await model
                .ListProjectsAsync(null, 1, CancellationToken.None)
                .ConfigureAwait(false);

            resourceManager.Verify(a => a.ListProjectsAsync(
                    It.Is<ProjectFilter>(f => f == null),
                    It.IsAny<Nullable<int>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Test]
        public async Task ListProjects_WhenCloudModelFilterIsNotNull_ThenProjectFilterIsSet()
        {
            var resourceManager = new Mock<IResourceManagerClient>();
            var model = new Application.Windows.ProjectPicker.ProjectPickerDialog.CloudModel(resourceManager.Object);

            await model
                .ListProjectsAsync("test", 1, CancellationToken.None)
                .ConfigureAwait(false);

            resourceManager.Verify(a => a.ListProjectsAsync(
                    It.Is<ProjectFilter>(f => f.ToString() == "name:\"*test*\" OR id:\"*test*\""),
                    It.IsAny<Nullable<int>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        //---------------------------------------------------------------------
        // StaticModel.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListProjects_WhenStaticModelProjectsIsNull_ThenListProjectsReturnsEmptyList()
        {
            var model = new Application.Windows.ProjectPicker.ProjectPickerDialog.StaticModel(null!);

            var result = await model
                .ListProjectsAsync(null, 100, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result.Projects.Any(), Is.False);
        }

        [Test]
        public async Task ListProjects_WhenStaticModelFilterIsNull_ThenListProjectsReturnsAllProjects()
        {
            var projects = new List<Project>()
            {
                new Project() { ProjectId = "foo", Name = "Foo" },
                new Project() { ProjectId = "bar", Name = "bar" },
                new Project() { ProjectId = "foobar", Name = "Foobar" }
            };

            var model = new Application.Windows.ProjectPicker.ProjectPickerDialog.StaticModel(projects);

            var result = await model
                .ListProjectsAsync(null, 100, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(
                result.Projects, Is.EquivalentTo(projects));
        }

        [Test]
        public async Task ListProjects_WhenStaticModelMaxResultsExceeded_ThenListProjectsReturnsTruncatedList()
        {
            var projects = new List<Project>()
            {
                new Project() { ProjectId = "foo", Name = "Foo" },
                new Project() { ProjectId = "bar", Name = "bar" },
                new Project() { ProjectId = "foobar", Name = "Foobar" }
            };

            var model = new Application.Windows.ProjectPicker.ProjectPickerDialog.StaticModel(projects);

            var result = await model
                .ListProjectsAsync(null, 2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result.Projects.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task ListProjects_WhenStaticModelFilterIsNotNull_ThenListProjectsReturnsMatchingProjects()
        {
            var projects = new List<Project>()
            {
                new Project() { ProjectId = "foo", Name = "Foo" },
                new Project() { ProjectId = "bar", Name = "bar" },
                new Project() { ProjectId = "foobar", Name = "Foobar" }
            };

            var model = new Application.Windows.ProjectPicker.ProjectPickerDialog.StaticModel(projects);

            var result = await model
                .ListProjectsAsync("FO", 100, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result.Projects.Count(), Is.EqualTo(2));
        }
    }
}
