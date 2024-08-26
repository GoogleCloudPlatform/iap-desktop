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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

#pragma warning disable CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method

namespace Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.System
{
    public class NotifyInstanceLocationEvent : SystemEventBase
    {
        public const string Method = "NotifyInstanceLocation";

        public string? ServerId => base.LogRecord.ProtoPayload?.Metadata?["serverId"]?.Value<string>();

        public NodeTypeLocator? NodeType
        {
            get
            {
                //
                // The node type is unqualified, e.g. "n1-node-96-624".
                //
                if (base.LogRecord.ProtoPayload?.Metadata != null && 
                    base.LogRecord.ProtoPayload.Metadata.ContainsKey("nodeType") &&
                    base.LogRecord.Resource?.Labels != null &&
                    base.LogRecord.Resource.Labels.ContainsKey("project_id") &&
                    base.LogRecord.Resource.Labels.ContainsKey("zone"))
                {
                    return new NodeTypeLocator(
                        base.LogRecord.Resource.Labels["project_id"],
                        base.LogRecord.Resource.Labels["zone"],
                        base.LogRecord.ProtoPayload.Metadata["nodeType"]?.Value<string>()!);
                }
                else
                {
                    return null;
                }
            }
        }

        public DateTime? SchedulingTimestamp => base.LogRecord.ProtoPayload?.Metadata?["timestamp"]?.Value<DateTime>();

        public override string Message => "Instance scheduled to run on sole tenant node " + this.ServerId;

        internal NotifyInstanceLocationEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsInstanceScheduledEvent(logRecord));
        }

        public static bool IsInstanceScheduledEvent(LogRecord record)
        {
            return record.IsSystemEvent &&
                record.ProtoPayload?.MethodName == Method;
        }
    }
}
