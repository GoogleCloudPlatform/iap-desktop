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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Apis.Analytics;
using Google.Solutions.IapDesktop.Application.Host;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Diagnostics
{
    /// <summary>
    /// Telemetry log that relays events to Google Analytics.
    /// </summary>
    /// <remarks>
    /// For cusom parameters, consider the requirements in
    /// https://support.google.com/analytics/answer/12229021
    /// </remarks>
    public class AnalyticsLog : TelemetryLog
    {
        /// <summary>
        /// Default parameters that are added to each event.
        /// </summary>
        private readonly IReadOnlyCollection<KeyValuePair<string, object>> defaultParameters;

        private readonly IMeasurementClient client;
        private readonly MeasurementSession session;

        private void WriteCore(
            string eventName,
            IEnumerable<KeyValuePair<string, object>> parameters)
        {
            //
            // Combine default parameters with caller-supplied
            // parameters.
            //
            var allParameters = this
                .defaultParameters
                .Concat(parameters)
                .Select(kvp => new KeyValuePair<string, string>(
                    kvp.Key,
                    kvp.Value.ToString()));

            _ = this.client
                .CollectEventAsync(
                    this.session,
                    eventName,
                    allParameters,
                    CancellationToken.None)
                .ContinueWith(
                    t => ApplicationTraceSource.Log.TraceError(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        public AnalyticsLog(
            IMeasurementClient client,
            IInstall install,
            IReadOnlyCollection<KeyValuePair<string, object>> defaultParameters,
            bool isAsync = true)
        {
            this.defaultParameters = defaultParameters;
            this.IsAsync = isAsync;

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
            this.client = client;
        }

        /// <summary>
        /// Indicates if events are relayed asynchronously.
        /// </summary>
        public bool IsAsync { get; }

        /// <inheritdoc/>>
        public override void Write(
            string eventName,
            IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (!this.Enabled)
            {
                //
                // Drop event.
                //
            }
            else if (this.IsAsync)
            {
                //
                // Force call to be performed on a thread pool thread
                // so that we don't block the caller.
                //
                ThreadPool.QueueUserWorkItem(
                    _ => WriteCore(eventName, parameters));
            }
            else
            {
                WriteCore(eventName, parameters);
            }
        }
    }
}
