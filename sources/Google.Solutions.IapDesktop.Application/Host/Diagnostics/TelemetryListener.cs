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
    internal class TelemetryListener : EventListener
    {
        private bool enabled;
        private readonly MeasurementSession session;
        private readonly IMeasurementClient client;

        public TelemetryListener(
            IMeasurementClient client,
            IInstall install)
        {
            this.client = client.ExpectNotNull(nameof(client));
            install.ExpectNotNull(nameof(install));

            this.session = new MeasurementSession(install.UniqueId);
        }

        private void Collect(
            string eventName,
            IDictionary<string, string> parameters)
        {
            //
            // Force call to be performed on a thread pool thread.
            //
            _ = Task.Run(async () =>
            {
                try
                {
                    await this.client
                        .CollectEventAsync(
                            this.session,
                            eventName,
                            parameters,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ApplicationTraceSource.Log.TraceError(e);
                }
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

            if (eventData.EventSource == ApplicationEventSource.Log)
            {
                switch (eventData.EventId)
                {
                    case 1:// TODO: use constant
                        Collect(
                            "app_command",
                            eventData.PayloadNames
                                .Zip(eventData.Payload, (n, v) => new KeyValuePair<string, string>(n, v?.ToString()))
                                .ToDictionary());
                        break;
                }
            }
        }
    }
}
