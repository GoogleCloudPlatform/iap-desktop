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

using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.History
{
    /// <summary>
    /// Tracks that a VM was "placed" on either a sole tenant node or
    /// on the fleet and was running from a certain point in time till
    /// a certain point in time.
    /// </summary>
    public class InstancePlacement
    {
        [JsonProperty("tenancy")]
        public Tenancies Tenancy { get; }

        [JsonProperty("server")]
        public string ServerId { get; }

        [JsonProperty("from")]
        public DateTime From { get; }

        [JsonProperty("to")]
        public DateTime To { get; }

        private static Tenancies MergeTenancy(Tenancies lhs, Tenancies rhs)
        {
            Debug.Assert(lhs.IsSingleFlag());
            Debug.Assert(rhs.IsSingleFlag());

            if (lhs == Tenancies.SoleTenant || rhs == Tenancies.SoleTenant)
            {
                // If one of them is sole tenant, both of them must be.
                // that is because in case of SoleTenant, we have strong
                // evidence that this is a SoleTenant VM whereas in the
                // case of Fleet, there is simply an absence of evidence
                // that it might be SoleTenant.
                return Tenancies.SoleTenant;
            }
            else
            {
                return Tenancies.Fleet;
            }
        }

        [JsonConstructor]
        internal InstancePlacement(
            [JsonProperty("tenancy")] Tenancies tenancy,
            [JsonProperty("server")] string serverId,
            [JsonProperty("from")] DateTime from,
            [JsonProperty("to")] DateTime to)
        {
            Debug.Assert(from <= to);
            Debug.Assert(tenancy != Tenancies.Unknown);
            Debug.Assert(!tenancy.IsFlagCombination());

            this.Tenancy = tenancy;
            this.ServerId = serverId;
            this.From = from;
            this.To = to;
        }

        public InstancePlacement(DateTime from, DateTime to)
            : this(Tenancies.Fleet, null, from, to)
        {
        }

        public InstancePlacement(string serverId, DateTime from, DateTime to)
            : this(Tenancies.SoleTenant, serverId, from, to)
        {
        }

        public bool IsAdjacent(InstancePlacement subsequentPlacement)
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

        public InstancePlacement Merge(InstancePlacement subsequentPlacement)
        {
            Debug.Assert(IsAdjacent(subsequentPlacement));
            return new InstancePlacement(
                MergeTenancy(this.Tenancy, subsequentPlacement.Tenancy),
                this.ServerId ?? subsequentPlacement.ServerId,
                this.From,
                subsequentPlacement.To);
        }

        public override string ToString()
        {
            var where = this.Tenancy == Tenancies.SoleTenant
                ? this.ServerId
                : "fleet";
            return $"{this.From} - {this.To} on {where}";
        }
    }

    [Flags]
    public enum Tenancies
    {
        Unknown = 0,
        Fleet = 1,
        SoleTenant = 2
    }
}
