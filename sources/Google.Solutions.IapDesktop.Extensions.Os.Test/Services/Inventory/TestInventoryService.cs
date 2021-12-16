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

using Google.Apis.Auth.OAuth2;
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
    public class TestInventoryService : CommonFixtureBase
    {
        // Publish dummy OS inventory data. The real data is published asynchronously,
        // so it's difficult to rely on it in integration tests.
        private const string PublishInventoryScript = @"Invoke-RestMethod " +
                        "-Headers @{\"Metadata-Flavor\"=\"Google\"} " +
                        "-Method PUT " +
                        "-Uri http://metadata.google.internal/computeMetadata/v1/instance/" +
                        "guest-attributes/guestInventory/Version " +
                        "-Body 99";

        //---------------------------------------------------------------------
        // GetInstanceInventoryAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceDoesNotExist_ThenGetInstanceInventoryAsyncReturnsNull(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            var result = await service
                .GetInstanceInventoryAsync(
                    new InstanceLocator(TestProject.ProjectId, "us-central1-a", "doesnotexist"),
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsNull(result);
        }

        [Test]
        public async Task WhenInstanceHasInventoryData_ThenGetInstanceInventoryAsyncSucceeds(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            var info = await service
                .GetInstanceInventoryAsync(instanceRef, CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsNotNull(info);
            Assert.AreEqual(instanceRef, info.Instance);
            Assert.IsNotNull(info.OperatingSystemVersion);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetInstanceInventoryAsyncThrowsResourceAccessDeniedException(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
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
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            // Make sure there is at least one instance.
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            var info = await service
                .ListProjectInventoryAsync(
                    TestProject.ProjectId,
                    OperatingSystems.All,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsTrue(info.ToList().Where(i => i.Instance == instanceRef).Any());
        }

        [Test]
        public async Task WhenOsMismatches_ThenListProjectInventoryAsyncExcludesInstance(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            // Make sure there is at least one instance.
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            var info = await service
                .ListProjectInventoryAsync(
                    TestProject.ProjectId,
                    OperatingSystems.Linux,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsFalse(info.ToList().Where(i => i.Instance == instanceRef).Any());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListProjectInventoryAsyncThrowsResourceAccessDeniedException(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => service.ListProjectInventoryAsync(
                    TestProject.ProjectId,
                    OperatingSystems.All,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // ListZoneInventoryAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAtLeastOneInstanceHasInventoryData_ThenListZoneInventoryAsyncSucceeds(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            // Make sure there is at least one instance.
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            var info = await service
                .ListZoneInventoryAsync(
                    new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                    OperatingSystems.All,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsTrue(info.ToList().Where(i => i.Instance == instanceRef).Any());
        }

        [Test]
        public async Task WhenOsMismatches_ThenListZoneInventoryAsyncExcludesInstance(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            // Make sure there is at least one instance.
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            var info = await service
                .ListZoneInventoryAsync(
                    new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                    OperatingSystems.Linux,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsFalse(info.ToList().Where(i => i.Instance == instanceRef).Any());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListZoneInventoryAsyncThrowsResourceAccessDeniedException(
            [WindowsInstance(
                InitializeScript = PublishInventoryScript)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var instanceRef = await testInstance;
            var service = new InventoryService(
                new ComputeEngineAdapter(await credential));

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => service.ListZoneInventoryAsync(
                    new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                    OperatingSystems.All,
                    CancellationToken.None).Wait());
        }
    }
}
