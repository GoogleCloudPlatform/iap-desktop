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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.UsageReport
{
    internal interface IReportBuilder
    {
        ushort PercentageDone { get; }
        string BuildStatus { get; }
        Task<ReportArchive> BuildAsync(CancellationToken token);
    }

    [Flags]
    internal enum AuditLogSources
    {
        Api,
        StorageExport
    }

    internal class ReportBuilder : IReportBuilder, IEventProcessor
    {
        private readonly ushort MaxPeriod = 400;

        private readonly IEnumerable<string> projectIds;
        private readonly AuditLogSources sources;
        private readonly InstanceSetHistoryBuilder builder;

        private readonly IAuditLogAdapter auditLogAdapter;
        private readonly IAuditLogStorageSinkAdapter auditExportAdapter;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public ReportBuilder(
            IAuditLogAdapter auditLogAdapter,
            IAuditLogStorageSinkAdapter auditExportAdapter,
            IComputeEngineAdapter computeEngineAdapter,
            AuditLogSources sources,
            IEnumerable<string> projectIds,
            DateTime startDate)
        {
            var now = DateTime.UtcNow;
            if (startDate >= now)
            {
                throw new ArgumentException("Invalid start date");
            }
            else if ((now - startDate).TotalDays > MaxPeriod)
            {
                throw new ArgumentException("Start date is too far in the past");
            }

            this.sources = sources;
            this.projectIds = projectIds;
            this.auditLogAdapter = auditLogAdapter;
            this.auditExportAdapter = auditExportAdapter;
            this.computeEngineAdapter = computeEngineAdapter;

            this.builder = new InstanceSetHistoryBuilder(startDate, now);
        }

        public ushort PercentageDone { get; private set; } = 0;

        public string BuildStatus { get; private set; } = string.Empty;

        public async Task<ReportArchive> BuildAsync(
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(this.projectIds, sources))
            {
                this.PercentageDone = 5;
                this.BuildStatus = "1. Analyzing current inventory...";

                //
                // (1) Take inventory of what's there currently (pretty fast).
                //
                foreach (var projectId in this.projectIds)
                {
                    //
                    // Load disks.
                    //
                    // NB. Instances.list returns the disks associated with each
                    // instance, but lacks the information about the source image.
                    // Therefore, we load disks first and then join the data.
                    //
                    var disks = await this.computeEngineAdapter.ListDisksAsync(
                            projectId,
                            cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // Load nodes.
                    //
                    var nodes = await this.computeEngineAdapter.ListNodesAsync(
                            projectId,
                            cancellationToken)
                        .ConfigureAwait(false);

                    //
                    // Load instances.
                    //
                    var instances = await this.computeEngineAdapter.ListInstancesAsync(
                            projectId,
                            cancellationToken)
                        .ConfigureAwait(false);

                    this.builder.AddExistingInstances(
                        instances,
                        nodes,
                        disks,
                        projectId);
                }

                //
                // (2) Try to use GCS exports for as many projects as we can (reasonably fast).
                //

                var pendingProjectIds = new HashSet<string>(this.projectIds);

                if (this.sources.HasFlag(AuditLogSources.StorageExport))
                {
                    this.PercentageDone = 10;
                    this.BuildStatus += $"\n2. Loading Cloud Storage exports...";

                    foreach (var projectId in this.projectIds)
                    {
                        // NB. Listing sinks does not requir any more permissions than
                        // querying the API.

                        var auditLogExportBucket = await this.auditExportAdapter
                            .FindCloudStorageExportBucketForAuditLogsAsync(
                                projectId,
                                this.builder.StartDate,
                                cancellationToken)
                            .ConfigureAwait(false);

                        if (auditLogExportBucket != null)
                        {
                            TraceSources.IapDesktop.TraceVerbose(
                                "Found storage export buckets for {0}: {1}",
                                projectId,
                                auditLogExportBucket);

                            this.BuildStatus += $"\n    - {auditLogExportBucket}...";
                            await this.auditExportAdapter.ProcessInstanceEventsAsync(
                                    auditLogExportBucket,
                                    this.builder.StartDate,
                                    this.builder.EndDate,
                                    this,
                                    cancellationToken)
                                .ConfigureAwait(false);

                            // Check this project off.
                            pendingProjectIds.Remove(projectId);
                        }
                    }
                }

                //
                // (3) Use API for remaining projects (very slow).
                //

                if (pendingProjectIds.Any() && this.sources.HasFlag(AuditLogSources.Api))
                {
                    this.PercentageDone = 30;
                    this.BuildStatus += $"\n3. Querying Audit Log API...";

                    TraceSources.IapDesktop.TraceVerbose(
                        "Querying audit log API for remaining projects {0}",
                        string.Join(", ", pendingProjectIds));

                    await this.auditLogAdapter.ProcessInstanceEventsAsync(
                            pendingProjectIds,
                            null,  // all zones.
                            null,  // all instances.
                            this.builder.StartDate,
                            this,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                //
                // (4) Finish up.
                //

                this.PercentageDone = 90;
                this.BuildStatus += "\n4. Finalizing report...";

                var archive = ReportArchive.FromInstanceSetHistory(this.builder.Build());

                await archive.LoadLicenseAnnotationsAsync(
                    this.computeEngineAdapter,
                    cancellationToken).ConfigureAwait(false);

                return archive;
            }
        }

        //---------------------------------------------------------------------
        // IEventProcessor.
        //---------------------------------------------------------------------

        EventOrder IEventProcessor.ExpectedOrder => this.builder.ExpectedOrder;

        IEnumerable<string> IEventProcessor.SupportedSeverities => this.builder.SupportedSeverities;

        IEnumerable<string> IEventProcessor.SupportedMethods => this.builder.SupportedMethods;

        void IEventProcessor.Process(EventBase e)
        {
            // Calculate percentage done by checking how many days we have processed
            // already. That is not very precise as the number of events tends to vary,
            // but it is good enough to give the user a clue.

            var daysProcessed = (this.builder.EndDate - e.Timestamp).TotalDays;
            this.PercentageDone = (ushort)(30.0 + 60.0 * daysProcessed /
                (this.builder.EndDate - this.builder.StartDate).TotalDays);

            this.builder.Process(e);
        }
    }
}
