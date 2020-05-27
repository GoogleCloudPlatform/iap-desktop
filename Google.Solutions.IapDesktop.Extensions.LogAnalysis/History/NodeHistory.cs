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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.History
{
    public class NodeHistory
    {
        public string ServerId { get; }

        public DateTime FirstUse { get; }
        public DateTime LastUse { get; }

        public IEnumerable<NodePlacement> Placements { get; }

        public string Zone => this.Placements.Any()
            ? this.Placements.First().Instance.Reference?.Zone
            : null;

        public string ProjectId => this.Placements.Any()
            ? this.Placements.First().Instance.Reference?.ProjectId
            : null;

        public uint PeakConcurrentPlacements { get; }

        internal NodeHistory(
            string serverId,
            DateTime firstUse,
            DateTime lastUse,
            uint peakInstanceCount,
            IEnumerable<NodePlacement> placements)
        {
            Debug.Assert(placements != null);
            Debug.Assert(firstUse <= lastUse);

            this.ServerId = serverId;
            this.FirstUse = firstUse;
            this.LastUse = lastUse;
            this.PeakConcurrentPlacements = peakInstanceCount;
            this.Placements = placements;
        }

        /// <summary>
        /// Create time series indicating the maximum number of parallel 
        /// placements for each day of this node's existence.
        /// </summary>
        public IEnumerable<DataPoint> MaxInstancePlacementsByDay =>
            this.Placements == null
                ? Enumerable.Empty<DataPoint>()
                : TimeseriesUtil.DailyHistogram(
                    this.Placements.Select(p => p.From),
                    this.Placements.Select(p => p.To));
    }
}
