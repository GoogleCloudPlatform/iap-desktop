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

using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Test.ServiceModel;
using Microsoft.Win32;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services
{
    [TestFixture]
    public class TestProjectInventoryService : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ProjectInventoryService inventory = null;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            this.inventory = new ProjectInventoryService(
                new InventorySettingsRepository(baseKey),
                new MockEventService());
        }

        [Test]
        public void WhenNoProjectsAdded_ListProjectsReturnsEmptyList()
        {
            var projects = this.inventory.ListProjectsAsync().Result;

            Assert.IsFalse(projects.Any());
        }

        [Test]
        public async Task WhenProjectsAddedTwice_ListProjectsReturnsProjectOnce()
        {
            await inventory.AddProjectAsync("test-123");
            await inventory.AddProjectAsync("test-123");

            var projects = this.inventory.ListProjectsAsync().Result;

            Assert.AreEqual(1, projects.Count());
            Assert.AreEqual("test-123", projects.First().Name);
        }

        [Test]
        public async Task WhenProjectsDeleted_ListProjectsExcludesProject()
        {
            await inventory.AddProjectAsync("test-123");
            await inventory.AddProjectAsync("test-456");
            await inventory.DeleteProjectAsync("test-456");

            var projects = this.inventory.ListProjectsAsync().Result;

            Assert.AreEqual(1, projects.Count());
            Assert.AreEqual("test-123", projects.First().Name);
        }
    }
}
