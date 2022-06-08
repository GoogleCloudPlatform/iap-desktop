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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events
{
    public abstract class EventBase
    {
        public abstract EventCategory Category { get; }
        public LogRecord LogRecord { get; }

        public DateTime Timestamp => this.LogRecord.Timestamp;

        public string Severity => this.LogRecord.Severity;

        public string PrincipalEmail => this.LogRecord.ProtoPayload?
            .AuthenticationInfo?
            .PrincipalEmail;

        public StatusInfo Status => this.LogRecord.ProtoPayload?.Status?.Message != null
            ? this.LogRecord.ProtoPayload?.Status
            : null;

        public string SourceHost =>
            this.LogRecord.ProtoPayload.RequestMetadata?.Value<string>("callerIp");
        public string UserAgent =>
            this.LogRecord.ProtoPayload.RequestMetadata?.Value<string>("callerSuppliedUserAgent");

        public string UserAgentShort
        {
            get
            {
                var userAgent = this.UserAgent ?? "(unknown agent)";
                int parenthesis = userAgent.IndexOf('(');
                if (parenthesis > 0)
                {
                    // Strip version and details.
                    return userAgent.Substring(0, parenthesis).Trim();
                }
                else
                {
                    return userAgent;
                }
            }
        }

        public IEnumerable<AccessLevelLocator> AccessLevels
        {
            get
            {
                var accessLevels = this.LogRecord.ProtoPayload?
                    .RequestMetadata?["requestAttributes"]?["auth"]?["accessLevels"];
                if (accessLevels != null)
                {
                    return accessLevels.Values<string>()
                        .Select(AccessLevelLocator.FromString);
                }
                else
                {
                    return Enumerable.Empty<AccessLevelLocator>();
                }
            }
        }

        public string DeviceState
            => this.LogRecord
                .ProtoPayload?
                .Metadata?
                .Value<string>("device_state")
                .NullIfEmpty();

        public string DeviceId
            => this.LogRecord
                .ProtoPayload?
                .Metadata?
                .Value<string>("device_id")
                .NullIfEmpty();

        public abstract string Message { get; }

        protected EventBase(LogRecord logRecord)
        {
            this.LogRecord = logRecord;
        }

        public override string ToString()
        {
            return $"{this.Timestamp} {this.Severity} {this.Message}";
        }
    }

    public enum EventCategory
    {
        // NB. Categories are contextual and do not map 1:1 to admin 
        // activity/system/data access events!

        Unknown,

        /// <summary>
        ///  Events that affect the lifecycle of a VM, initiated by the user
        /// </summary>
        Lifecycle,

        /// <summary>
        /// Events that affect the lifecycle of a VM, initiated by system
        /// </summary>
        System,

        /// <summary>
        /// Other, security-relevant events 
        /// </summary>
        Access
    }
}
