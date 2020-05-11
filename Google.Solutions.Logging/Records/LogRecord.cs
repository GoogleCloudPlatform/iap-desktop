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
using System;
using System.Collections.Generic;
using System.IO;

namespace Google.Solutions.Logging.Records
{
    /// <summary>
    /// Class representation of a 'Log record', which is the top-level type all 
    /// records use.
    /// </summary>
    public class LogRecord
    {
        [JsonProperty("insertId")]
        public string InsertId { get; set; }

        [JsonProperty("logName")]
        public string LogName { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("resource")]
        public ResourceRecord Resource { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("protoPayload")]
        public AuditLogRecord ProtoPayload { get; set; }

        //---------------------------------------------------------------------
        // Derived part.
        //---------------------------------------------------------------------

        private string[] SplitLogName()
        {
            //
            // LogName has the format
            // projects/<project-ud>/logs/cloudaudit.googleapis.com%2F<type>'
            //
            var parts = this.LogName.Split('/');
            if (parts.Length != 4)
            {
                throw new ArgumentException(
                    "Enountered unexpected LogName format: " + this.LogName);
            }

            return parts;
        }

        public string ProjectId => SplitLogName()[1];

        public bool IsSystemEvent => this.LogName.EndsWith("%2Fsystem_event");

        public bool IsActivityEvent => this.LogName.EndsWith("%2Factivity");

        //---------------------------------------------------------------------
        // Parsing.
        //---------------------------------------------------------------------

        public static LogRecord Deserialize(JsonReader reader)
        {
            var serializer = JsonSerializer.Create();
            return serializer.Deserialize<LogRecord>(reader);
        }

        public static LogRecord Deserialize(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                return Deserialize(reader);
            }
        }
    }

    public class ResourceRecord
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("labels")]
        public IDictionary<string, string> Labels { get; set; }
    }
}
