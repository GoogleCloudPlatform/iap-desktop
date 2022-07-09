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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Logging.v2.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.Testing.Common;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestAuditLogAdapter : ActivityFixtureBase
    {
        //---------------------------------------------------------------------
        // ListEventsAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceCreated_ThenListLogEntriesReturnsInsertEvent(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<ICredential> credential)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var adapter = new AuditLogAdapter(await credential);
            var request = new ListLogEntriesRequest()
            {
                ResourceNames = new[]
                {
                    "projects/" + TestProject.ProjectId
                },
                Filter = $"resource.type=\"gce_instance\" " +
                    $"AND protoPayload.methodName:{InsertInstanceEvent.Method} " +
                    $"AND timestamp > {startDate:yyyy-MM-dd}",
                PageSize = 1000,
                OrderBy = "timestamp desc"
            };

            var events = new List<EventBase>();
            var instanceBuilder = new InstanceSetHistoryBuilder(startDate, endDate);

            // Creating the VM might be quicker than the logs become available.
            for (int retry = 0; retry < 4 && !events.Any(); retry++)
            {
                await adapter.ListEventsAsync(
                        request,
                        events.Add,
                        new Apis.Util.ExponentialBackOff(),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (!events.Any())
                {
                    await Task.Delay(20 * 1000).ConfigureAwait(false);
                }
            }

            var insertEvent = events.OfType<InsertInstanceEvent>()
                .First(e => e.InstanceReference == instanceRef);
            Assert.IsNotNull(insertEvent);
        }

        //---------------------------------------------------------------------
        // ProcessInstanceEventsAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceCreated_ThenProcessInstanceEventsAsyncCanFeedHistorySetBuilder(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Roles = new[] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.LogsViewer })] ResourceTask<ICredential> credential)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var instanceBuilder = new InstanceSetHistoryBuilder(
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow);

            var computeAdapter = new ComputeEngineAdapter(await credential);
            instanceBuilder.AddExistingInstances(
                await computeAdapter.ListInstancesAsync(
                        TestProject.ProjectId,
                        CancellationToken.None)
                    .ConfigureAwait(false),
                await computeAdapter.ListNodesAsync
                        (TestProject.ProjectId,
                        CancellationToken.None)
                    .ConfigureAwait(false),
                await computeAdapter.ListDisksAsync(
                        TestProject.ProjectId,
                        CancellationToken.None)
                    .ConfigureAwait(false),
                TestProject.ProjectId);

            var adapter = new AuditLogAdapter(await credential);

            await adapter.ProcessInstanceEventsAsync(
                    new[] { TestProject.ProjectId },
                    null,  // all zones.
                    null,  // all instances.
                    instanceBuilder.StartDate,
                    instanceBuilder,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var set = instanceBuilder.Build();
            var testInstanceHistory = set.Instances.FirstOrDefault(i => i.Reference == instanceRef);

            Assert.IsNotNull(testInstanceHistory, "Instance found in history");
        }

        [Test]
        public async Task WhenUserNotInRole_ThenProcessInstanceEventsAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var instanceBuilder = new InstanceSetHistoryBuilder(
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow);

            var adapter = new AuditLogAdapter(await credential);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ProcessInstanceEventsAsync(
                    new[] { TestProject.ProjectId },
                    null,  // all zones.
                    null,  // all instances.
                    instanceBuilder.StartDate,
                    instanceBuilder,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Filter string.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingInvalidProjectId_ThenListEventsAsyncThrowsException(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<ICredential> credential)
        {
            var startDate = DateTime.UtcNow.AddDays(-30);
            var request = new ListLogEntriesRequest()
            {
                ResourceNames = new[]
                {
                    $"projects/{TestProject.InvalidProjectId}"
                },
                Filter = $"resource.type=\"gce_instance\" " +
                    $"AND protoPayload.methodName:{InsertInstanceEvent.Method} " +
                    $"AND timestamp > {startDate:yyyy-MM-dd}",
                PageSize = 1000,
                OrderBy = "timestamp desc"
            };

            var adapter = new AuditLogAdapter(await credential);
            ExceptionAssert.ThrowsAggregateException<GoogleApiException>(
                () => adapter.ListEventsAsync(
                    request,
                    _ => { },
                    new Apis.Util.ExponentialBackOff(),
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenMethodAndSeveritiesSpecified_ThenCreateFilterStringAddsCriteria()
        {
            var filter = AuditLogAdapter.CreateFilterString(
                null,
                null,
                new[] { "method-1", "method-2" },
                new[] { "INFO", "ERROR" },
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.AreEqual(
                "protoPayload.methodName=(\"method-1\" OR \"method-2\") " +
                    "AND severity=(\"INFO\" OR \"ERROR\") " +
                    "AND resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                    "AND timestamp > \"2020-01-02T03:04:05.0060000Z\"",
                filter);
        }

        [Test]
        public void WhenMethodAndSeveritiesNotSpecified_ThenCreateFilterStringSkipsCriteria()
        {
            var filter = AuditLogAdapter.CreateFilterString(
                null,
                Enumerable.Empty<ulong>(),
                null,
                null,
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.AreEqual(
                "resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\"",
                filter);
        }

        [Test]
        public void WhenMethodAndSeveritiesEmpty_ThenCreateFilterStringSkipsCriteria()
        {
            var filter = AuditLogAdapter.CreateFilterString(
                null,
                Enumerable.Empty<ulong>(),
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.AreEqual(
                "resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\"",
                filter);
        }

        [Test]
        public void WhenInstanceIdSpecified_ThenCreateFilterStringAddsCriteria()
        {
            var filter = AuditLogAdapter.CreateFilterString(
                null,
                new[] { 123454321234ul },
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.AreEqual(
                "(resource.labels.instance_id=(\"123454321234\") OR labels.instance_id=(\"123454321234\")) " +
                "AND resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\"",
                filter);
        }

        [Test]
        public void WhenZonesSpecified_ThenCreateFilterStringAddsCriteria()
        {
            var filter = AuditLogAdapter.CreateFilterString(
                new[] { "us-central1-a" },
                null,
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.AreEqual(
                "(resource.labels.zone=(\"us-central1-a\") OR labels.zone=(\"us-central1-a\")) " +
                "AND resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\"",
                filter);
        }

        //---------------------------------------------------------------------
        // ListCloudStorageSinksAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenListCloudStorageSinksAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new AuditLogAdapter(await credential);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListCloudStorageSinksAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenSinkExists_ThenListCloudStorageSinksAsyncReturnsList(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new AuditLogAdapter(await credential);

            var buckets = await adapter.ListCloudStorageSinksAsync(
                    TestProject.ProjectId,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(buckets);
        }
    }
}
