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
    /// <summary>
    /// Reconstructs the history of an instance by analyzing
    /// events in reverse chronological order.
    /// </summary>
    internal class InstanceHistoryBuilder
    {
        // NB. Instance IDs stay unique throughout the history while VmInstanceReferences
        // become ambiguous. Therefore, it is important to use instance ID as primary
        // key, even though the reference is more user-friendly and meaningful.

        public long InstanceId { get; }
        private readonly LinkedList<SoleTenantPlacement> placements = new LinkedList<SoleTenantPlacement>();

        public Tenancy Tenancy { get; private set; }
        public DateTime? LastStoppedOn { get; private set; }

        public bool IsDefunct { get; private set; } = false;

        public GlobalResourceReference Image { get; set; }
        public VmInstanceReference Reference { get; set; }

        private DateTime lastEventDate = DateTime.MaxValue;

        private void AddPlacement(SoleTenantPlacement placement)
        {
            if (this.placements.Any())
            {
                var subsequentPlacement = this.placements.First();
                if (placement.To == subsequentPlacement.From &&
                    placement.ServerId == subsequentPlacement.ServerId)
                {
                    // Placement are right adjacent -> merge.
                    placement = new SoleTenantPlacement(
                        placement.ServerId,
                        placement.From,
                        subsequentPlacement.To);
                    this.placements.RemoveFirst();
                }
            }

            this.placements.AddFirst(placement);
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        private InstanceHistoryBuilder(
            long instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            DateTime? lastStoppedOn,
            Tenancy tenancy)
        {
            Debug.Assert(instanceId != 0);

            this.InstanceId = instanceId;
            this.LastStoppedOn = lastStoppedOn;
            this.Reference = reference;
            this.Image = image;
            this.Tenancy = tenancy;
        }

        internal static InstanceHistoryBuilder ForExistingInstance(
            long instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            DateTime lastSeen,
            Tenancy tenancy)
        {
            return new InstanceHistoryBuilder(
                instanceId,
                reference,
                image,
                lastSeen,   // Pretend it was stopped when it was last seen.
                tenancy);
        }

        internal static InstanceHistoryBuilder ForDeletedInstance(long instanceId, VmInstanceReference reference)
        {
            return new InstanceHistoryBuilder(
                instanceId,
                reference,
                null,
                (DateTime?)null,    // Not clear yet when it was stopped
                Tenancy.Unknown);
        }

        public InstanceHistory Build()
        {
            return new InstanceHistory(
                this.InstanceId,
                this.Reference,
                this.Image,
                this.Tenancy,
                this.placements);
        }

        public bool IsMoreInformationNeeded
        {
            get
            {
                if (this.Tenancy == Tenancy.Unknown)
                {
                    return true;
                }
                else if (this.Tenancy == Tenancy.SoleTenant)
                {
                    return this.Image == null;
                }
                else
                {
                    return false;
                }
            }
        }

        //---------------------------------------------------------------------
        // Lifecycle events that construct the history.
        //---------------------------------------------------------------------

        public void OnInsert(DateTime date, GlobalResourceReference image)
        {
            Debug.Assert(date < this.lastEventDate);
            this.lastEventDate = date;

            // NB. We might get multiple calls for a single instance, each providing some, but
            // potentially not all information.

            if (this.Tenancy == Tenancy.Unknown)
            {
                // We have not seen any OnSetPlacement call - by now, it is therefore 
                // clear that this is not a sole tenant VM.
                this.Tenancy = History.Tenancy.Fleet;
            }

            if (this.Image == null)
            {
                this.Image = image;
            }
        }

        public void OnStart(DateTime date)
        {
            Debug.Assert(date < this.lastEventDate);
            this.lastEventDate = date;

        }

        public void OnStop(DateTime date)
        {
            Debug.Assert(date < this.lastEventDate);
            this.lastEventDate = date;

            this.LastStoppedOn = date;
        }

        public void OnSetPlacement(string serverId, DateTime date)
        {
            Debug.Assert(date < this.lastEventDate);
            this.lastEventDate = date;

            // Now we know for sure it is a sole tenant VM.
            this.Tenancy = Tenancy.SoleTenant;

            DateTime placedUntil;
            if (this.placements.Count == 0)
            {
                if (this.LastStoppedOn.HasValue)
                {
                    Debug.Assert(this.LastStoppedOn != null);
                    Debug.Assert(date <= this.LastStoppedOn);

                    placedUntil = this.LastStoppedOn.Value;
                }
                else
                {
                    Debug.WriteLine(
                        $"Instance {this.InstanceId} was placed, but never stopped, " +
                        "and yet is not running anymore. Flagging as defunct.");
                    this.IsDefunct = true;
                    return;
                }
            }
            else
            {
                if (this.LastStoppedOn.HasValue)
                {
                    Debug.Assert(date <= this.LastStoppedOn);
                    Debug.Assert(date <= this.placements.First().From);

                    placedUntil = DateTimeUtil.Min(
                        this.LastStoppedOn.Value,
                        this.placements.First().From);
                }
                else
                {
                    Debug.Assert(date <= this.placements.First().From);
                    placedUntil = this.placements.First().From;
                }
            }

            // For a fleet VM, we'd never receive this kind of VM, so this must be 
            // a sole tenant VM.
            this.Tenancy = History.Tenancy.SoleTenant;
            AddPlacement(new SoleTenantPlacement(serverId, date, placedUntil));
        }
    }
}
