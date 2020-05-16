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

        public IEnumerable<IPlacement> Placements { get; }

        public GlobalResourceReference Image { get; }

        public Tenancy Tenancy { get; }

        internal InstanceHistory(
            long instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            Tenancy tenancy,
            IEnumerable<IPlacement> placements
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

    public interface IPlacement
    {
        DateTime From { get; }
        DateTime To { get; }
        bool IsAdjacent(IPlacement subsequentPlacement);
        IPlacement Merge(IPlacement subsequentPlacement);
    }

    public class SoleTenantPlacement : IPlacement
    {
        public string ServerId { get; }
        public DateTime From { get; }
        public DateTime To { get; }

        public SoleTenantPlacement(string serverId, DateTime from, DateTime to)
        {
            this.ServerId = serverId;
            this.From = from;
            this.To = to;
        }

        public bool IsAdjacent(IPlacement subsequentPlacement)
        {
            if (subsequentPlacement is SoleTenantPlacement soleTenantPlacement)
            {
                return this.To == soleTenantPlacement.From &&
                       this.ServerId == soleTenantPlacement.ServerId;
            }
            else
            {
                return false;
            }
        }

        public IPlacement Merge(IPlacement subsequentPlacement)
        {
            Debug.Assert(IsAdjacent(subsequentPlacement));
            return new SoleTenantPlacement(
                this.ServerId,
                this.From,
                subsequentPlacement.To);
        }

        public override string ToString()
        {
            return $"{this.From} - {this.To} on {this.ServerId}";
        }
    }

    public class FleetPlacement : IPlacement
    {
        public DateTime From { get; }
        public DateTime To { get; }

        public FleetPlacement(DateTime from, DateTime to)
        {
            this.From = from;
            this.To = to;
        }

        public bool IsAdjacent(IPlacement subsequentPlacement)
        {
            if (subsequentPlacement is FleetPlacement soleTenantPlacement)
            {
                return this.To == soleTenantPlacement.From;
            }
            else
            {
                return false;
            }
        }

        public IPlacement Merge(IPlacement subsequentPlacement)
        {
            Debug.Assert(IsAdjacent(subsequentPlacement));
            return new FleetPlacement(
                this.From,
                subsequentPlacement.To);
        }

        public override string ToString()
        {
            return $"{this.From} - {this.To} on fleet";
        }
    }

    public enum Tenancy
    {
        SoleTenant, 
        Fleet, 
        Unknown
    }
}
