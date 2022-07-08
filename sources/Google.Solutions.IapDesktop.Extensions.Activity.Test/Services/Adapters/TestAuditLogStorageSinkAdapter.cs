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
using Google.Solutions.Common.Test;
using Google.Solutions.Support.Nunit.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GcsObject = Google.Apis.Storage.v1.Data.Object;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestAuditLogStorageSinkAdapter : ActivityFixtureBase
    {
        private static readonly StorageObjectLocator GarbageLocator = new StorageObjectLocator(
            GcsTestData.Bucket,
            "cloudaudit.googleapis.com/activity/2019/12/31/10:00:00_10:59:59_S0.json");
        private static readonly StorageObjectLocator EmptyLocator = new StorageObjectLocator(
            GcsTestData.Bucket,
            "cloudaudit.googleapis.com/activity/2019/12/31/11:00:00_10:59:59_S0.json");
        private static readonly StorageObjectLocator ValidLocator_Jan1_00 = new StorageObjectLocator(
            GcsTestData.Bucket,
            "cloudaudit.googleapis.com/activity/2020/01/01/00:00:00_00:59:59_S0.json");
        private static readonly StorageObjectLocator ValidLocator_Jan1_01 = new StorageObjectLocator(
            GcsTestData.Bucket,
            "cloudaudit.googleapis.com/activity/2020/01/01/01:00:00_01:59:59_S0.json");
        private static readonly StorageObjectLocator ValidLocator_Jan2_00 = new StorageObjectLocator(
            GcsTestData.Bucket,
            "cloudaudit.googleapis.com/activity/2020/01/02/00:00:00_00:59:59_S0.json");

        private static string GenerateEventJson(DateTime timestamp)
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'NotifyInstanceLocation',
                 'request': {
                   '@type': 'type.googleapis.com/NotifyInstanceLocation'
                 },
                 'metadata': {
                   'serverId': '4aaaa7b32a208e7ccb4ee62acedee725',
                   'timestamp': '1900-01-01T00:00:00.000Z',
                   '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceLocationMetadata'
                 }
               },
               'insertId': '-x0boqfe25xye',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '7045222222254025',
                   'project_id': 'project-1',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '" + timestamp.ToString("o") + @"',
               'severity': 'INFO',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
               'receiveTimestamp': '1900-01-01T00:00:00.000Z'
             }";

            return json.Replace("\r\n", string.Empty);
        }

        private static readonly DateTime EventTimestamp_Jan1_00_01 = new DateTime(2020, 1, 1, 0, 1, 0, DateTimeKind.Utc);
        private static readonly DateTime EventTimestamp_Jan1_00_02 = new DateTime(2020, 1, 1, 0, 2, 0, DateTimeKind.Utc);
        private static readonly DateTime EventTimestamp_Jan1_00_03 = new DateTime(2020, 1, 1, 0, 3, 0, DateTimeKind.Utc);
        private static readonly DateTime EventTimestamp_Jan1_01_01 = new DateTime(2020, 1, 1, 1, 1, 0, DateTimeKind.Utc);
        private static readonly DateTime EventTimestamp_Jan2_00_01 = new DateTime(2020, 1, 2, 0, 1, 0, DateTimeKind.Utc);

        [OneTimeSetUp]
        public static void SetUpTestBucket()
        {
            GcsTestData.CreateObjectIfNotExist(
                GarbageLocator,
                "<garbage/>");
            GcsTestData.CreateObjectIfNotExist(
                EmptyLocator,
                string.Empty);
            GcsTestData.CreateObjectIfNotExist(
                ValidLocator_Jan1_00,
                GenerateEventJson(EventTimestamp_Jan1_00_01) + "\n" +
                GenerateEventJson(EventTimestamp_Jan1_00_02) + "\n" +
                GenerateEventJson(EventTimestamp_Jan1_00_03) + "\n");
            GcsTestData.CreateObjectIfNotExist(
                ValidLocator_Jan1_01,
                GenerateEventJson(EventTimestamp_Jan1_01_01));
            GcsTestData.CreateObjectIfNotExist(
                ValidLocator_Jan2_00,
                GenerateEventJson(EventTimestamp_Jan2_00_01));
        }

        //---------------------------------------------------------------------
        // FindCloudStorageExportBucketForAuditLogsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSinkTooYoung_ThenFindCloudStorageExportBucketForAuditLogsAsyncReturnsNull()
        {
            var auditLogAdapter = new Mock<IAuditLogAdapter>();
            auditLogAdapter.Setup(
                a => a.ListCloudStorageSinksAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new LogSink()
                    {
                        Destination = "storage.googleapis.com/mybucket",
                        CreateTime = "2020-01-01"
                    }
                });

            var service = new AuditLogStorageSinkAdapter(
                new Mock<IStorageAdapter>().Object,
                auditLogAdapter.Object);

            var bucket = await service.FindCloudStorageExportBucketForAuditLogsAsync(
                    TestProject.ProjectId,
                    new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Before sink creation
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(bucket);
        }

        [Test]
        public async Task WhenExportBucketEmpty_ThenFindCloudStorageExportBucketForAuditLogsAsyncReturnsNull()
        {
            var auditLogAdapter = new Mock<IAuditLogAdapter>();
            auditLogAdapter.Setup(
                a => a.ListCloudStorageSinksAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new LogSink()
                    {
                        Destination = "storage.googleapis.com/mybucket",
                        CreateTime = "2019-01-01"
                    }
                });

            var storageAdapter = new Mock<IStorageAdapter>();
            storageAdapter.Setup(
                a => a.ListObjectsAsync(
                    It.Is<string>(b => b == "mybucket"),
                    It.Is<string>(p => p == AuditLogStorageSinkAdapter.AuditLogPrefix),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<GcsObject>());

            var service = new AuditLogStorageSinkAdapter(
                storageAdapter.Object,
                auditLogAdapter.Object);

            var bucket = await service
                .FindCloudStorageExportBucketForAuditLogsAsync(
                    TestProject.ProjectId,
                    new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(bucket);
        }

        [Test]
        public async Task WhenExportBucketAccessDenied_ThenFindCloudStorageExportBucketForAuditLogsAsyncReturnsNull()
        {
            var auditLogAdapter = new Mock<IAuditLogAdapter>();
            auditLogAdapter.Setup(
                a => a.ListCloudStorageSinksAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new LogSink()
                    {
                        Destination = "storage.googleapis.com/mybucket",
                        CreateTime = "2019-01-01"
                    }
                });

            var storageAdapter = new Mock<IStorageAdapter>();
            storageAdapter.Setup(
                a => a.ListObjectsAsync(
                    It.Is<string>(b => b == "mybucket"),
                    It.Is<string>(p => p == AuditLogStorageSinkAdapter.AuditLogPrefix),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("denied", null));

            var service = new AuditLogStorageSinkAdapter(
                storageAdapter.Object,
                auditLogAdapter.Object);

            var bucket = await service
                .FindCloudStorageExportBucketForAuditLogsAsync(
                    TestProject.ProjectId,
                    new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(bucket);
        }

        [Test]
        public async Task WhenUserIsInLogsViewerRoleOnly_ThenFindCloudStorageExportBucketForAuditLogsAsyncReturnsNull(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var bucket = await service
                .FindCloudStorageExportBucketForAuditLogsAsync(
                    TestProject.ProjectId,
                    DateTime.Now.AddDays(-1),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(bucket, "Bucket not accessible, if if it existed");
        }

        //---------------------------------------------------------------------
        // ListInstanceEventsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenObjectContainsGarbage_ThenListInstanceEventsAsyncThrowsException(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            ExceptionAssert.ThrowsAggregateException<JsonReaderException>(
                () => service.ListInstanceEventsAsync(GarbageLocator, CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenObjectIsEmpty_ThenListInstanceEventsAsyncReturnsEmptyListOfEvents(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var events = await service.ListInstanceEventsAsync(
                    EmptyLocator,
                    CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public async Task WhenObjectContainsEventExport_ThenListInstanceEventsAsyncReturnsEvents(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var events = await service.ListInstanceEventsAsync(
                    ValidLocator_Jan1_00,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(3, events.Count());
        }

        //---------------------------------------------------------------------
        // ListInstanceEventsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenOneObjectContainsGarbage_ThenListInstanceEventsAsyncThrowsException(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            ExceptionAssert.ThrowsAggregateException<JsonReaderException>(
                () => service.ListInstanceEventsAsync(
                    new[] { ValidLocator_Jan1_00, GarbageLocator },
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenObjectsContainEventExports_ThenListInstanceEventsAsyncReturnsEvents(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var events = await service.ListInstanceEventsAsync(
                    new[] { EmptyLocator, ValidLocator_Jan1_00 },
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(3, events.Count());
        }

        //---------------------------------------------------------------------
        // FindAuditLogExportObjectsGroupedByDay.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenEndTimeBeforeStartTime_ThenFindExportObjectsThrowsException(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => service.FindAuditLogExportObjectsGroupedByDay(
                    GcsTestData.Bucket,
                    new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenNoObjectsInRange_ThenFindExportObjectsReturnsEmptyDictionary(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var locators = await service.FindAuditLogExportObjectsGroupedByDay(
                    GcsTestData.Bucket,
                    new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2020, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsEmpty(locators);
        }

        [Test]
        public async Task WhenObjectsInRange_ThenFindExportObjectsReturnsList(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var jan1 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var jan2 = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            var locators = await service.FindAuditLogExportObjectsGroupedByDay(
                    GcsTestData.Bucket,
                    jan1,
                    jan2,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(2, locators.Count()); // 2 days
            Assert.AreEqual(2, locators[jan1].Count());
            Assert.AreEqual(1, locators[jan2].Count());
        }

        //---------------------------------------------------------------------
        // ProcessInstanceEventsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenExpectedOrderIsNewestFirst_ThenEventsAreProcessedInDescendingOrder(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var eventsProcessed = new List<EventBase>();
            var processor = new Mock<IEventProcessor>();
            processor.SetupGet(p => p.ExpectedOrder).Returns(EventOrder.NewestFirst);
            processor.SetupGet(p => p.SupportedMethods).Returns(new[] { "NotifyInstanceLocation" });
            processor.SetupGet(p => p.SupportedSeverities).Returns(new[] { "INFO", "ERROR" });
            processor
                .Setup(p => p.Process(It.IsAny<EventBase>()))
                .Callback((EventBase e) => eventsProcessed.Add(e));

            var jan1 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await service.ProcessInstanceEventsAsync(
                    GcsTestData.Bucket,
                        jan1,
                    jan1.AddMonths(1),
                    processor.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, eventsProcessed.Count);

            Assert.AreEqual(EventTimestamp_Jan1_00_01, eventsProcessed[4].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan1_00_02, eventsProcessed[3].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan1_00_03, eventsProcessed[2].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan1_01_01, eventsProcessed[1].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan2_00_01, eventsProcessed[0].Timestamp);
        }

        [Test]
        public async Task WhenExpectedOrderIsOldestFirst_ThenEventsAreProcessedInAscendingOrder(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var eventsProcessed = new List<EventBase>();
            var processor = new Mock<IEventProcessor>();
            processor.SetupGet(p => p.ExpectedOrder).Returns(EventOrder.OldestFirst);
            processor.SetupGet(p => p.SupportedMethods).Returns(new[] { "NotifyInstanceLocation" });
            processor.SetupGet(p => p.SupportedSeverities).Returns(new[] { "INFO", "ERROR" });
            processor
                .Setup(p => p.Process(It.IsAny<EventBase>()))
                .Callback((EventBase e) => eventsProcessed.Add(e));

            var jan1 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await service.ProcessInstanceEventsAsync(
                    GcsTestData.Bucket,
                    jan1,
                    jan1.AddMonths(1),
                    processor.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, eventsProcessed.Count);

            Assert.AreEqual(EventTimestamp_Jan1_00_01, eventsProcessed[0].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan1_00_02, eventsProcessed[1].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan1_00_03, eventsProcessed[2].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan1_01_01, eventsProcessed[3].Timestamp);
            Assert.AreEqual(EventTimestamp_Jan2_00_01, eventsProcessed[4].Timestamp);
        }

        [Test]
        public async Task WhenSeverityDoesNotMatch_ThenEventsAreNotProcessed(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var eventsProcessed = new List<EventBase>();
            var processor = new Mock<IEventProcessor>();
            processor.SetupGet(p => p.ExpectedOrder).Returns(EventOrder.OldestFirst);
            processor.SetupGet(p => p.SupportedMethods).Returns(new[] { "NotifyInstanceLocation" });
            processor.SetupGet(p => p.SupportedSeverities).Returns(new[] { "NOT-INFO" });
            processor
                .Setup(p => p.Process(It.IsAny<EventBase>()))
                .Callback((EventBase e) => eventsProcessed.Add(e));

            var jan1 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await service.ProcessInstanceEventsAsync(
                    GcsTestData.Bucket,
                    jan1,
                    jan1.AddMonths(1),
                    processor.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(0, eventsProcessed.Count);
        }

        [Test]
        public async Task WhenMethodDoesNotMatch_ThenEventsAreNotProcessed(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var service = new AuditLogStorageSinkAdapter(
                new StorageAdapter(await credential),
                new AuditLogAdapter(await credential));

            var eventsProcessed = new List<EventBase>();
            var processor = new Mock<IEventProcessor>();
            processor.SetupGet(p => p.ExpectedOrder).Returns(EventOrder.OldestFirst);
            processor.SetupGet(p => p.SupportedMethods).Returns(new[] { "SomeOtherMethod" });
            processor.SetupGet(p => p.SupportedSeverities).Returns(new[] { "INFO" });
            processor
                .Setup(p => p.Process(It.IsAny<EventBase>()))
                .Callback((EventBase e) => eventsProcessed.Add(e));

            var jan1 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await service.ProcessInstanceEventsAsync(
                    GcsTestData.Bucket,
                    jan1,
                    jan1.AddMonths(1),
                    processor.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(0, eventsProcessed.Count);
        }
    }
}
