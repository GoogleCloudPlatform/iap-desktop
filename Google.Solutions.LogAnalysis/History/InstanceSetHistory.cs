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
using System.Linq;

namespace Google.Solutions.LogAnalysis.History
{
    public class InstanceSetHistory
    {
        [JsonProperty("start")]
        public DateTime StartDate { get; }

        [JsonProperty("end")]
        public DateTime EndDate { get; }

        [JsonProperty("instances")]
        public IEnumerable<InstanceHistory> Instances { get; }

        [JsonConstructor]
        internal InstanceSetHistory(
            [JsonProperty("start")] DateTime startDate,
            [JsonProperty("end")] DateTime endDate,
            [JsonProperty("instances")] IEnumerable<InstanceHistory> instances)
        {
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.Instances = instances;
        }

        //public IEnumerable<HistogramDataPoint> GetInstanceCountHistogram(
        //    DateTime from,
        //    DateTime to)
        //{
        //    var placements = this.Instances
        //        .Where(i => i.Placements != null)
        //        .SelectMany(i => i.Placements);
        //    // TODO
        //    throw new NotImplementedException();
        //}

        //---------------------------------------------------------------------
        // Serialization.
        //---------------------------------------------------------------------

        internal class Envelope
        {
            internal const string TypeAnnotation = "type.googleapis.com/google.solutions.loganalysis.InstanceSetHistory";

            [JsonProperty("@type")]
            internal string Type => TypeAnnotation;

            [JsonProperty("instanceSetHistory")]
            internal InstanceSetHistory InstanceSetHistory { get; }

            public Envelope(
                InstanceSetHistory instanceSetHistory)
            {
                this.InstanceSetHistory = instanceSetHistory;
            }

            [JsonConstructor]
            public Envelope(
                [JsonProperty("@type")] string typeAnnotation,
                [JsonProperty("instanceSetHistory")]InstanceSetHistory instanceSetHistory)
                : this(instanceSetHistory)
            {
                if (typeAnnotation != TypeAnnotation)
                {
                    throw new FormatException("Missing type annotation: " + TypeAnnotation);
                }
            }
        }

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
            CreateSerializer().Serialize(writer, new Envelope(this));
        }

        public static InstanceSetHistory Deserialize(TextReader reader)
        {
            return CreateSerializer().Deserialize<InstanceSetHistory.Envelope>(
                new JsonTextReader(reader)).InstanceSetHistory;
        }
    }
}
