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
using System.Diagnostics;

namespace Google.Solutions.LogAnalysis.History
{
    public class Placement
    {
        public Tenancy Tenancy { get; }
        public string ServerId { get; }
        public DateTime From { get; }
        public DateTime To { get; }

        private static Tenancy MergeTenancy(Tenancy lhs, Tenancy rhs)
        {
            if (lhs == Tenancy.SoleTenant || rhs == Tenancy.SoleTenant)
            {
                // If one of them is sole tenant, both of them must be.
                // that is because in case of SoleTenant, we have strong
                // evidence that this is a SoleTenant VM whereas in the
                // case of Fleet, there is simply an absence of evidence
                // that it might be SoleTenant.
                return Tenancy.SoleTenant;
            }
            else
            {
                return Tenancy.Fleet;
            }
        }

        internal Placement(Tenancy tenancy, string serverId, DateTime from, DateTime to)
        {
            //Debug.Assert(tenancy != Tenancy.SoleTenant || serverId != null);

            this.Tenancy = tenancy;
            this.ServerId = serverId;
            this.From = from;
            this.To = to;
        }

        public Placement(DateTime from, DateTime to)
            : this(Tenancy.Fleet, null, from, to)
        {
        }

        public Placement(string serverId, DateTime from, DateTime to)
            : this(Tenancy.SoleTenant, serverId, from, to)
        {
        }

        public bool IsAdjacent(Placement subsequentPlacement)
        {
            Debug.Assert(this.To <= subsequentPlacement.To);
            Debug.Assert(this.From <= subsequentPlacement.From);

            if (Math.Abs((this.To - subsequentPlacement.From).TotalSeconds) < 60)
            {
                // These two placements are so close to another that one of them
                // probably isn't right.
                if (this.ServerId != null && subsequentPlacement.ServerId != null)
                {
                    return this.ServerId == subsequentPlacement.ServerId;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                // There is substantial space between these placements.
                return false;
            }
        }

        public Placement Merge(Placement subsequentPlacement)
        {
            Debug.Assert(IsAdjacent(subsequentPlacement));
            return new Placement(
                MergeTenancy(this.Tenancy, subsequentPlacement.Tenancy),
                this.ServerId ?? subsequentPlacement.ServerId,
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
