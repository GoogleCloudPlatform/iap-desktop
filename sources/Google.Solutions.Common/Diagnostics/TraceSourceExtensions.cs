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

using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Google.Solutions.Common.Diagnostics
{
    public static class TraceSourceExtensions
    {
        public static void TraceVerbose(this TraceSource source, string message)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Verbose, 0, message);
            }
        }

        public static void TraceVerbose(this TraceSource source, string message, params object?[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Verbose, 0, string.Format(message, args));
            }
        }

        public static void TraceWarning(this TraceSource source, string message, params object?[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Warning))
            {
                source.TraceData(TraceEventType.Warning, 0, string.Format(message, args));
            }
        }

        public static void TraceError(this TraceSource source, string message, params object?[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Error))
            {
                source.TraceData(TraceEventType.Error, 0, string.Format(message, args));
            }
        }

        public static void TraceError(this TraceSource source, Exception exception)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Error))
            {
                source.TraceData(TraceEventType.Error, 0, exception.Unwrap());
            }
        }

        public static TraceCallScope TraceMethod(
            this TraceSource source,
            [CallerMemberName] string? method = null)
        {
            return new TraceCallScope(source, method ?? "unknown");
        }

        public static void ForwardTo(
            this TraceSource source,
            TraceSource destination)
        {
            //
            // Add a listener that propagates all events to the
            // destination source. 
            //
            // NB. Simply copying the list of current listeners
            // would work too, but could have unexpected effects
            // if the list of listener changes over time.
            // 
            source.Switch = destination.Switch;
            source.Listeners.Add(new ForwardingListener(destination));
        }

        private class ForwardingListener : TraceListener
        {
            private readonly TraceSource destination;

            public ForwardingListener(TraceSource destination)
            {
                this.destination = destination.ExpectNotNull(nameof(destination));
            }

            public override void Flush() => this.destination.Flush();

            public override void TraceData(
                TraceEventCache eventCache,
                string source,
                TraceEventType eventType,
                int id,
                object data)
                => this.destination.TraceData(eventType, id, data);

            public override void TraceData(
                TraceEventCache eventCache,
                string source,
                TraceEventType eventType,
                int id,
                params object[] data)
                => this.destination.TraceData(eventType, id, data);

            public override void TraceEvent(
                TraceEventCache eventCache,
                string source,
                TraceEventType eventType,
                int id)
                => this.destination.TraceEvent(eventType, id);

            public override void TraceEvent(
                TraceEventCache eventCache,
                string source,
                TraceEventType eventType,
                int id,
                string message)
                => this.destination.TraceEvent(eventType, id, message);

            public override void TraceEvent(
                TraceEventCache eventCache,
                string source,
                TraceEventType eventType,
                int id,
                string format,
                params object[] args)
                => this.destination.TraceEvent(eventType, id, format, args);

            public override void TraceTransfer(
                TraceEventCache eventCache,
                string source,
                int id,
                string message,
                Guid relatedActivityId)
                => this.destination.TraceTransfer(id, message, relatedActivityId);

            public override void Write(string message)
                => this.destination.TraceInformation(message);

            public override void Write(string message, string category)
                => this.destination.TraceInformation(message);

            public override void Write(object o)
                => this.destination.TraceInformation(o?.ToString());

            public override void Write(object o, string category)
                => this.destination.TraceInformation(o?.ToString());

            public override void WriteLine(string message)
                => this.destination.TraceInformation(message);

            public override void WriteLine(object o)
                => this.destination.TraceInformation(o?.ToString());

            public override void WriteLine(string message, string category)
                => this.destination.TraceInformation(message);

            public override void WriteLine(object o, string category)
                => this.destination.TraceInformation(o?.ToString());

        }
    }
}
