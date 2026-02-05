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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestProjectRepository : ApplicationFixtureBase
    {
        private static readonly ProjectLocator SampleProject
            = new ProjectLocator("test-123");

        private ProjectRepository CreateProjectRepository()
        {
            return new ProjectRepository(RegistryKeyPath.ForCurrentTest().CreateKey());
        }

        //---------------------------------------------------------------------
        // ListProjects.
        //---------------------------------------------------------------------

        [Test]
        public void ListProjects_WhenNoProjectsAdded()
        {
            using (var repository = CreateProjectRepository())
            {
                var projects = repository.ListProjectsAsync().Result;

                Assert.IsFalse(projects.Any());
            }
        }

        [Test]
        public void ListProjects_WhenProjectsAddedTwice()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(SampleProject);
                repository.AddProject(SampleProject);

                var projects = repository.ListProjectsAsync().Result;

                Assert.That(projects.Count(), Is.EqualTo(1));
                Assert.That(projects.First().ProjectId, Is.EqualTo("test-123"));
            }
        }

        [Test]
        public void ListProjects_WhenProjectsDeleted()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(SampleProject);
                repository.AddProject(new ProjectLocator("test-456"));
                repository.RemoveProject(new ProjectLocator("test-456"));
                repository.RemoveProject(new ProjectLocator("test-456"));

                var projects = repository.ListProjectsAsync().Result;

                Assert.That(projects.Count(), Is.EqualTo(1));
                Assert.That(projects.First().ProjectId, Is.EqualTo("test-123"));
            }
        }

        //---------------------------------------------------------------------
        // AddProject.
        //---------------------------------------------------------------------

        [Test]
        public void AddProject_WhenProjectExists()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(SampleProject);
                using (var key = repository.OpenRegistryKey("test-123"))
                {
                    Assert.IsNotNull(key);
                }
            }
        }

        [Test]
        public void AddProject_CreateSubkey()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(SampleProject);
                using (var key = repository.OpenRegistryKey("test-123", "subkey"))
                {
                    Assert.IsNotNull(key);
                }
            }
        }

        //---------------------------------------------------------------------
        // RemoveProject.
        //---------------------------------------------------------------------

        [Test]
        public void RemoveProject_RaisesEvent()
        {
            using (var repository = CreateProjectRepository())
            {
                PropertyAssert.RaisesPropertyChangedNotification(
                    repository,
                    () => repository.RemoveProject(SampleProject),
                    nameof(repository.Projects));
            }
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public void Projects_WhenNoProjectsAdded()
        {
            using (var repository = CreateProjectRepository())
            {
                Assert.IsFalse(repository.Projects.Any());
            }
        }

        [Test]
        public void Projects_WhenProjectsAddedTwice()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(SampleProject);
                repository.AddProject(SampleProject);

                var projects = repository.Projects;

                Assert.That(projects.Count(), Is.EqualTo(1));
                Assert.That(projects.First().ProjectId, Is.EqualTo("test-123"));
            }
        }

        [Test]
        public void Projects_WhenProjectsDeleted()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(SampleProject);
                repository.AddProject(new ProjectLocator("test-456"));
                repository.RemoveProject(new ProjectLocator("test-456"));
                repository.RemoveProject(new ProjectLocator("test-456"));

                var projects = repository.Projects;

                Assert.That(projects.Count(), Is.EqualTo(1));
                Assert.That(projects.First().ProjectId, Is.EqualTo("test-123"));
            }
        }

        //---------------------------------------------------------------------
        // OpenRegistryKey.
        //---------------------------------------------------------------------

        [Test]
        public void OpenRegistryKey_WhenProjectNotAdded()
        {
            using (var repository = CreateProjectRepository())
            {
                Assert.Throws<KeyNotFoundException>(
                    () => repository.OpenRegistryKey(SampleProject.Name));
            }
        }
    }
}
