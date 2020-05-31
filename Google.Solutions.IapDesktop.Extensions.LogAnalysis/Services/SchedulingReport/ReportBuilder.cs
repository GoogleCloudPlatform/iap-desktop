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
using Google.Apis.Logging.v2;
using Google.Apis.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Logs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    internal interface IReportBuilder
    {
        ushort PercentageDone { get; }
        string BuildStatus { get; }
        Task<ReportArchive> BuildAsync(CancellationToken token);
    }

    class AuditLogDownloader : IReportBuilder, IEventProcessor
    {
        private readonly ushort MaxPeriod = 400;

        private readonly IEnumerable<string> projectIds;
        private readonly InstanceSetHistoryBuilder builder;

        private readonly ComputeService computeService;
        private readonly LoggingService loggingService;

        public AuditLogDownloader(
            IAuthorizationAdapter authService, 
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

            this.projectIds = projectIds;

            this.builder = new InstanceSetHistoryBuilder(startDate, now);

            // TODO: move to IAuthzServuce
            var assemblyName = typeof(AuditLogDownloader).Assembly.GetName();

            this.computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = authService.Authorization.Credential,
                ApplicationName = $"{assemblyName.Name}/{assemblyName.Version}"
            });

            this.loggingService = new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = authService.Authorization.Credential,
                ApplicationName = $"{assemblyName.Name}/{assemblyName.Version}"
            });
        }

        public ushort PercentageDone { get; private set; } = 0;

        public string BuildStatus { get; private set; } = string.Empty;

        public async Task<ReportArchive> BuildAsync(CancellationToken token)
        {
            this.PercentageDone = 5;
            this.BuildStatus = "Analyzing current state...";

            foreach (var projectId in this.projectIds)
            {
                // TODO: pass cancellation token
                await this.builder.AddExistingInstances(
                    computeService.Instances,
                    computeService.Disks,
                    projectId);
            }

            this.PercentageDone = 10;
            this.BuildStatus = $"Analyzing changes done since {this.builder.StartDate:d}...";

            await loggingService.Entries.ListInstanceEventsAsync(
                this.projectIds,
                this.builder.StartDate,
                this,
                token);

            this.PercentageDone = 90;
            this.BuildStatus = "Finalizing report...";

            var archive = ReportArchive.FromInstanceSetHistory(this.builder.Build());

            // TODO: pass cancellation token
            await archive.LoadLicenseAnnotationsAsync(computeService.Images);

            return archive;
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
            this.PercentageDone = (ushort)(10.0 + 80.0 * daysProcessed / 
                (this.builder.EndDate - this.builder.StartDate).TotalDays);

            this.builder.Process(e);
        }
    }
}
