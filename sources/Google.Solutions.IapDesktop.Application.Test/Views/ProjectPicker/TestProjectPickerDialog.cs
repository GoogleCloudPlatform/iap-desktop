﻿//
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
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Views.ProjectPicker
{
    [TestFixture]
    public class TestProjectPickerDialog
    {
        //---------------------------------------------------------------------
        // CloudModel.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenCloudModelFilterIsNull_ThenProjectFilterIsNull()
        {
            var resourceManager = new Mock<IResourceManagerAdapter>();
            var model = new ProjectPickerDialog.CloudModel(resourceManager.Object);

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
        public async Task WhenCloudModelFilterIsNotNull_ThenProjectFilterIsSet()
        {
            var resourceManager = new Mock<IResourceManagerAdapter>();
            var model = new ProjectPickerDialog.CloudModel(resourceManager.Object);

            await model
                .ListProjectsAsync("test", 1, CancellationToken.None)
                .ConfigureAwait(false);

            resourceManager.Verify(a => a.ListProjectsAsync(
                    It.Is<ProjectFilter>(f => f.ToString() == "name:\"test*\" OR id:\"test*\""),
                    It.IsAny<Nullable<int>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        //---------------------------------------------------------------------
        // StaticModel.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenStaticModelProjectsIsNull_ThenListProjectsReturnsEmptyList()
        {
            var model = new ProjectPickerDialog.StaticModel(null);

            var result = await model
                .ListProjectsAsync(null, 100, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(result.Projects.Any());
        }

        [Test]
        public async Task WhenStaticModelFilterIsNull_ThenListProjectsReturnsAllProjects()
        {
            var projects = new List<Project>()
            {
                new Project() { ProjectId = "foo", Name = "Foo" },
                new Project() { ProjectId = "bar", Name = "bar" },
                new Project() { ProjectId = "foobar", Name = "Foobar" }
            };

            var model = new ProjectPickerDialog.StaticModel(projects);

            var result = await model
                .ListProjectsAsync(null, 100, CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.AreEquivalent(
                projects,
                result.Projects);
        }

        [Test]
        public async Task WhenStaticModelMaxResultsExceeded_ThenListProjectsReturnsTruncatedList()
        {
            var projects = new List<Project>()
            {
                new Project() { ProjectId = "foo", Name = "Foo" },
                new Project() { ProjectId = "bar", Name = "bar" },
                new Project() { ProjectId = "foobar", Name = "Foobar" }
            };

            var model = new ProjectPickerDialog.StaticModel(projects);

            var result = await model
                .ListProjectsAsync(null, 2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(2, result.Projects.Count());
        }

        [Test]
        public async Task WhenStaticModelFilterIsNotNull_ThenListProjectsReturnsMatchingProjects()
        {
            var projects = new List<Project>()
            {
                new Project() { ProjectId = "foo", Name = "Foo" },
                new Project() { ProjectId = "bar", Name = "bar" },
                new Project() { ProjectId = "foobar", Name = "Foobar" }
            };

            var model = new ProjectPickerDialog.StaticModel(projects);

            var result = await model
                .ListProjectsAsync("FO", 100, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(2, result.Projects.Count());
        }
    }
}
