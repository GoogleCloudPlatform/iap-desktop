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

using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events.Access
{
    /// <summary>
    /// Event that indicates a new 2FA authentication session. In a 
    /// StartSession call, a client declares its capabilities to the server 
    /// and obtains information about the available challenges.
    /// </summary>
    public class OsLoginStartSessionEvent : OsLoginEventBase
    {
        public const string ServiceName = "oslogin.googleapis.com";
        public const string Method = "google.cloud.oslogin.v1.OsLoginService.StartSession";

        public override EventCategory Category => EventCategory.Access;

        internal OsLoginStartSessionEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsStartOsLoginStartSessionEvent(logRecord));
        }

        public static bool IsStartOsLoginStartSessionEvent(LogRecord record)
        {
            return record.IsDataAccessEvent &&
                record.ProtoPayload.MethodName == Method;
        }

        public string ChallengeStatus => this.LogRecord.ProtoPayload.Response?.Value<string>("status");

        public override string Message =>
            string.Format("Start OS Login 2FA session for {0}: {1}",
                this.LogRecord.ProtoPayload.AuthenticationInfo.PrincipalEmail,
                this.ChallengeStatus);
    }
}
