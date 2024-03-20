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
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestProjectRepository : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser, RegistryView.Default);

        private ProjectRepository CreateProjectRepository()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            return new ProjectRepository(baseKey);
        }

        [Test]
        public void WhenNoProjectsAdded_ListProjectsReturnsEmptyList()
        {
            using (var repository = CreateProjectRepository())
            {
                var projects = repository.ListProjectsAsync().Result;

                Assert.IsFalse(projects.Any());
            }
        }

        [Test]
        public void WhenProjectsAddedTwice_ListProjectsReturnsProjectOnce()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(new ProjectLocator("test-123"));
                repository.AddProject(new ProjectLocator("test-123"));

                var projects = repository.ListProjectsAsync().Result;

                Assert.AreEqual(1, projects.Count());
                Assert.AreEqual("test-123", projects.First().ProjectId);
            }
        }

        [Test]
        public void WhenProjectsDeleted_ListProjectsExcludesProject()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(new ProjectLocator("test-123"));
                repository.AddProject(new ProjectLocator("test-456"));
                repository.RemoveProject(new ProjectLocator("test-456"));
                repository.RemoveProject(new ProjectLocator("test-456"));

                var projects = repository.ListProjectsAsync().Result;

                Assert.AreEqual(1, projects.Count());
                Assert.AreEqual("test-123", projects.First().ProjectId);
            }
        }

        [Test]
        public void WhenProjectExists_ThenCreateRegistryKeyReturnsKey()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(new ProjectLocator("test-123"));
                using (var key = repository.OpenRegistryKey("test-123"))
                {
                    Assert.IsNotNull(key);
                }
            }
        }

        [Test]
        public void WhenProjectExists_ThenCreateRegistryKeyWithSubkeyReturnsKey()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(new ProjectLocator("test-123"));
                using (var key = repository.OpenRegistryKey("test-123", "subkey", true))
                {
                    Assert.IsNotNull(key);
                }
            }
        }

        [Test]
        public void WhenSubkeyDoesNotExist_ThenOpenRegistryReturnsNull()
        {
            using (var repository = CreateProjectRepository())
            {
                repository.AddProject(new ProjectLocator("test-123"));
                using (var key = repository.OpenRegistryKey("test-123", "subkey", false))
                {
                    Assert.IsNull(key);
                }
            }
        }
    }
}
