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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.History
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

        public string ProjectId => this.reference?.ProjectId;

        public Tenancies Tenancy => this.placements.Any()
            ? this.placements.First().Tenancy
            : Tenancies.Unknown;

        private bool missingStopEvent = false;

        //
        // Information accumulated as we go thru history.
        //
        private readonly LinkedList<InstancePlacement> placements = new LinkedList<InstancePlacement>();

        private ImageLocator image;
        private InstanceLocator reference;

        private DateTime? lastStoppedOn;
        private DateTime lastEventDate = DateTime.MaxValue;

        private void AddPlacement(InstancePlacement placement)
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

        public void AddPlacement(Tenancies tenancy, string serverId, NodeTypeLocator nodeType, DateTime date)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(date <= this.lastEventDate);
            Debug.Assert(tenancy == Tenancies.SoleTenant || (serverId == null && nodeType == null));
            this.lastEventDate = date;

            DateTime placedUntil;
            if (this.placements.Count == 0)
            {
                if (this.lastStoppedOn.HasValue)
                {
                    Debug.Assert(this.lastStoppedOn != null);
                    Debug.Assert(date <= this.lastStoppedOn);

                    placedUntil = this.lastStoppedOn.Value;
                }
                else
                {
                    TraceSources.IapDesktop.TraceWarning(
                        "Instance {0} was placed, but never stopped, " +
                        "and yet is not running anymore. Flagging as defunct.",
                        this.InstanceId);
                    this.missingStopEvent = true;
                    return;
                }
            }
            else
            {
                if (this.lastStoppedOn.HasValue)
                {
                    Debug.Assert(date <= this.lastStoppedOn);
                    Debug.Assert(date <= this.placements.First().From);

                    placedUntil = DateTimeUtil.Min(
                        this.lastStoppedOn.Value,
                        this.placements.First().From);
                }
                else
                {
                    Debug.Assert(date <= this.placements.First().From);
                    placedUntil = this.placements.First().From;
                }
            }

            if (tenancy == Tenancies.SoleTenant)
            {
                AddPlacement(new InstancePlacement(serverId, nodeType, date, placedUntil));
            }
            else
            {
                AddPlacement(new InstancePlacement(date, placedUntil));
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        private InstanceHistoryBuilder(
            ulong instanceId,
            InstanceLocator reference,
            ImageLocator image,
            InstanceState state,
            DateTime? lastSeen,
            Tenancies tenancy,
            string serverId,
            NodeTypeLocator nodeType)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(tenancy == Tenancies.SoleTenant || (serverId == null && nodeType == null));

            if (instanceId == 0)
            {
                throw new ArgumentException("Instance ID cannot be 0");
            }

            this.InstanceId = instanceId;
            this.reference = reference;
            this.image = image;
            this.lastStoppedOn = lastSeen;

            if (state == InstanceState.Running)
            {
                Debug.Assert(tenancy != Tenancies.Unknown);
                Debug.Assert(lastSeen != null);

                AddPlacement(new InstancePlacement(
                    tenancy, 
                    serverId, 
                    nodeType, 
                    lastSeen.Value, 
                    lastSeen.Value));
            }
        }

        internal static InstanceHistoryBuilder ForExistingInstance(
            ulong instanceId,
            InstanceLocator reference,
            ImageLocator image,
            InstanceState state,
            DateTime lastSeen,
            Tenancies tenancy,
            string serverId,
            NodeTypeLocator nodeType)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(state != InstanceState.Deleted);

            return new InstanceHistoryBuilder(
                instanceId,
                reference,
                image,
                state,
                lastSeen,
                tenancy,
                serverId,
                nodeType);
        }

        internal static InstanceHistoryBuilder ForDeletedInstance(ulong instanceId)
        {
            return new InstanceHistoryBuilder(
                instanceId,
                null,
                null,
                InstanceState.Deleted,
                (DateTime?)null,    // Not clear yet when it was stopped
                Tenancies.Unknown,
                null,
                null);
        }

        public InstanceHistory Build(DateTime reportStartDate)
        {
            IEnumerable<InstancePlacement> sanitizedPlacements = this.placements;

            if (placements.Count() == 1 &&
                placements.First() is InstancePlacement firstPlacement &&
                firstPlacement.From == this.lastStoppedOn &&
                firstPlacement.To == this.lastStoppedOn)
            {
                // This instance is running, but we did not see a 
                // start event -- so the instance must have been started
                // even earlier.
                //
                // Keeping the (synthetic) placement would cause statistics
                // to count this instance as not running - therefore, extend
                // the placement so that it covers the entire analyzed time
                // frame. 
                sanitizedPlacements = new[]
                {
                    new InstancePlacement(
                        firstPlacement.Tenancy,
                        firstPlacement.ServerId,
                        firstPlacement.NodeType,
                        reportStartDate,
                        firstPlacement.To)
                };
            }

            Debug.Assert(sanitizedPlacements.All(p => p.From != p.To));

            return new InstanceHistory(
                this.InstanceId,
                this.reference,
                this.State,
                this.image,
                sanitizedPlacements);
        }

        public InstanceHistoryState State
        {
            get
            {
                if (this.missingStopEvent)
                {
                    return InstanceHistoryState.MissingStopEvent;
                }
                else if (this.Tenancy == Tenancies.Unknown)
                {
                    return InstanceHistoryState.MissingTenancy;
                }
                else if (this.reference == null)
                {
                    return InstanceHistoryState.MissingName;
                }
                else if (this.Tenancy == Tenancies.SoleTenant && this.image == null)
                {
                    return InstanceHistoryState.MissingImage;
                }
                else
                {
                    return InstanceHistoryState.Complete;
                }

            }
        }

        //---------------------------------------------------------------------
        // Lifecycle events that construct the history.
        //---------------------------------------------------------------------

        public void OnInsert(DateTime date, InstanceLocator reference, ImageLocator image)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            // NB. We might get multiple calls for a single instance, each providing some, but
            // potentially not all information.
            if (this.reference == null)
            {
                this.reference = reference;
            }

            if (this.image == null)
            {
                this.image = image;
            }

            // Register Fleet placement - this might be merged with an existing
            // SoleTenant placement if there has been one registerd before.
            AddPlacement(Tenancies.Fleet, null, null, date);
        }

        public void OnStart(DateTime date, InstanceLocator reference)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            if (this.reference == null)
            {
                this.reference = reference;
            }

            // Register Fleet placement - this might be merged with an existing
            // SoleTenant placement if there has been one registerd before.
            AddPlacement(Tenancies.Fleet, null, null, date);
        }

        public void OnStop(DateTime date, InstanceLocator reference)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            this.lastStoppedOn = date;

            if (this.reference == null)
            {
                this.reference = reference;
            }
        }

        public void OnSetPlacement(string serverId, NodeTypeLocator nodeType, DateTime date)
        {
            Debug.Assert(date <= this.lastEventDate);
            Debug.Assert(serverId != null);
            this.lastEventDate = date;

            // NB. While the serverId is always populated, the nodeType
            // is null for events emitted before August 2020.

            AddPlacement(Tenancies.SoleTenant, serverId, nodeType, date);
        }

        //---------------------------------------------------------------------
        // IEventProcessor
        //---------------------------------------------------------------------

        public EventOrder ExpectedOrder => ProcessingOrder;

        public IEnumerable<string> SupportedSeverities => ProcessingSeverities;

        public IEnumerable<string> SupportedMethods => ProcessingMethods;

        public void Process(EventBase e)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(e))
            {
                if (e is NotifyInstanceLocationEvent notifyLocation)
                {
                    OnSetPlacement(
                        notifyLocation.ServerId, 
                        notifyLocation.NodeType, 
                        notifyLocation.Timestamp);
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
}
