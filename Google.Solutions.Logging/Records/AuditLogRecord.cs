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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Google.Solutions.Logging.Records
{
    /// <summary>
    /// Class representation of a 'AuditLog' record, see
    /// https://cloud.google.com/logging/docs/reference/audit/auditlog/rest/Shared.Types/AuditLog.
    /// </summary>
    public class AuditLogRecord
    {
        public const string TypeString = "type.googleapis.com/google.cloud.audit.AuditLog";

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        [JsonProperty("methodName")]
        public string MethodName { get; set; }

        [JsonProperty("resourceName")]
        public string ResourceName { get; set; }

        [JsonProperty("authenticationInfo")]
        public AuthenticationInfo AuthenticationInfo { get; set; }

        //---------------------------------------------------------------------
        // Polymorphic part.
        //---------------------------------------------------------------------

        [JsonProperty("metadata")]
        public JObject Metadata { get; set; }

        [JsonProperty("request")]
        public JObject Request { get; set; }

        [JsonProperty("response")]
        public JObject Response { get; set; }
    }

    public class AuthenticationInfo
    {
        [JsonProperty("principalEmail")]
        public string PrincipalEmail { get; set; }
    }
}
