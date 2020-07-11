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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestComputeEngineAdapter : FixtureBase
    {
        [Test]
        public async Task WhenUserInViewerRole_ThenListInstancesAsyncReturnsInstances(
            [LinuxInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ICredential credential)
        {
            // Make sure there is at least one instance.
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var adapter = new ComputeEngineAdapter(credential);

            var instances = await adapter.ListInstancesAsync(
                    TestProject.ProjectId,
                    CancellationToken.None);

            Assert.Greater(instances.Count(), 1);
            Assert.IsNotNull(instances.FirstOrDefault(i => i.Name == instanceRef.Name));
        }


        [Test]
        public void WhenUserNotInRole_ThenListInstancesAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ICredential credential)
        {
            var adapter = new ComputeEngineAdapter(credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListInstancesAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenUserNotInRole_ThenListDisksAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ICredential credential)
        {
            var adapter = new ComputeEngineAdapter(credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListDisksAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenUserNotInRole_ThenGetImageThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ICredential credential)
        {
            var adapter = new ComputeEngineAdapter(credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetImageAsync(
                    new ImageLocator(TestProject.ProjectId, "someimage"),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetInstanceAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ICredential credential)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var adapter = new ComputeEngineAdapter(credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetInstanceAsync(
                    testInstance.Locator,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetGuestAttributesAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ICredential credential)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var adapter = new ComputeEngineAdapter(credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetGuestAttributesAsync(
                    testInstance.Locator,
                    "somepath/",
                    CancellationToken.None).Wait());
        }
    }
}
