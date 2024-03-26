//
// Copyright 2024 Google LLC
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
using Google.Solutions.Apis.Analytics;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Diagnostics
{
    /// <summary>
    /// Listens to selected ETW events and reports them as
    /// Measurements to Google Analytics.
    /// </summary>
    public interface ITelemetryCollector
    {
        /// <summary>
        /// Enable or disable telemetry collection.
        /// </summary>
        bool Enabled { get; set; }
    }

    public class TelemetryCollector : EventListener, ITelemetryCollector
    {
        private bool enabled;
        private readonly MeasurementSession session;
        private readonly IMeasurementClient client;
        private readonly QueueUserWorkItem queueUserWorkItem;
        private readonly IReadOnlyCollection<KeyValuePair<string, string>> defaultParameters;

        public delegate bool QueueUserWorkItem(WaitCallback callback);

        internal TelemetryCollector(
            IMeasurementClient client,
            IInstall install,
            IReadOnlyCollection<KeyValuePair<string, string>> defaultParameters,
            QueueUserWorkItem queueUserWorkItem)
        {
            this.client = client.ExpectNotNull(nameof(client));
            this.defaultParameters = defaultParameters.ExpectNotNull(nameof(defaultParameters));
            this.queueUserWorkItem = queueUserWorkItem.ExpectNotNull(nameof(queueUserWorkItem));
            install.ExpectNotNull(nameof(install));

            //
            // Create a new session and associate all subsequent
            // measurements with that session.
            //
            // To identify "returning users", we use the unique ID
            // of the installation as client ID. Note that:
            //
            // *  The ID is always the same for this user/machine.
            // *  The ID is not associated with a user's Google identity
            //    in any way, so telemetry data can't be de-anonymized later.
            // 
            this.session = new MeasurementSession(install.UniqueId);
        }

        public TelemetryCollector(
            IMeasurementClient client,
            IInstall install,
            IReadOnlyCollection<KeyValuePair<string, string>> defaultParameters)
            : this(
                  client,
                  install,
                  defaultParameters,
                  ThreadPool.QueueUserWorkItem)
        {
        }

        private void Collect(
            string eventName,
            EventWrittenEventArgs eventData)
        {
            var parameters = eventData.PayloadNames
                .Zip(
                    eventData.Payload,
                    (n, v) => new KeyValuePair<string, string>(n, v?.ToString() ?? string.Empty));

            //
            // Force call to be performed on a thread pool thread so that we never
            // block the caller.
            //
            this.queueUserWorkItem(_ =>
            {
                _ = this.client.CollectEventAsync(
                        this.session,
                        eventName,
                        this.defaultParameters.Concat(parameters),
                        CancellationToken.None)
                    .ContinueWith(
                        t => ApplicationTraceSource.Log.TraceError(t.Exception),
                        TaskContinuationOptions.OnlyOnFaulted);
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
                    EnableEvents(ApiEventSource.Log, EventLevel.Informational);
                    EnableEvents(ApplicationEventSource.Log, EventLevel.Informational);
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
            // NB. When adding new events, consider the requirements in
            // https://support.google.com/analytics/answer/12229021
            //

            if (eventData.EventSource == ApplicationEventSource.Log)
            {
                switch (eventData.EventId)
                {
                    case ApplicationEventSource.CommandExecutedId:
                        Collect("app_cmd_executed", eventData);
                        break;

                    case ApplicationEventSource.CommandFailedId:
                        Collect("app_cmd_failed", eventData);
                        break;
                }
            }
            else if (eventData.EventSource == ApiEventSource.Log)
            {
                switch (eventData.EventId)
                {
                    case ApiEventSource.OfflineCredentialActivatedId:
                        Collect("app_auth_offline", eventData);
                        break;

                    case ApiEventSource.OfflineCredentialActivationFailedId:
                        Collect("app_auth_offline_failed", eventData);
                        break;

                    case ApiEventSource.AuthorizedId:
                        Collect("app_auth", eventData);
                        break;
                }
            }
        }
    }
}
