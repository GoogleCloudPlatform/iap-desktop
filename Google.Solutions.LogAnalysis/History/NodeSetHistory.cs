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

using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.LogAnalysis.History
{
    public class NodeSetHistory
    {
        public IEnumerable<NodeHistory> Nodes { get; }

        private NodeSetHistory(IEnumerable<NodeHistory> nodes)
        {
            this.Nodes = nodes;
        }

        private static IEnumerable<NodeHistory> NodesFromInstanceSetHistory(
            IEnumerable<InstanceHistory> instanceHistories)
        {
            var placementsByServer = instanceHistories
                .Where(i => i.Tenancy == Tenancy.SoleTenant)
                .Where(i => i.Placements != null)
                .SelectMany(i => i.Placements.Select(p => new
                {
                    Instance = i,
                    p.ServerId,
                    p.From,
                    p.To
                }))
                .GroupBy(p => p.ServerId);

            foreach (var server in placementsByServer)
            {
                var peakInstanceCount = TimeseriesUtil.GetHighWatermark(
                    server.Select(p => p.From),
                    server.Select(p => p.To));

                yield return new NodeHistory(
                    server.Key,
                    server.Select(p => p.From).Min(),
                    server.Select(p => p.To).Max(),
                    peakInstanceCount,
                    server.Select(p => new NodePlacement(p.From, p.To, p.Instance)));
            }
        }

        public static NodeSetHistory FromInstancyHistory(
            IEnumerable<InstanceHistory> instanceHistories)
        {
            return new NodeSetHistory(NodesFromInstanceSetHistory(instanceHistories));
        }
    }
}
