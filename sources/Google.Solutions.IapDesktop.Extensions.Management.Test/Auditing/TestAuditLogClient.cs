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

using Google.Solutions.Apis;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Apis.Logging;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.History;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
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
        // ProcessInstanceEvents.
        //---------------------------------------------------------------------

        [Test]
        public async Task ProcessInstanceEvents_WhenUserNotInRole_ThenThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var client = new AuditLogClient(
                new LoggingClient(
                    LoggingClient.CreateEndpoint(),
                    await auth,
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
        public async Task ProcessInstanceEvents_WhenUserInViewerRole_ThenInvokesProcessor(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<IAuthorization> auth)
        {
            var startDate = DateTime.UtcNow.AddDays(-3);
            var endDate = DateTime.UtcNow;

            var client = new AuditLogClient(
                new LoggingClient(
                    LoggingClient.CreateEndpoint(),
                    await auth,
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
        // CreateFilterString.
        //---------------------------------------------------------------------

        [Test]
        public void CreateFilterString_WhenMethodAndSeveritiesSpecified_ThenAddsCriteria()
        {
            var filter = AuditLogClient.CreateFilterString(
                null,
                null,
                new[] { "method-1", "method-2" },
                new[] { "INFO", "ERROR" },
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.That(
                filter, Is.EqualTo("protoPayload.methodName=(\"method-1\" OR \"method-2\") " +
                    "AND severity=(\"INFO\" OR \"ERROR\") " +
                    "AND resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                    "AND timestamp > \"2020-01-02T03:04:05.0060000Z\""));
        }

        [Test]
        public void CreateFilterString_WhenMethodAndSeveritiesNotSpecified_ThenSkipsCriteria()
        {
            var filter = AuditLogClient.CreateFilterString(
                null,
                Enumerable.Empty<ulong>(),
                null,
                null,
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.That(
                filter, Is.EqualTo("resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\""));
        }

        [Test]
        public void CreateFilterString_WhenMethodAndSeveritiesEmpty_ThenSkipsCriteria()
        {
            var filter = AuditLogClient.CreateFilterString(
                null,
                Enumerable.Empty<ulong>(),
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.That(
                filter, Is.EqualTo("resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\""));
        }

        [Test]
        public void CreateFilterString_WhenInstanceIdSpecified_ThenAddsCriteria()
        {
            var filter = AuditLogClient.CreateFilterString(
                null,
                new[] { 123454321234ul },
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.That(
                filter, Is.EqualTo("(resource.labels.instance_id=(\"123454321234\") OR labels.instance_id=(\"123454321234\")) " +
                "AND resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\""));
        }

        [Test]
        public void CreateFilterString_WhenZonesSpecified_ThenAddsCriteria()
        {
            var filter = AuditLogClient.CreateFilterString(
                new[] { "us-central1-a" },
                null,
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.That(
                filter, Is.EqualTo("(resource.labels.zone=(\"us-central1-a\") OR labels.zone=(\"us-central1-a\")) " +
                "AND resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\") " +
                "AND timestamp > \"2020-01-02T03:04:05.0060000Z\""));
        }
    }
}
