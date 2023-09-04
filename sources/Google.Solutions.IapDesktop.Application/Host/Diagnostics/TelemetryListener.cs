//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Analytics;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
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
    public class TelemetryListener : EventListener 
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
            EventWrittenEventArgs eventData)
        {
            var parameters = eventData.PayloadNames
                .Zip(
                    eventData.Payload,
                    (n, v) => new KeyValuePair<string, string>(n, v?.ToString()))
                .ToDictionary();

            //
            // Force call to be performed on a thread pool thread so that we never
            // block the caller.
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
                    case ApplicationEventSource.CommandExecutedId:
                        Collect("app_cmd_executed", eventData);
                        break;

                    case ApplicationEventSource.CommandFailedId:
                        Collect("app_cmd_failed", eventData);
                        break;
                }
            }
        }
    }
}
