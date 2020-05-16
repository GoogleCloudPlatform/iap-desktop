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

using Google.Solutions.Compute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.LogAnalysis.History
{
    public class InstanceHistory
    {
        // TODO: make ulong
        public long InstanceId { get; }

        public VmInstanceReference Reference { get; }

        public IEnumerable<Placement> Placements { get; }

        public GlobalResourceReference Image { get; }

        public Tenancy Tenancy { get; }

        internal InstanceHistory(
            long instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            Tenancy tenancy,
            IEnumerable<Placement> placements
            )
        {
            Debug.Assert(tenancy == Tenancy.SoleTenant || !placements.Any());

            this.InstanceId = instanceId;
            this.Reference = reference;
            this.Image = image;
            this.Tenancy = tenancy;
            this.Placements = placements;
        }

        public override string ToString()
        {
            return $"{this.Reference} ({this.InstanceId})";
        }
    }

    public class Placement
    {
        public Tenancy Tenancy { get; }
        public string ServerId { get; }
        public DateTime From { get; }
        public DateTime To { get; }

        private Placement(Tenancy tenancy, string serverId, DateTime from, DateTime to)
        {
            this.Tenancy = tenancy;
            this.ServerId = serverId;
            this.From = from;
            this.To = to;
        }

        public Placement(DateTime from, DateTime to)
            :this(Tenancy.Fleet, null, from, to)
        {
        }

        public Placement(string serverId, DateTime from, DateTime to)
            :this(Tenancy.SoleTenant, serverId, from, to)
        {
        }

        public bool IsAdjacent(Placement subsequentPlacement)
        {
            return this.Tenancy == subsequentPlacement.Tenancy &&
                    this.To == subsequentPlacement.From &&
                    this.ServerId == subsequentPlacement.ServerId;
        }

        public Placement Merge(Placement subsequentPlacement)
        {
            Debug.Assert(IsAdjacent(subsequentPlacement));
            return new Placement(
                this.Tenancy,
                this.ServerId,
                this.From,
                subsequentPlacement.To);
        }

        public override string ToString()
        {
            var where = this.Tenancy == Tenancy.SoleTenant
                ? this.ServerId
                : "fleet";
            return $"{this.From} - {this.To} on {where}";
        }
    }

    public enum Tenancy
    {
        SoleTenant, 
        Fleet, 
        Unknown
    }
}
