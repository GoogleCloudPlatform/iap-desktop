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
    /// Event that indicates a continuation of an authentication session. 
    /// The client completes the challenge proposed by the server on the 
    /// previous StartSession call or requests and completes a different 
    /// challenge type. Then, the ContinueSession method accepts the 
    /// response to the challenge or method and either authenticates or 
    /// rejects the authentication attempt.
    /// </summary>
    public class OsLoginContinueSessionEvent : OsLoginEventBase
    {
        public const string ServiceName = "oslogin.googleapis.com";
        public const string Method = "google.cloud.oslogin.v1.OsLoginService.ContinueSession";
        public const string BetaMethod = "google.cloud.oslogin.v1beta.OsLoginService.ContinueSession";

        public override EventCategory Category => EventCategory.Access;

        internal OsLoginContinueSessionEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsStartOsLoginContinueSessionEvent(logRecord));
        }

        public static bool IsStartOsLoginContinueSessionEvent(LogRecord record)
        {
            return record.IsDataAccessEvent &&
                (record.ProtoPayload?.MethodName == Method ||
                 record.ProtoPayload?.MethodName == BetaMethod);
        }

        public string ChallengeStatus => this.LogRecord.ProtoPayload.Response?.Value<string>("status");

        public override string Message =>
            string.Format("Continue OS Login 2FA session for {0}: {1}",
                this.LogRecord.ProtoPayload.AuthenticationInfo.PrincipalEmail,
                this.ChallengeStatus);
    }
}
