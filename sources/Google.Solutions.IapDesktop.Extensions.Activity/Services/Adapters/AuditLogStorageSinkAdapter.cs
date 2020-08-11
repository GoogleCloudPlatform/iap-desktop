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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters
{
    public interface IAuditLogStorageSinkAdapter
    {
        Task<string> FindCloudStorageExportBucketForAuditLogsAsync(
            string projectId,
            DateTime earliestDateRequired,
            CancellationToken cancellationToken);

        Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            StorageObjectLocator locator,
            CancellationToken cancellationToken);

        Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
           IEnumerable<StorageObjectLocator> locators,
           CancellationToken cancellationToken);

        Task ProcessInstanceEventsAsync(
            string bucket,
            DateTime startTime,
            DateTime endTime,
            IEventProcessor processor,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IAuditLogStorageSinkAdapter))]
    public class AuditLogStorageSinkAdapter : IAuditLogStorageSinkAdapter
    {
        private const string ActivityPrefix = "cloudaudit.googleapis.com/activity/";
        private const string SystemEventPrefix = "cloudaudit.googleapis.com/system_event/";

        private readonly IStorageAdapter storageAdapter;
        private readonly IAuditLogAdapter auditLogAdapter;

        public AuditLogStorageSinkAdapter(
            IStorageAdapter storageAdapter,
            IAuditLogAdapter auditLogAdapter)
        {
            this.storageAdapter = storageAdapter;
            this.auditLogAdapter = auditLogAdapter;
        }

        private static DateTime? DateFromObjectName(string name)
        {
            var match = new Regex(
                    "cloudaudit.googleapis.com/.*/"+
                    "([0-9]{4})/([0-9]{2})/([0-9]{2})/[0-9]{2}:[0-9]{2}:[0-9]{2}_"+
                    "[0-9]{2}:[0-9]{2}:[0-9]{2}_.*.json")
                .Match(name);

            if (match.Success)
            {
                return new DateTime(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value),
                    0,
                    0,
                    0,
                    DateTimeKind.Utc).Date;
            }
            else
            {
                return null;
            }
        }

        private async Task<IEnumerable<StorageObjectLocator>> FindAuditLogExportObjects(
            string bucket,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(bucket))
            {
                //
                // The object names for audit logs follow this convention:
                // - cloudaudit.googleapis.com/activity/yyyy/mm/dd/<from-time>_<to-time>_S0.json
                // - cloudaudit.googleapis.com/system_event/yyyy/mm/dd/<from-time>_<to-time>_S0.json
                // with <from-time> and <to-time> formatted as hh:MM:ss
                // 
                // Note that there might be other, unrelated objects in the same bucket.
                //  

                var objectsByBucket = await this.storageAdapter.ListObjectsAsync(
                        bucket,
                        cancellationToken)
                    .ConfigureAwait(false);

                return objectsByBucket
                    .Where(o => o.Name.StartsWith(ActivityPrefix) || 
                                o.Name.StartsWith(SystemEventPrefix))
                    .Select(o => new StorageObjectLocator(o.Bucket, o.Name));
            }
        }

        internal async Task<IDictionary<DateTime, IEnumerable<StorageObjectLocator>>> FindAuditLogExportObjectsGroupedByDay(
            string bucket,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken)
        {
            Debug.Assert(startTime.Kind == DateTimeKind.Utc);
            Debug.Assert(endTime.Kind == DateTimeKind.Utc);

            if (startTime.Date > endTime.Date)
            {
                throw new ArgumentException(nameof(startTime));
            }

            using (TraceSources.IapDesktop.TraceMethod().WithParameters(bucket, startTime))
            {
                var exportObjects = await FindAuditLogExportObjects(
                        bucket, 
                        cancellationToken)
                    .ConfigureAwait(false);

                //
                // NB. Because somebody might have copied the objects from one bucket to another,
                // it's best not to rely on the creation timestamp to determine which day
                // the exported events relate to and instead extract that information from
                // the object name.
                //

                return exportObjects
                    .Select(locator => new
                    {
                        Locator = locator,
                        Date = DateFromObjectName(locator.ObjectName)
                    })
                    .Where(rec => rec.Date != null && rec.Date >= startTime && rec.Date <= endTime)
                    .GroupBy(rec => rec.Date)   // Trim time portion
                    .ToDictionary(
                        group => group.Key.Value,
                        group => group.Select(g => g.Locator));
            }
        }

        //---------------------------------------------------------------------
        // IAuditLogStorageSinkAdapter.
        //---------------------------------------------------------------------

        public async Task<string> FindCloudStorageExportBucketForAuditLogsAsync(
            string projectId,
            DateTime earliestDateRequired,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(
                projectId,
                earliestDateRequired))
            {
                var allCloudStorageSinks = await this.auditLogAdapter
                    .ListCloudStorageSinksAsync(projectId, cancellationToken)
                    .ConfigureAwait(false);

                //
                // Given the list of export sinks, find a sink that:
                //
                // (1) Has been created before the cutoff date (if it's newer,
                //     then it won't have all the data we need.
                // (2) Contains audit logs (as opposed to other kinds of logs),
                //     Determining if a sink handles audit logs could be done
                //     by inspecting the filter string - but it'd difficult to
                //     do this without having a proper parser for the filter
                //     syntax. An easier way is to simply inspect if the 
                //     associated bucket has any audit log exports or not.
                //
                //     This has the additional benefit of verifying that the
                //     bucket is actually accessible by the user.
                //

                foreach (var sink in allCloudStorageSinks)
                {
                    if (DateTime.Parse((string)sink.CreateTime) > earliestDateRequired)
                    {
                        // Sink created after the ctoff date, so it will not
                        // have sufficient history. 
                        continue;
                    }

                    // Accessing the bucket requires permissions which
                    // the user might not have for some of the projects.
                    var exportObjects = Enumerable.Empty<StorageObjectLocator>();
                    try
                    {
                        exportObjects = await FindAuditLogExportObjects(
                                sink.GetDestinationBucket(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (ResourceAccessDeniedException)
                    {
                        TraceSources.IapDesktop.TraceWarning(
                            "Found storage export bucket {0} for project {1}, but cannot access it",
                            sink.GetDestinationBucket(),
                            projectId);
                    }

                    if (!exportObjects.Any())
                    {
                        // Bucket does not contain any audit log export objects.
                        continue;
                    }

                    // We have a winner.
                    return sink.GetDestinationBucket();
                }

                // No suitable sink found.
                TraceSources.IapDesktop.TraceVerbose(
                    "No GCS export bucket for audit log data found for project {0}",
                    projectId);
                return null;
            }
        }

        public async Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            StorageObjectLocator locator,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(locator))
            {
                var events = new List<EventBase>();

                using (var stream = await this.storageAdapter.DownloadObjectToMemoryAsync(
                        locator,
                        cancellationToken)
                    .ConfigureAwait(false))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // The file contains a sequence of JSON structures, separated
                    // by a newline.

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // The file might contain empty lines.
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            events.Add(EventFactory.FromRecord(LogRecord.Deserialize(line)));
                        }
                    }
                }

                return events;
            }
        }

        public async Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            IEnumerable<StorageObjectLocator> locators,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(
                string.Join(", ", locators)))
            {
                var resultsByLocator = await locators.SelectParallelAsync(
                    locator => ListInstanceEventsAsync(
                        locator,
                        cancellationToken))
                    .ConfigureAwait(false);

                return resultsByLocator.SelectMany(r => r);
            }
        }

        public async Task ProcessInstanceEventsAsync(
            string bucket,
            DateTime startTime,
            DateTime endTime,
            IEventProcessor processor,
            CancellationToken cancellationToken)
        {
            Debug.Assert(startTime.Kind == DateTimeKind.Utc);

            if (startTime.Date > DateTime.UtcNow.Date)
            {
                return;
            }

            using (TraceSources.IapDesktop.TraceMethod().WithParameters(bucket, startTime))
            {
                var severitiesWhitelist = processor.SupportedSeverities.ToHashSet();
                var methodsWhitelist = processor.SupportedMethods.ToHashSet();

                var objectsByDay = await FindAuditLogExportObjectsGroupedByDay(
                        bucket,
                        startTime,
                        DateTime.UtcNow,
                        cancellationToken)
                    .ConfigureAwait(false);

                var days = (processor.ExpectedOrder == EventOrder.OldestFirst)
                    ? DateRange.DayRange(startTime, endTime.Date, 1)
                    : DateRange.DayRange(endTime, startTime.Date, -1);

                foreach (var day in days)
                {
                    TraceSources.IapDesktop.TraceVerbose("Processing {0}", day);

                    //
                    // Grab the objects for this day (typically 2, one activity and one system event).
                    // Each object is (probably) sorted in ascending order, but we need a global,
                    // descending order. Therefore, download everything for that day, merge, and
                    // sort it before processing each event.
                    //

                    if (objectsByDay.TryGetValue(day, out IEnumerable<StorageObjectLocator> objectsForDay))
                    {
                        TraceSources.IapDesktop.TraceVerbose(
                            "Processing {1} export objects for {0}", day, objectsForDay.Count());

                        var eventsForDay = await ListInstanceEventsAsync(
                                objectsForDay,
                                cancellationToken)
                            .ConfigureAwait(false);

                        // Merge and sort events.
                        var eventsForDayOrdered = (processor.ExpectedOrder == EventOrder.OldestFirst)
                            ? eventsForDay.OrderBy(e => e.Timestamp)
                            : eventsForDay.OrderByDescending(e => e.Timestamp);

                        foreach (var e in eventsForDayOrdered)
                        {
                            if (e.LogRecord?.ProtoPayload?.MethodName != null &&
                                methodsWhitelist.Contains(e.LogRecord.ProtoPayload.MethodName) &&
                                severitiesWhitelist.Contains(e.Severity))
                            {
                                processor.Process(e);
                            }
                        }
                    }
                    else
                    {
                        TraceSources.IapDesktop.TraceWarning("No export objects found for {0}", day);
                    }
                }
            }
        }
    }
}
