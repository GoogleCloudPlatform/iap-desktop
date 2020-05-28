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

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.History
{
    public class AnnotatedInstanceSetHistory
    {
        [JsonProperty("history")]
        public InstanceSetHistory History { get; }

        [JsonProperty("annotations")]
        internal IDictionary<string, ImageAnnotation> Annotations { get; }

        [JsonConstructor]
        internal AnnotatedInstanceSetHistory(
            [JsonProperty("history")] InstanceSetHistory InstanceSet,
            [JsonProperty("annotations")] IDictionary<string, ImageAnnotation> annotations)
        {
            this.History = InstanceSet;
            this.Annotations = annotations;
        }

        public static AnnotatedInstanceSetHistory FromInstanceSetHistory(
            InstanceSetHistory instanceSet)
        {
            return new AnnotatedInstanceSetHistory(
                instanceSet,
                new Dictionary<string, ImageAnnotation>());
        }

        //---------------------------------------------------------------------
        // Serialization.
        //---------------------------------------------------------------------

        internal class Envelope
        {
            internal const string TypeAnnotation = "type.googleapis.com/Google.Solutions.IapDesktop.Extensions.LogAnalysis.AnnotatedInstanceSetHistory";

            [JsonProperty("@type")]
            internal string Type => TypeAnnotation;

            [JsonProperty("annotatedInstanceSetHistory")]
            internal AnnotatedInstanceSetHistory InstanceSetHistory { get; }

            public Envelope(
                AnnotatedInstanceSetHistory instanceSetHistory)
            {
                this.InstanceSetHistory = instanceSetHistory;
            }

            [JsonConstructor]
            public Envelope(
                [JsonProperty("@type")] string typeAnnotation,
                [JsonProperty("instanceSetHistory")] AnnotatedInstanceSetHistory instanceSetHistory)
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

        public static AnnotatedInstanceSetHistory Deserialize(TextReader reader)
        {
            return CreateSerializer().Deserialize<AnnotatedInstanceSetHistory.Envelope>(
                new JsonTextReader(reader)).InstanceSetHistory;
        }
    }
}
