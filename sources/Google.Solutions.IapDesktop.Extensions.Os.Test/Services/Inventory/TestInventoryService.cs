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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Services.Inventory
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestInventoryService : FixtureBase
    {
        // Force publishing of OS inventory data.
        private const string PublishInventoryScript = @"
            & $Env:ProgramFiles\Google\OSConfig\google_osconfig_agent.exe osinventory -debug -stdout | Out-Default
        ";

        //---------------------------------------------------------------------
        // GetInstanceInventoryAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceDoesNotExist_ThenGetInstanceInventoryAsyncReturnsNull(
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            var result = await service.GetInstanceInventoryAsync(
                new InstanceLocator(TestProject.ProjectId, "us-central1-a", "doesnotexist"),
                CancellationToken.None);

            Assert.IsNull(result);
        }

        [Test]
        public async Task WhenInstanceHasInventoryData_ThenGetInstanceInventoryAsyncSucceeds(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript,
                EnableOsInventory = true)] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var instanceRef = await testInstance.GetInstanceAsync();
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            var info = await service.GetInstanceInventoryAsync(instanceRef, CancellationToken.None);

            Assert.IsNotNull(info);
            Assert.AreEqual(instanceRef, info.Instance);
            Assert.IsNotNull(info.KernelVersion);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetInstanceInventoryAsyncThrowsResourceAccessDeniedException(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript,
                EnableOsInventory = true)] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] CredentialRequest credential)
        {
            var instanceRef = await testInstance.GetInstanceAsync();
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => service.GetInstanceInventoryAsync(
                    instanceRef,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // ListProjectInventoryAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAtLeastOneInstanceHasInventoryData_ThenListProjectInventoryAsyncSucceeds(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript,
                EnableOsInventory = true)] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            // Make sure there is at least one instance.
            var instanceRef = await testInstance.GetInstanceAsync();
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            var info = await service.ListProjectInventoryAsync(
                TestProject.ProjectId, CancellationToken.None);

            Assert.IsNotNull(info);
            Assert.Greater(info.Count(), 1);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListProjectInventoryAsyncThrowsResourceAccessDeniedException(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript,
                EnableOsInventory = true)] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] CredentialRequest credential)
        {
            var instanceRef = await testInstance.GetInstanceAsync();
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => service.ListProjectInventoryAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // ListZoneInventoryAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAtLeastOneInstanceHasInventoryData_ThenListZoneInventoryAsyncSucceeds(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript,
                EnableOsInventory = true)] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            // Make sure there is at least one instance.
            var instanceRef = await testInstance.GetInstanceAsync();
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            var info = await service.ListZoneInventoryAsync(
                new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                CancellationToken.None);

            Assert.IsNotNull(info);
            Assert.Greater(info.Count(), 1);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListZoneInventoryAsyncThrowsResourceAccessDeniedException(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript,
                EnableOsInventory = true)] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] CredentialRequest credential)
        {
            var instanceRef = await testInstance.GetInstanceAsync();
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential.GetCredentialAsync()));

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => service.ListZoneInventoryAsync(
                    new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                    CancellationToken.None).Wait());
        }
    }
}
