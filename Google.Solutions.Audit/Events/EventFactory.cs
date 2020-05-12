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

using Google.Solutions.Audit.Events.Lifecycle;
using Google.Solutions.Audit.Events.System;
using Google.Solutions.Audit.Records;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Google.Solutions.Audit.Events
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
                { StopInstanceEvent.Method, rec => new StopInstanceEvent(rec) },
                { StopInstanceEvent.BetaMethod, rec => new StopInstanceEvent(rec) },
                { ResetInstanceEvent.Method, rec => new ResetInstanceEvent(rec) },

                // Some beta events omitted (based on audit_log_services.ts)

                // TODO: v1.compute.instances.startWithEncryptionKey
                // TODO: v1.compute.instances.simulateMaintenanceEvent
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
                { StoppedDueToPdDoubleServeEvent.Method, rec => new StoppedDueToPdDoubleServeEvent(rec) },
                { TerminateOnHostMaintenanceEvent.Method, rec => new TerminateOnHostMaintenanceEvent(rec) }

                // Some more esoteric event types omitted (based on InstanceEventInfo.java).

                // TODO: UnknownSystemEvent
            };


        public static EventBase FromRecord(LogRecord record)
        {
            if (lifecycleEvents.TryGetValue(record.ProtoPayload.MethodName, out var lcFunc))
            {
                return lcFunc(record);
            }
            else if (systemEvents.TryGetValue(record.ProtoPayload.MethodName, out var sysFunc))
            {
                return sysFunc(record);
            }
            else
            {
                // The list of event types is incomplete any might grow stale over time,
                // so ensure to fail open.
                return new UnknownEvent(record);
            }
        }

        public static EventBase ToEvent(this LogRecord record) => FromRecord(record);

        public static IEnumerable<EventBase> Read(JsonReader reader)
        {
            //
            // Deserializing everything would be trmendously inefficient.
            // Instead, deserialize objects one by one.
            //

            while (reader.Read())
            {
                // Start of a new object.
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var record = LogRecord.Deserialize(reader);
                    if (record.IsValid)
                    {
                        yield return record.ToEvent();
                    }
                }
            }
        }
    }
}
