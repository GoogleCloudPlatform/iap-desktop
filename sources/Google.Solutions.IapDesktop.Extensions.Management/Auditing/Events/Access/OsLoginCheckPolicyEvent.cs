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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access
{
    /// <summary>
    /// Event that indicates a connection attempt to a VM.
    /// </summary>
    public class OsLoginCheckPolicyEvent : OsLoginEventBase
    {
        public const string ServiceName = "oslogin.googleapis.com";
        public const string Method = "google.cloud.oslogin.v1.OsLoginService.CheckPolicy";
        public const string BetaMethod = "google.cloud.oslogin.v1beta.OsLoginService.CheckPolicy";

        public override EventCategory Category => EventCategory.Access;

        internal OsLoginCheckPolicyEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsStartOsLoginCheckPolicyEvent(logRecord));
        }

        public static bool IsStartOsLoginCheckPolicyEvent(LogRecord record)
        {
            return record.IsDataAccessEvent &&
                (record.ProtoPayload?.MethodName == Method ||
                 record.ProtoPayload?.MethodName == BetaMethod);
        }

        public bool IsSuccess => this.LogRecord.ProtoPayload.Response?.Value<bool?>("success") == true;

        public override string Message =>
            string.Format("OS Login access for {0} and policy {1} {2}",
                this.LogRecord.ProtoPayload.AuthenticationInfo.PrincipalEmail,
                this.LogRecord.ProtoPayload.Request.Value<string>("policy"),
                this.IsSuccess ? "granted" : "denied");
    }
}
