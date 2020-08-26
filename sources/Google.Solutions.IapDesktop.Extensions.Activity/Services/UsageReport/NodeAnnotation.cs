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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.UsageReport
{
    public class NodeAnnotation
    {
        // Up until mid 2020, this was the only node type available.
        private static readonly string defaultNodeType = "n1-node-96-624";

        private static readonly IDictionary<string, NodeAnnotation> knownTypes
            = new Dictionary<string, NodeAnnotation>()
            {
                // See https://cloud.google.com/compute/docs/nodes/sole-tenant-nodes#node_types
                // The core count is not available via API, cf. b/166257346.
                { "c2-node-60-240", new NodeAnnotation("c2-node-60-240", 36) },
                { "m1-node-96-1433", new NodeAnnotation("m1-node-96-1433", 56) },
                { "n1-node-96-624", new NodeAnnotation("n1-node-96-624", 56) },
                { "n2-node-80-640", new NodeAnnotation("n2-node-80-640", 48)},
                { "n2d-node-224-896", new NodeAnnotation("n2d-node-224-896", 128) }
            };


        [JsonProperty("physicalCores")]
        public int PhysicalCores { get; }

        [JsonProperty("nodeType")]
        public string NodeType { get; }

        [JsonConstructor]
        internal NodeAnnotation(
            [JsonProperty("nodeType")] string nodeType,
            [JsonProperty("physicalCores")] int physicalCores)
        {
            this.NodeType = nodeType;
            this.PhysicalCores = physicalCores;
        }

        internal static NodeAnnotation FromNodeType(NodeTypeLocator nodeType)
        {
            if (nodeType == null)
            {
                return knownTypes[defaultNodeType];
            }
            else if (knownTypes.TryGetValue(nodeType.Name, out NodeAnnotation annotation))
            {
                return annotation;
            }
            else
            {
                TraceSources.IapDesktop.TraceWarning(
                    "Unrecognized node type {0}, assuming defaults", 
                    nodeType);

                return knownTypes[defaultNodeType];
            }
        }
    }
}
