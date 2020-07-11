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
using Google.Solutions.Common.Test.Integration;
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
    public class TestAddMetadata : FixtureBase
    {
        private InstancesResource instancesResource;

        [SetUp]
        public void SetUp()
        {
            this.instancesResource = TestProject.CreateComputeService().Instances;
        }

        [Test]
        public async Task WhenUsingNewKey_ThenAddMetadataSucceeds(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var key = Guid.NewGuid().ToString();
            var value = "metadata value";

            await this.instancesResource.AddMetadataAsync(
                testInstance.Locator,
                key,
                value,
                CancellationToken.None);

            var instance = await this.instancesResource.Get(
                testInstance.Locator.ProjectId,
                testInstance.Locator.Zone,
                testInstance.Locator.Name)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                instance.Metadata.Items.First(i => i.Key == key).Value);
        }

        [Test]
        public async Task WhenUsingExistingKey_ThenAddMetadataSucceeds(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var key = Guid.NewGuid().ToString();

            await this.instancesResource.AddMetadataAsync(
                testInstance.Locator,
                key,
                "value to be overridden",
                CancellationToken.None);

            var value = "metadata value";
            await this.instancesResource.AddMetadataAsync(
                testInstance.Locator,
                key,
                value,
                CancellationToken.None);

            var instance = await this.instancesResource.Get(
                testInstance.Locator.ProjectId,
                testInstance.Locator.Zone,
                testInstance.Locator.Name)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

            Assert.AreEqual(
                value,
                instance.Metadata.Items.First(i => i.Key == key).Value);
        }
    }
}
