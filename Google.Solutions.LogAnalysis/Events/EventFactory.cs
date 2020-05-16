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

using Google.Solutions.LogAnalysis.Events.Lifecycle;
using Google.Solutions.LogAnalysis.Events.System;
using Google.Solutions.LogAnalysis.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Google.Solutions.LogAnalysis.Events
{
    public static class EventFactory
    {
        private readonly static IDictionary<string, Func<LogRecord, EventBase>> lifecycleEvents
            = new Dictionary<string, Func<LogRecord, EventBase>>()
            {
                { DeleteInstanceEvent.Method, rec => new DeleteInstanceEvent(rec) },
                { InsertInstanceEvent.Method, rec => new InsertInstanceEvent(rec) },
                { InsertInstanceEvent.BetaMethod, rec => new InsertInstanceEvent(rec) },
                { StartInstanceEvent.Method, rec => new StartInstanceEvent(rec) },
                { StartWithEncryptionKeyEvent.BetaMethod, rec => new StartWithEncryptionKeyEvent(rec) },
                { StopInstanceEvent.Method, rec => new StopInstanceEvent(rec) },
                { StopInstanceEvent.BetaMethod, rec => new StopInstanceEvent(rec) },
                { ResetInstanceEvent.Method, rec => new ResetInstanceEvent(rec) },

                // Some beta events omitted (based on audit_log_services.ts)
            };

        private readonly static IDictionary<string, Func<LogRecord, EventBase>> systemEvents
            = new Dictionary<string, Func<LogRecord, EventBase>>()
            {
                { AutomaticRestartEvent.Method, rec => new AutomaticRestartEvent(rec) },
                { GuestTerminateEvent.Method, rec => new GuestTerminateEvent(rec) },
                { HostErrorEvent.Method, rec => new HostErrorEvent(rec) },
                { InstanceManagerHaltForRestartEvent.Method, rec => new InstanceManagerHaltForRestartEvent(rec) },
                { InstancePreemptedEvent.Method, rec => new InstancePreemptedEvent(rec) },
                { InstanceResetEvent.Method, rec => new InstanceResetEvent(rec) },
                { MigrateOnHostMaintenanceEvent.Method, rec => new MigrateOnHostMaintenanceEvent(rec) },
                { NotifyInstanceLocationEvent.Method, rec => new NotifyInstanceLocationEvent(rec) },
                { RecreateInstanceEvent.Method, rec => new RecreateInstanceEvent(rec) },
                { TerminateOnHostMaintenanceEvent.Method, rec => new TerminateOnHostMaintenanceEvent(rec) }

                // Some more esoteric event types omitted (based on InstanceEventInfo.java).
            };

        public static IEnumerable<string> LifecycleEventMethods => lifecycleEvents.Keys;
        public static IEnumerable<string> SystemEventMethods => systemEvents.Keys;

        public static EventBase FromRecord(LogRecord record)
        {
            if (!record.IsValidAuditLogRecord)
            {
                throw new ArgumentException("Not a valid audit log record");
            }
            else if (record.IsSystemEvent)
            {
                if (systemEvents.TryGetValue(record.ProtoPayload.MethodName, out var sysFunc))
                {
                    return sysFunc(record);
                }
                else
                {
                    // There are some less common/more esoteric system events that do not
                    // have a wrapper class. Map these to GenericSystemEvent.
                    return new GenericSystemEvent(record);
                }
            }
            else if (record.IsActivityEvent)
            {
                if (lifecycleEvents.TryGetValue(record.ProtoPayload.MethodName, out var lcFunc))
                {
                    return lcFunc(record);
                }
            }

            // The list of activity event types is incomplete any might grow stale over time,
            // so ensure to fail open.
            return new UnknownEvent(record);
        }

        public static EventBase ToEvent(this LogRecord record) => FromRecord(record);
    }
}
