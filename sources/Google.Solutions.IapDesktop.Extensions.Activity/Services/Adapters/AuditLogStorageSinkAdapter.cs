using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
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
using GcsObject = Google.Apis.Storage.v1.Data.Object;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters
{
    public interface IAuditLogStorageSinkAdapter
    {
        Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            StorageObjectLocator locator,
            CancellationToken cancellationToken);

        Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
           IEnumerable<StorageObjectLocator> locators,
           CancellationToken cancellationToken);

        Task ProcessInstanceEventsAsync(
            string bucket,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IAuditLogStorageSinkAdapter))]
    public class AuditLogStorageSinkAdapter : IAuditLogStorageSinkAdapter
    {
        private const string ActivityPrefix = "cloudaudit.googleapis.com/activity/";
        private const string SystemEventPrefix = "cloudaudit.googleapis.com/system_event/";

        private readonly IStorageAdapter storageAdapter;

        public AuditLogStorageSinkAdapter(IStorageAdapter storageAdapter)
        {
            this.storageAdapter = storageAdapter;
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

        //---------------------------------------------------------------------
        // IAuditLogStorageSinkAdapter.
        //---------------------------------------------------------------------

        public async Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            StorageObjectLocator locator,
            CancellationToken cancellationToken)
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
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    events.Add(EventFactory.FromRecord(LogRecord.Deserialize(line)));
                }
            }

            return events;
        }

        public async Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            IEnumerable<StorageObjectLocator> locators,
            CancellationToken cancellationToken)
        {
            // Download objects in parallel.
            // TODO: Chunking
            var tasks = new List<Task<IEnumerable<EventBase>>>();
            foreach (var locator in locators)
            {
                tasks.Add(ListInstanceEventsAsync(
                    locators,
                    cancellationToken));
            }

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            return tasks.SelectMany(t => t.Result);
        }

        public async Task ProcessInstanceEventsAsync(
            string bucket,  // TODO: Multiple buckets
            DateTime startTime,
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
                //
                // The object names for audit logs follow this convention:
                // - cloudaudit.googleapis.com/activity/yyyy/mm/dd/hh:00:00_hh:MM:ss_S0.json
                // - cloudaudit.googleapis.com/system_event/yyyy/mm/dd/hh:00:00_hh:MM:ss_S0.json
                //
                // Note that there might be other, unrelated objects in the same bucket.
                // Because somebody might have copied the objects from one bucket to another,
                // it's best not to rely on the creation timestamp to determine which day
                // the exported events relate to and instead extract that information from
                // the object name.
                //  
                var allObjects = await this.storageAdapter
                    .ListObjectsAsync(bucket, cancellationToken)
                    .ConfigureAwait(false);

                var objectsByDay = allObjects
                    .Where(o => o.Name.StartsWith(ActivityPrefix) || o.Name.StartsWith(SystemEventPrefix))
                    .Select(o => new
                    {
                        Object = o,
                        Date = DateFromObjectName(o.Name)
                    })
                    .Where(rec => rec.Date != null)
                    .GroupBy(rec => rec.Date)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(g => g.Object));

                // Start today and go backwards.
                for (var day = DateTime.UtcNow.Date;
                     day >= startTime.Date;
                     day = day.AddDays(-1))
                {
                    TraceSources.IapDesktop.TraceWarning("Processing {0}", day);

                    //
                    // Grab the objects for this day (typically 2, one activity and one system event).
                    // Each object is (probably) sorted in ascending order, but we need a global,
                    // descending order. Therefore, download everything for that day, merge, and
                    // sort it before processing each event.
                    //

                    if (objectsByDay.TryGetValue(day, out IEnumerable<GcsObject> objectsForDay))
                    {
                        TraceSources.IapDesktop.TraceVerbose(
                            "Found {1} export objects for {0}", 
                            day, objectsForDay.Count());

                        var eventsForDay = await ListInstanceEventsAsync(
                                objectsForDay.Select(o => new StorageObjectLocator(bucket, o.Name)),
                                cancellationToken)
                            .ConfigureAwait(false);

                        // Merge and sort events.
                        var eventsForDayOrdered = eventsForDay.OrderByDescending(e => e.Timestamp);

                        foreach (var e in eventsForDayOrdered)
                        {
                            processor.Process(e);
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
