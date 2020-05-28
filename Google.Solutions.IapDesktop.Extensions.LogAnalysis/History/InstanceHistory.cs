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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.History
{
    public class InstanceHistory
    {
        [JsonProperty("id")]
        public ulong InstanceId { get; }

        [JsonProperty("vm")]
        public InstanceLocator Reference { get; }

        [JsonProperty("placements")]
        public IEnumerable<InstancePlacement> Placements { get; }

        [JsonProperty("image")]
        public ImageLocator Image { get; }

        [JsonProperty("state")]
        public InstanceHistoryState State { get; }

        [JsonConstructor]
        internal InstanceHistory(
            [JsonProperty("id")] ulong instanceId,
            [JsonProperty("vm")] InstanceLocator reference,
            [JsonProperty("state")] InstanceHistoryState state,
            [JsonProperty("image")] ImageLocator image,
            [JsonProperty("placements")] IEnumerable<InstancePlacement> placements
            )
        {
            this.InstanceId = instanceId;
            this.Reference = reference;
            this.State = state;
            this.Image = image;
            this.Placements = placements;
        }

        public override string ToString()
        {
            return $"{this.Reference} ({this.InstanceId})";
        }
    }

    public enum InstanceHistoryState
    {
        Complete,
        MissingTenancy,
        MissingName,
        MissingImage,
        MissingStopEvent
    }
}
