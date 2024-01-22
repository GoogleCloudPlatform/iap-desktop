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

using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Google.Solutions.Apis.Analytics
{
    /// <summary>
    /// A Google analytics measurement session. 
    /// </summary>
    public class MeasurementSession
    {
        private long lastEventMsec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Unique session Id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Client ID, uniquely identifies the device/client.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Client ID, uniquely identifies the user, potentially across
        /// multiple devices.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Custom properties about the user, optional.
        /// </summary>
        public IDictionary<string, string> UserProperties { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Enable or disable debug mode.
        /// </summary>
        public bool DebugMode { get; set; } = false;

        internal IEnumerable<KeyValuePair<string, string>> GenerateParameters()
        {
            //
            // Calculate time (in milliseconds) since the last
            // event was sent. This time is counted as "engagement time".
            //
            // For details about session tracking, see
            // https://developers.google.com/analytics/devguides/collection/protocol/ga4/sending-events.
            //
            var nowMsec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var timeSinceLastEventMsec =
                nowMsec - Interlocked.Exchange(ref this.lastEventMsec, nowMsec);

            yield return new KeyValuePair<string, string>(
                "engagement_time_msec",
                timeSinceLastEventMsec.ToString());

            yield return new KeyValuePair<string, string>("session_id", this.Id.ToString());

            if (this.DebugMode)
            {
                yield return new KeyValuePair<string, string>("debug_mode", "true");
            }
        }

        public MeasurementSession(string clientId)
        {
            this.Id = Guid.NewGuid();
            this.ClientId = clientId.ExpectNotEmpty(nameof(clientId));
        }
    }
}
