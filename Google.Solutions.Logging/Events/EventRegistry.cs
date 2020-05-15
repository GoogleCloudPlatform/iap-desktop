﻿//
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

using Google.Solutions.Logging.Events.Lifecycle;
using Google.Solutions.Logging.Events.System;
using Google.Solutions.Logging.Records;
using System;
using System.Collections.Generic;

namespace Google.Solutions.Logging.Events
{
    public static class EventRegistry
    {
        private readonly static IDictionary<string, Func<LogRecord, EventBase>> lifecycleEventTypes
            = new Dictionary<string, Func<LogRecord, EventBase>>()
            {
                { DeleteInstanceEvent.Method, rec => new DeleteInstanceEvent(rec) },
                { InsertInstanceEvent.Method, rec => new InsertInstanceEvent(rec) },
                { InsertInstanceEvent.BetaMethod, rec => new InsertInstanceEvent(rec) },
                { StartInstanceEvent.Method, rec => new StartInstanceEvent(rec) },
                { StopInstanceEvent.Method, rec => new StopInstanceEvent(rec) },
                { StopInstanceEvent.BetaMethod, rec => new StopInstanceEvent(rec) },

                // TODO: 
                // - v1.compute.instances.reset
            };


        private readonly static IDictionary<string, Func<LogRecord, EventBase>> systemEventTypes
            = new Dictionary<string, Func<LogRecord, EventBase>>()
            {
                { AutomaticRestartEvent.Method, rec => new AutomaticRestartEvent(rec) },
                { GuestTerminateEvent.Method, rec => new GuestTerminateEvent(rec) },
                { InstanceScheduledEvent.Method, rec => new InstanceScheduledEvent(rec) },
                { MigrateOnHostMaintenanceEvent.Method, rec => new MigrateOnHostMaintenanceEvent(rec) },
                { TerminateOnHostMaintenanceEvent.Method, rec => new TerminateOnHostMaintenanceEvent(rec) },

                // TODO: 
                // - v1.compute.instances.reset
            };

        public static EventBase ToEvent(this LogRecord record)
        {
            if (lifecycleEventTypes.TryGetValue(
                record.ProtoPayload.MethodName, 
                out Func<LogRecord, EventBase> lcFactoryFunc))
            {
                return lcFactoryFunc(record);
            }
            else if (systemEventTypes.TryGetValue(
                record.ProtoPayload.MethodName,
                out Func<LogRecord, EventBase> sysFactoryFunc))
            {
                return sysFactoryFunc(record);
            }
            else
            {
                throw new ArgumentException("Unrecognized event with method " +
                    record.ProtoPayload.MethodName);
            }
        }
    }
}
