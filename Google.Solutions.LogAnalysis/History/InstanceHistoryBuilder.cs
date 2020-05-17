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

using Google.Apis.Util;
using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Events.Lifecycle;
using Google.Solutions.LogAnalysis.Events.System;
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
    internal class InstanceHistoryBuilder : IEventProcessor
    {
        // NB. Instance IDs stay unique throughout the history while VmInstanceReferences
        // become ambiguous. Therefore, it is important to use instance ID as primary
        // key, even though the reference is more user-friendly and meaningful.

        // Error events are not relevant for building the history, we only need
        // informational records.
        internal static EventOrder ProcessingOrder = EventOrder.NewestFirst;
        internal static IEnumerable<string> ProcessingSeverities => new[] { "NOTICE", "INFO" };
        internal static IEnumerable<string> ProcessingMethods =>
            EventFactory.LifecycleEventMethods.Concat(EventFactory.SystemEventMethods);

        public ulong InstanceId { get; }
        private readonly LinkedList<Placement> placements = new LinkedList<Placement>();

        public Tenancy Tenancy => this.placements.Any()
            ? this.placements.First().Tenancy
            : Tenancy.Unknown;

        public DateTime? LastStoppedOn { get; private set; }

        public bool IsDefunct { get; private set; } = false;

        public InstanceState State { get; private set; }

        public GlobalResourceReference Image { get; set; }
        public VmInstanceReference Reference { get; set; }

        private DateTime lastEventDate = DateTime.MaxValue;

        private void AddPlacement(Placement placement)
        {
            if (this.placements.Any())
            {
                var subsequentPlacement = this.placements.First();
                if (placement.IsAdjacent(subsequentPlacement))
                {
                    // Placement are right adjacent -> merge.
                    placement = placement.Merge(subsequentPlacement);
                    this.placements.RemoveFirst();
                }
            }

            this.placements.AddFirst(placement);
        }

        public void AddPlacement(Tenancy tenancy, string serverId, DateTime date)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;
            this.State = InstanceState.Running;

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

            if (tenancy == Tenancy.SoleTenant)
            {
                AddPlacement(new Placement(serverId, date, placedUntil));
            }
            else
            {
                AddPlacement(new Placement(date, placedUntil));
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        private InstanceHistoryBuilder(
            ulong instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            InstanceState state,
            DateTime? lastSeen,
            Tenancy tenancy)
        {
            if (instanceId == 0)
            {
                throw new ArgumentException("Instance ID cannot be 0");
            }

            this.InstanceId = instanceId;
            this.Reference = reference;
            this.Image = image;
            this.State = state;
            this.LastStoppedOn = lastSeen;

            if (state == InstanceState.Running)
            {
                Debug.Assert(tenancy != Tenancy.Unknown);
                Debug.Assert(lastSeen != null);

                // Add a synthetic placement.
                AddPlacement(new Placement(tenancy, null, lastSeen.Value, lastSeen.Value));
            }
        }

        internal static InstanceHistoryBuilder ForExistingInstance(
            ulong instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            InstanceState state,
            DateTime lastSeen,
            Tenancy tenancy)
        {
            Debug.Assert(state != InstanceState.Deleted);

            return new InstanceHistoryBuilder(
                instanceId,
                reference,
                image,
                state,
                lastSeen,
                tenancy);
        }

        internal static InstanceHistoryBuilder ForDeletedInstance(ulong instanceId)
        {
            return new InstanceHistoryBuilder(
                instanceId,
                null,
                null,
                InstanceState.Deleted,
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
                    return this.Image == null || this.Reference == null;
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

        public void OnInsert(DateTime date, VmInstanceReference reference, GlobalResourceReference image)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            // Mind you, we are processing history in reverse, so this is the
            // state before the event happened.
            this.State = InstanceState.Deleted;

            // NB. We might get multiple calls for a single instance, each providing some, but
            // potentially not all information.
            if (this.Reference == null)
            {
                this.Reference = reference;
            }

            if (this.Image == null)
            {
                this.Image = image;
            }

            // Register Fleet placement - this might be merged with an existing
            // SoleTenant placement if there has been one registerd before.
            AddPlacement(Tenancy.Fleet, null, date);
        }

        public void OnStart(DateTime date, VmInstanceReference reference)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            // Mind you, we are processing history in reverse, so this is the
            // state before the event happened.
            this.State = InstanceState.Terminated;

            if (this.Reference == null)
            {
                this.Reference = reference;
            }

            // Register Fleet placement - this might be merged with an existing
            // SoleTenant placement if there has been one registerd before.
            AddPlacement(Tenancy.Fleet, null, date);
        }

        public void OnStop(DateTime date, VmInstanceReference reference)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            this.State = InstanceState.Running;

            this.LastStoppedOn = date;

            if (this.Reference == null)
            {
                this.Reference = reference;
            }
        }

        public void OnSetPlacement(string serverId, DateTime date)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;
            this.State = InstanceState.Running;

            AddPlacement(Tenancy.SoleTenant, serverId, date);
        }

        //---------------------------------------------------------------------
        // IEventProcessor
        //---------------------------------------------------------------------

        public EventOrder ExpectedOrder => ProcessingOrder;

        public IEnumerable<string> SupportedSeverities => ProcessingSeverities;

        public IEnumerable<string> SupportedMethods => ProcessingMethods;

        public void Process(EventBase e)
        {
            if (e is NotifyInstanceLocationEvent notifyLocation)
            {
                OnSetPlacement(notifyLocation.ServerId, notifyLocation.Timestamp);
            }
            else if (e is InsertInstanceEvent insert)
            {
                OnInsert(insert.Timestamp, insert.InstanceReference, insert.Image);
            }
            else if (e is IInstanceStateChangeEvent stateChange)
            {
                if (stateChange.IsStartingInstance)
                {
                    OnStart(e.Timestamp, ((VmInstanceEventBase)e).InstanceReference);
                }
                else if (stateChange.IsTerminatingInstance)
                {
                    OnStop(e.Timestamp, ((VmInstanceEventBase)e).InstanceReference);
                }
            }
        }
    }
}
