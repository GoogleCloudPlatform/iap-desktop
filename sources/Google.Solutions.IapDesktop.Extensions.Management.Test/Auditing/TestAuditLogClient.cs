﻿//
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
using Google.Apis.Util;
using Google.Solutions.Apis;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Apis.Logging;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Management.History;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestAuditLogClient : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // ProcessInstanceEventsAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenProcessInstanceEventsThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var client = new AuditLogClient(
                new LoggingClient(
                    LoggingClient.CreateEndpoint(),
                    await credential.ToAuthorization(),
                    TestProject.UserAgent));

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.ProcessInstanceEventsAsync(
                    new[] { TestProject.ProjectId },
                    null,  // all zones.
                    null,  // all instances.
                    DateTime.UtcNow.AddDays(-1),
                    new Mock<IEventProcessor>().Object,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserInViewerRole_ThenProcessInstanceEventsInvokesProcessor(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<ICredential> credential)
        {
            var startDate = DateTime.UtcNow.AddDays(-3);
            var endDate = DateTime.UtcNow;

            var client = new AuditLogClient(
                new LoggingClient(
                    LoggingClient.CreateEndpoint(),
                    await credential.ToAuthorization(),
                    TestProject.UserAgent));

            var processor = new Mock<IEventProcessor>();

            await client
                .ProcessInstanceEventsAsync(
                    new[] { TestProject.ProjectId },
                    null,
                    null,
                    startDate,
                    processor.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            processor.Verify(p => p.Process(It.IsAny<EventBase>()), Times.AtLeastOnce());
        }

        //---------------------------------------------------------------------
        // Filter string.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMethodAndSeveritiesSpecified_ThenCreateFilterStringAddsCriteria()
        {
            var filter = AuditLogClient.CreateFilterString(
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
            var filter = AuditLogClient.CreateFilterString(
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
            var filter = AuditLogClient.CreateFilterString(
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
            var filter = AuditLogClient.CreateFilterString(
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
            var filter = AuditLogClient.CreateFilterString(
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
    }
}
