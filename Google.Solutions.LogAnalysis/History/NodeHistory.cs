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
using System.Collections;
using System.Collections.Generic;

namespace Google.Solutions.LogAnalysis.History
{
    public class NodeHistory
    {
        public string ServerId { get; }

        public DateTime FirstUse { get; }
        public DateTime LastUse { get; }

        public IEnumerable<NodePlacement> Placements { get; }

        public uint PeakConcurrentPlacements { get; }

        internal NodeHistory(
            string serverId,
            DateTime firstUse,
            DateTime lastUse,
            uint peakInstanceCount,
            IEnumerable<NodePlacement> placements)
        {
            this.ServerId = serverId;
            this.FirstUse = firstUse;
            this.LastUse = lastUse;
            this.PeakConcurrentPlacements = peakInstanceCount;
            this.Placements = placements;
        }
    }
}
