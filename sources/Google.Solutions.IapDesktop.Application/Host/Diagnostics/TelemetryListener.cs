using Google.Solutions.Apis.Analytics;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Host.Diagnostics
{
    /// <summary>
    /// Listens to selected ETW events and reports them as
    /// Measurements to Google Analytics.
    /// </summary>
    public class TelemetryListener : EventListener // TODO: test
    {
        private bool enabled;
        private readonly MeasurementSession session;
        private readonly IMeasurementClient client;
        private readonly QueueUserWorkItem queueUserWorkItem;

        public delegate bool QueueUserWorkItem(WaitCallback callback);

        public TelemetryListener(
            IMeasurementClient client,
            IInstall install,
            QueueUserWorkItem queueUserWorkItem)
        {
            this.client = client.ExpectNotNull(nameof(client));
            this.queueUserWorkItem = queueUserWorkItem;
            install.ExpectNotNull(nameof(install));

            this.session = new MeasurementSession(install.UniqueId);
        }

        public TelemetryListener(
            IMeasurementClient client,
            IInstall install)
            : this(
                  client,
                  install,
                  ThreadPool.QueueUserWorkItem)
        {
        }

        private void Collect(
            string eventName,
            IDictionary<string, string> parameters)
        {
            //
            // Force call to be performed on a thread pool thread.
            //
            this.queueUserWorkItem(_ =>
            {
                _ = this.client
                    .CollectEventAsync(
                        this.session,
                        eventName,
                        parameters,
                        CancellationToken.None)
                    .ContinueWith(
                        t => ApplicationTraceSource.Log.TraceError(t.Exception),
                        TaskContinuationOptions.NotOnFaulted);
            });
        }

        public bool Enabled
        {
            get => this.enabled;
            set
            {
                if (value)
                {
                    //
                    // Make sure the source is enabled.
                    //
                    EnableEvents(ApplicationEventSource.Log, EventLevel.Verbose); // TODO: use keyword
                }

                this.enabled = value;
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!this.enabled)
            {
                return;
            }

            //
            // Relay relevant events.
            //

            if (eventData.EventSource == ApplicationEventSource.Log)
            {
                switch (eventData.EventId)
                {
                    case 1:// TODO: use constant
                        Collect(
                            "app_command",
                            eventData.PayloadNames
                                .Zip(
                                    eventData.Payload, 
                                    (n, v) => new KeyValuePair<string, string>(n, v?.ToString()))
                                .ToDictionary());
                        break;
                }
            }
        }
    }
}
