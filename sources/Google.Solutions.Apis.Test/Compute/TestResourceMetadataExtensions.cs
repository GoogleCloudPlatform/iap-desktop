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
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    [UsesCloudResources]
    public class TestResourceMetadataExtensions : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // AddMetadata (instance).
        //---------------------------------------------------------------------

        [Test]
        public async Task AddMetadata_WhenUsingNewKey(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;
            var instancesResource = TestProject.CreateComputeService().Instances;

            var key = Guid.NewGuid().ToString();
            var value = "metadata value";

            await instancesResource
                .AddMetadataAsync(
                    locator,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var instance = await instancesResource
                .Get(
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
        public async Task AddMetadata_WhenUsingExistingKey(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;
            var instancesResource = TestProject.CreateComputeService().Instances;

            var key = Guid.NewGuid().ToString();

            await instancesResource
                .AddMetadataAsync(
                    locator,
                    key,
                    "value to be overridden",
                    CancellationToken.None)
                .ConfigureAwait(false);

            var value = "metadata value";
            await instancesResource
                .AddMetadataAsync(
                    locator,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var instance = await instancesResource.Get(
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
        public async Task UpdateMetadata_WhenUpdateConflictingOnFirstAttempt(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;
            var instancesResource = TestProject.CreateComputeService().Instances;

            var key = Guid.NewGuid().ToString();

            var callbacks = 0;
            await instancesResource
                .UpdateMetadataAsync(
                    locator,
                    metadata =>
                    {
                        if (callbacks++ == 0)
                        {
                            // Provoke a conflict on the first attempt.
                            instancesResource.AddMetadataAsync(
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

            var instance = await instancesResource
                .Get(
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
        public async Task UpdateMetadata_WhenUpdateKeepsConflicting(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var locator = await testInstance;
            var instancesResource = TestProject.CreateComputeService().Instances;

            var key = Guid.NewGuid().ToString();

            var callbacks = 0;
            ExceptionAssert.ThrowsAggregateException<GoogleApiException>(
                () => instancesResource.UpdateMetadataAsync(
                    locator,
                    metadata =>
                    {
                        // Provoke a conflict every time.
                        instancesResource.AddMetadataAsync(
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
        // AddMetadata (project).
        //---------------------------------------------------------------------

        [Test]
        public async Task AddMetadata_WhenUsingNewKey()
        {
            var projectsResource = TestProject.CreateComputeService().Projects;

            var key = Guid.NewGuid().ToString();
            var value = "metadata value";

            await projectsResource
                .AddMetadataAsync(
                    TestProject.ProjectId,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var project = await projectsResource.Get(TestProject.ProjectId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                project.CommonInstanceMetadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task AddMetadata_WhenUsingExistingKey()
        {
            var key = Guid.NewGuid().ToString();
            var projectsResource = TestProject.CreateComputeService().Projects;

            await projectsResource
                .AddMetadataAsync(
                    TestProject.ProjectId,
                    key,
                    "value to be overridden",
                    CancellationToken.None)
                .ConfigureAwait(false);

            var value = "metadata value";
            await projectsResource
                .AddMetadataAsync(
                    TestProject.ProjectId,
                    key,
                    value,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var project = await projectsResource.Get(TestProject.ProjectId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                project.CommonInstanceMetadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task AddMetadata_WhenUpdateConflictingOnFirstAttempt_ThenUpdateProjectMetadataRetriesAndSucceeds()
        {
            var key = Guid.NewGuid().ToString();
            var projectsResource = TestProject.CreateComputeService().Projects;

            var callbacks = 0;
            await projectsResource
                .UpdateMetadataAsync(
                    TestProject.ProjectId,
                    metadata =>
                    {
                        if (callbacks++ == 0)
                        {
                            // Provoke a conflict on the first attempt.
                            projectsResource.AddMetadataAsync(
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

            var project = await projectsResource.Get(TestProject.ProjectId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(
                "value",
                project.CommonInstanceMetadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public void AddMetadata_WhenUpdateKeepsConflicting_ThenThrowsException()
        {
            var key = Guid.NewGuid().ToString();
            var projectsResource = TestProject.CreateComputeService().Projects;

            var callbacks = 0;
            ExceptionAssert.ThrowsAggregateException<GoogleApiException>(
                () => projectsResource.UpdateMetadataAsync(
                    TestProject.ProjectId,
                    metadata =>
                    {
                        // Provoke a conflict every time.
                        projectsResource.AddMetadataAsync(
                            TestProject.ProjectId,
                            key,
                            "conflict #" + callbacks++,
                            CancellationToken.None).Wait();

                        metadata.Add(key, "value");
                    },
                    CancellationToken.None,
                    1).Wait());
        }

        //---------------------------------------------------------------------
        // GetFlag (project).
        //---------------------------------------------------------------------

        [Test]
        public void GetFlag_WhenProjectMetadataIsNull_ThenGetFlagReturnsNull()
        {
            Assert.IsNull(new Project().GetFlag("flag"));
        }

        //---------------------------------------------------------------------
        // GetFlag (instance).
        //---------------------------------------------------------------------

        [Test]
        public void GetFlag_WhenInstanceMetadataIsNull_ThenGetFlagReturnsNull()
        {
            Assert.IsNull(new Instance().GetFlag(new Project(), "flag"));
        }

        [Test]
        public void GetFlag_WhenInstanceFlagTrue_ThenGetFlagReturnsTrue()
        {
            var project = new Project();
            var instance = new Instance()
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };

            Assert.IsTrue(instance.GetFlag(project, "flag"));
        }

        [Test]
        public void GetFlag_WhenProjectFlagTrueAndInstanceFlagNull_ThenGetFlagReturnsTrue()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };
            var instance = new Instance();

            Assert.IsTrue(instance.GetFlag(project, "flag"));
        }

        [Test]
        public void GetFlag_WhenProjectFlagTrueAndInstanceFlagFalse_ThenGetFlagReturnsFalse()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };
            var instance = new Instance()
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "FALSE"
                        }
                    }
                }
            };

            Assert.IsFalse(instance.GetFlag(project, "flag"));
        }

        [Test]
        public void GetFlag_WhenProjectFlagFalseAndInstanceFlagTrue_ThenGetFlagReturnsTrue()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "false"
                        }
                    }
                }
            };
            var instance = new Instance()
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };

            Assert.IsTrue(instance.GetFlag(project, "flag"));
        }
    }
}
