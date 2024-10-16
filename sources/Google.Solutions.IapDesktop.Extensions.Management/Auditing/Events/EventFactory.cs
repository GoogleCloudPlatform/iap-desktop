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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.System;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events
{
    public static class EventFactory
    {

        private static readonly Dictionary<string, Func<LogRecord, EventBase>> lifecycleEvents
            = new Dictionary<string, Func<LogRecord, EventBase>>()
            {
                { DeleteInstanceEvent.Method, rec => new DeleteInstanceEvent(rec) },
                { InsertInstanceEvent.Method, rec => new InsertInstanceEvent(rec) },
                { InsertInstanceEvent.BetaMethod, rec => new InsertInstanceEvent(rec) },
                { StartInstanceEvent.Method, rec => new StartInstanceEvent(rec) },
                { StartWithEncryptionKeyEvent.Method, rec => new StartWithEncryptionKeyEvent(rec) },
                { StartWithEncryptionKeyEvent.BetaMethod, rec => new StartWithEncryptionKeyEvent(rec) },
                { StopInstanceEvent.Method, rec => new StopInstanceEvent(rec) },
                { StopInstanceEvent.BetaMethod, rec => new StopInstanceEvent(rec) },
                { ResetInstanceEvent.Method, rec => new ResetInstanceEvent(rec) },
                { SuspendInstanceEvent.Method, rec => new SuspendInstanceEvent(rec) },
                { SuspendInstanceEvent.BetaMethod, rec => new SuspendInstanceEvent(rec) },
                { SuspendInstanceEvent.AlphaMethod, rec => new SuspendInstanceEvent(rec) },
                { ResumeInstanceEvent.Method, rec => new ResumeInstanceEvent(rec) },
                { ResumeInstanceEvent.BetaMethod, rec => new ResumeInstanceEvent(rec) },
                { ResumeInstanceEvent.AlphaMethod, rec => new ResumeInstanceEvent(rec) },
                
                // Some lifecyce-related beta events omitted (based on audit_log_services.ts),
            };

        private static readonly Dictionary<string, Func<LogRecord, EventBase>> systemEvents
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

        private static readonly Dictionary<string, Func<LogRecord, EventBase>> accessEvents
            = new Dictionary<string, Func<LogRecord, EventBase>>()
            {
                { AuthorizeUserTunnelEvent.Method, rec => new AuthorizeUserTunnelEvent(rec) },
                { SetMetadataEvent.Method, rec => new SetMetadataEvent(rec) },
                { SetCommonInstanceMetadataEvent.Method, rec => new SetCommonInstanceMetadataEvent(rec) },
                { OsLoginCheckPolicyEvent.Method, rec => new OsLoginCheckPolicyEvent(rec) },
                { OsLoginCheckPolicyEvent.BetaMethod, rec => new OsLoginCheckPolicyEvent(rec) },
                { OsLoginStartSessionEvent.Method, rec => new OsLoginStartSessionEvent(rec) },
                { OsLoginStartSessionEvent.BetaMethod, rec => new OsLoginStartSessionEvent(rec) },
                { OsLoginContinueSessionEvent.Method, rec => new OsLoginContinueSessionEvent(rec) },
                { OsLoginContinueSessionEvent.BetaMethod, rec => new OsLoginContinueSessionEvent(rec) }
            };

        public static IEnumerable<string> LifecycleEventMethods => lifecycleEvents.Keys;
        public static IEnumerable<string> SystemEventMethods => systemEvents.Keys;
        public static IEnumerable<string> AccessEventMethods => accessEvents.Keys;

        public static EventBase FromRecord(LogRecord record)
        {
            if (!record.IsValidAuditLogRecord)
            {
                throw new ArgumentException("Not a valid audit log record");
            }

            if (record.ProtoPayload?.MethodName != null &&
                lifecycleEvents.TryGetValue(record.ProtoPayload.MethodName, out var lcFunc))
            {
                var e = lcFunc(record);
                Debug.Assert(e.Category == EventCategory.Lifecycle);
                return e;
            }
            else if (record.ProtoPayload?.MethodName != null &&
                systemEvents.TryGetValue(record.ProtoPayload.MethodName, out var sysFunc))
            {
                var e = sysFunc(record);
                Debug.Assert(e.Category == EventCategory.System);
                return e;
            }
            else if (record.ProtoPayload?.MethodName != null &&
                accessEvents.TryGetValue(record.ProtoPayload.MethodName, out var accessFunc))
            {
                var e = accessFunc(record);
                Debug.Assert(e.Category == EventCategory.Access);
                return e;
            }
            else if (record.IsSystemEvent)
            {
                //
                // There are some less common/more esoteric system events that do not
                // have a wrapper class. Map these to GenericSystemEvent.
                //
                return new GenericSystemEvent(record);
            }
            else
            {
                //
                // The list of activity event types is incomplete any might grow stale over time,
                // so ensure to fail open.
                //
                return new UnknownEvent(record);
            }
        }

        public static EventBase ToEvent(this LogRecord record) => FromRecord(record);
    }
}
