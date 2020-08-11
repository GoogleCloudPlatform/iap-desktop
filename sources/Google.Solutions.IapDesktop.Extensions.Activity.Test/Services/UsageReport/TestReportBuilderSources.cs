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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.UsageReport;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.UsageReport
{
    [TestFixture]
    public class TestReportBuilderSources
    {
        [Test]
        public async Task WhenNoApplicableExportSinkAvailable_ThenApiIsUsed()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            var auditLogAdapter = new Mock<IAuditLogAdapter>();
            var auditExportAdapter = new Mock<IAuditLogStorageSinkAdapter>();
            auditExportAdapter.Setup(
                a => a.FindCloudStorageExportBucketForAuditLogsAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);

            var reportBuilder = new ReportBuilder(
                auditLogAdapter.Object,
                auditExportAdapter.Object,
                computeEngineAdapter.Object,
                AuditLogSources.Api | AuditLogSources.StorageExport,
                new[] { "project-1" },
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            await reportBuilder.BuildAsync(CancellationToken.None);

            auditExportAdapter.Verify(
                a => a.ProcessInstanceEventsAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<IEventProcessor>(),
                    It.IsAny<CancellationToken>()), Times.Never);

            auditLogAdapter.Verify(
                a => a.ProcessInstanceEventsAsync(
                    It.Is<IEnumerable<string>>(p => p.All(id => id == "project-1")),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<ulong>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<IEventProcessor>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenExportsAvailableAndAccessible_ThenApiIsNotUsed()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            var auditLogAdapter = new Mock<IAuditLogAdapter>();
            var auditExportAdapter = new Mock<IAuditLogStorageSinkAdapter>();
            auditExportAdapter.Setup(
                a => a.FindCloudStorageExportBucketForAuditLogsAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("audit-bucket");

            var reportBuilder = new ReportBuilder(
                auditLogAdapter.Object,
                auditExportAdapter.Object,
                computeEngineAdapter.Object,
                AuditLogSources.Api | AuditLogSources.StorageExport,
                new[] { "project-1" },
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            await reportBuilder.BuildAsync(CancellationToken.None);

            auditExportAdapter.Verify(
                a => a.ProcessInstanceEventsAsync(
                    It.Is<string>(bucket => bucket == "audit-bucket"),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<IEventProcessor>(),
                    It.IsAny<CancellationToken>()), Times.Once);

            auditLogAdapter.Verify(
                a => a.ProcessInstanceEventsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<ulong>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<IEventProcessor>(),
                    It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
