using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GcsObject = Google.Apis.Storage.v1.Data.Object;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters
{
    public interface IAuditLogStorageSinkAdapter
    {
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

        public Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            string bucket,
            string objectName,
            CancellationToken cancellationToken)
        {
            // TODO
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<EventBase>> ListInstanceEventsAsync(
            string bucket,
            IEnumerable<string> objectNames,
            CancellationToken cancellationToken)
        {
            var events = new List<EventBase>();

            foreach (var objectName in objectNames)
            {
                events.AddRange(await ListInstanceEventsAsync(
                        bucket, 
                        objectName,
                        cancellationToken)
                    .ConfigureAwait(false));
            }

            return events;
        }

        public async Task ProcessInstanceEventsAsync(
            string bucket,
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
                // The bucket containing exported audit logs has the following structure:
                // - cloudaudit.googleapis.com/activity/yyyy/mm/dd/hh:00:00_hh:MM:ss_S0.json
                // - cloudaudit.googleapis.com/system_event/yyyy/mm/dd/hh:00:00_hh:MM:ss_S0.json
                //
                // Note that there might be other, unrelated objects in the same bucket.
                // Because somebody might have copied the objects from one bucket to another,
                // it's best not to rely on the creation timestamp to determine which day
                // the exported events relate to.
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
                                bucket,
                                objectsForDay.Select(o => o.Name),
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
