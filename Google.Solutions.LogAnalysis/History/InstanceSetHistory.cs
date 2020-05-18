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
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace Google.Solutions.LogAnalysis.History
{
    public class InstanceSetHistory
    {
        internal const string TypeAnnotation = "type.googleapis.com/google.solutions.loganalysis.InstanceSetHistory";

        [JsonProperty("@type")]
        internal string Type => TypeAnnotation;

        [JsonProperty("start")]
        public DateTime StartDate { get; }

        [JsonProperty("end")]
        public DateTime EndDate { get; }

        [JsonProperty("instances")]
        public IEnumerable<InstanceHistory> Instances { get; }

        internal InstanceSetHistory(
            DateTime startDate,
            DateTime endDate,
            IEnumerable<InstanceHistory> instances)
        {
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.Instances = instances;
        }

        [JsonConstructor]
        internal InstanceSetHistory(
            [JsonProperty("@type")] string typeAnnotation,
            [JsonProperty("start")] DateTime startDate,
            [JsonProperty("end")] DateTime endDate,
            [JsonProperty("instances")] IEnumerable<InstanceHistory> instances)
            : this(startDate, endDate, instances)
        {
            if (typeAnnotation != TypeAnnotation)
            {
                throw new FormatException("Missing type annotation: " + TypeAnnotation);
            }
        }

        //---------------------------------------------------------------------
        // Serialization.
        //---------------------------------------------------------------------

        private static JsonSerializer CreateSerializer()
        {
            return JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    NullValueHandling = NullValueHandling.Ignore
                });
        }

        public void Serialize(TextWriter writer)
        {
            CreateSerializer().Serialize(writer, this);
        }

        public static InstanceSetHistory Deserialize(TextReader reader)
        {
            return CreateSerializer().Deserialize<InstanceSetHistory>(
                new JsonTextReader(reader));
        }
    }
}
