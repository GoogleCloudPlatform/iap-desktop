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

using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events.Iap
{
    public class AuthorizeUserTunnelEvent : EventBase
    {
        public const string ServiceName = "iap.googleapis.com";
        public const string Method = "AuthorizeUser";

        internal AuthorizeUserTunnelEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsAuthorizeUserEvent(logRecord));
        }

        public static bool IsAuthorizeUserEvent(LogRecord record)
        {
            return record.IsDataAccessEvent &&
                record.ProtoPayload.MethodName == Method &&
                record.Resource.Type == "gce_instance"; // Distinguish from IAP-Web events.
        }

        public bool IsError => this.Severity == "ERROR";

        public override string Message => IsError
            ? $"{TunnelDescription} [{this.Status.Message}]"
            : TunnelDescription;

        private string TunnelDescription
        {
            get
            {
                var userAgent = this.UserAgent ?? "(unknown agent)";
                int parenthesis = userAgent.IndexOf('(');
                if (parenthesis > 0)
                {
                    // Strip version and details.
                    userAgent = userAgent.Substring(0, parenthesis).Trim();
                }

                return $"{SourceHost ?? "(unknown)"} to " + 
                    $"{DestinationHost ?? "(unknown host)"}:{DestinationPort ?? "(unknown port)"} "+
                    $"using {userAgent}";
            }
        }

        //---------------------------------------------------------------------
        // Record-specific fields.
        //---------------------------------------------------------------------

        public string SourceHost => 
            base.LogRecord.ProtoPayload.RequestMetadata?.Value<string>("callerIp");
        public string UserAgent => 
            base.LogRecord.ProtoPayload.RequestMetadata?.Value<string>("callerSuppliedUserAgent");
        public string DestinationHost =>
            base.LogRecord.ProtoPayload.RequestMetadata?["destinationAttributes"]?.Value<string>("ip");
        public string DestinationPort =>
            base.LogRecord.ProtoPayload.RequestMetadata?["destinationAttributes"]?.Value<string>("port");

        public ulong InstanceId => string.IsNullOrEmpty(base.LogRecord.Resource.Labels["instance_id"])
            ? 0
            : ulong.Parse(base.LogRecord.Resource.Labels["instance_id"]);

        public string ProjectId => base.LogRecord.Resource.Labels["project_id"];
        public string Zone => base.LogRecord.Resource.Labels["zone"];

    }
}
