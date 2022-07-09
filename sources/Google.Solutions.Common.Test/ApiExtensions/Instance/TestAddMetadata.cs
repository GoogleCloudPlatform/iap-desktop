//
// Copyright 2019 Google LLC
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

using Google.Apis.Compute.v1;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common;
using Google.Solutions.Testing.Common.Integration;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Extensions
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("Windows")]
    public class TestAddMetadata : CommonFixtureBase
    {
        private InstancesResource instancesResource;
        private ProjectsResource projectsResource;

        [SetUp]
        public void SetUp()
        {
            var service = TestProject.CreateComputeService();

            this.instancesResource = service.Instances;
            this.projectsResource = service.Projects;
        }

        //---------------------------------------------------------------------
        // Instance metadata.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingNewKey_ThenAddInstanceMetadataSucceeds(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;

            var key = Guid.NewGuid().ToString();
            var value = "metadata value";

            await this.instancesResource.AddMetadataAsync(
                    locator,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var instance = await this.instancesResource.Get(
                    locator.ProjectId,
                    locator.Zone,
                    locator.Name)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                instance.Metadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task WhenUsingExistingKey_ThenAddInstanceMetadataSucceeds(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;

            var key = Guid.NewGuid().ToString();

            await this.instancesResource.AddMetadataAsync(
                    locator,
                    key,
                    "value to be overridden",
                    CancellationToken.None)
                .ConfigureAwait(false);

            var value = "metadata value";
            await this.instancesResource.AddMetadataAsync(
                    locator,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var instance = await this.instancesResource.Get(
                    locator.ProjectId,
                    locator.Zone,
                    locator.Name)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                instance.Metadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task WhenUpdateConflictingOnFirstAttempt_ThenUpdateInstanceMetadataRetriesAndSucceeds(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;

            var key = Guid.NewGuid().ToString();

            int callbacks = 0;
            await this.instancesResource.UpdateMetadataAsync(
                    locator,
                    metadata =>
                    {
                        if (callbacks++ == 0)
                        {
                            // Provoke a conflict on the first attempt.
                            this.instancesResource.AddMetadataAsync(
                                locator,
                                key,
                                "conflict #" + callbacks,
                                CancellationToken.None).Wait();
                        }

                        metadata.Add(key, "value");
                    },
                    CancellationToken.None,
                    2)
                .ConfigureAwait(false);

            var instance = await this.instancesResource.Get(
                    locator.ProjectId,
                    locator.Zone,
                    locator.Name)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                "value",
                instance.Metadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task WhenUpdateKeepsConflicting_ThenUpdateInstanceMetadataThrowsException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;

            var key = Guid.NewGuid().ToString();

            int callbacks = 0;
            ExceptionAssert.ThrowsAggregateException<GoogleApiException>(
                () => this.instancesResource.UpdateMetadataAsync(
                    locator,
                    metadata =>
                    {
                        // Provoke a conflict every time.
                        this.instancesResource.AddMetadataAsync(
                            locator,
                            key,
                            "conflict #" + callbacks++,
                            CancellationToken.None).Wait();

                        metadata.Add(key, "value");
                    },
                    CancellationToken.None,
                    2).Wait());
        }

        //---------------------------------------------------------------------
        // Project metadata.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingNewKey_ThenAddProjectMetadataSucceeds()
        {
            var key = Guid.NewGuid().ToString();
            var value = "metadata value";

            await this.projectsResource.AddMetadataAsync(
                    TestProject.ProjectId,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var project = await this.projectsResource.Get(TestProject.ProjectId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                project.CommonInstanceMetadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task WhenUsingExistingKey_ThenAddProjectMetadataSucceeds()
        {
            var key = Guid.NewGuid().ToString();

            await this.projectsResource.AddMetadataAsync(
                    TestProject.ProjectId,
                    key,
                    "value to be overridden",
                    CancellationToken.None)
                .ConfigureAwait(false);

            var value = "metadata value";
            await this.projectsResource.AddMetadataAsync(
                    TestProject.ProjectId,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var project = await this.projectsResource.Get(TestProject.ProjectId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                project.CommonInstanceMetadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task WhenUpdateConflictingOnFirstAttempt_ThenUpdateProjectMetadataRetriesAndSucceeds()
        {
            var key = Guid.NewGuid().ToString();

            int callbacks = 0;
            await this.projectsResource.UpdateMetadataAsync(
                    TestProject.ProjectId,
                    metadata =>
                    {
                        if (callbacks++ == 0)
                        {
                            // Provoke a conflict on the first attempt.
                            this.projectsResource.AddMetadataAsync(
                                TestProject.ProjectId,
                                key,
                                "conflict #" + callbacks,
                                CancellationToken.None).Wait();
                        }

                        metadata.Add(key, "value");
                    },
                    CancellationToken.None,
                    2)
                .ConfigureAwait(false);

            var project = await this.projectsResource.Get(TestProject.ProjectId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                "value",
                project.CommonInstanceMetadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public void WhenUpdateKeepsConflicting_ThenUpdateProjectMetadataThrowsException()
        {
            var key = Guid.NewGuid().ToString();

            int callbacks = 0;
            ExceptionAssert.ThrowsAggregateException<GoogleApiException>(
                () => this.projectsResource.UpdateMetadataAsync(
                    TestProject.ProjectId,
                    metadata =>
                    {
                        // Provoke a conflict every time.
                        this.projectsResource.AddMetadataAsync(
                            TestProject.ProjectId,
                            key,
                            "conflict #" + callbacks++,
                            CancellationToken.None).Wait();

                        metadata.Add(key, "value");
                    },
                    CancellationToken.None,
                    1).Wait());
        }
    }
}
