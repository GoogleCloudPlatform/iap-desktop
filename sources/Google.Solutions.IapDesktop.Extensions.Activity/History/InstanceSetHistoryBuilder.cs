﻿//
// Copyright 2020 Google LLC
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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.History
{
    public class InstanceSetHistoryBuilder : IEventProcessor
    {
        private readonly IDictionary<ulong, InstanceHistoryBuilder> instanceBuilders =
            new Dictionary<ulong, InstanceHistoryBuilder>();

        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        private static string ShortZoneIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        internal InstanceHistoryBuilder GetInstanceHistoryBuilder(ulong instanceId)
        {
            if (this.instanceBuilders.TryGetValue(instanceId, out InstanceHistoryBuilder builder))
            {
                return builder;
            }
            else
            {
                var newBuilder = InstanceHistoryBuilder.ForDeletedInstance(instanceId);
                this.instanceBuilders[instanceId] = newBuilder;
                return newBuilder;
            }
        }

        public InstanceSetHistoryBuilder(
            DateTime startDate,
            DateTime endDate)
        {
            if (startDate.Kind != DateTimeKind.Utc ||
                endDate.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Start/end date must be in UTC time");
            }

            if (startDate > endDate)
            {
                throw new ArgumentException("Start date and end date are reversed");
            }

            this.StartDate = startDate;
            this.EndDate = endDate;
        }

        public void AddExistingInstance(
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
            this.instanceBuilders[instanceId] = InstanceHistoryBuilder.ForExistingInstance(
                instanceId,
                reference,
                image,
                state,
                lastSeen,
                tenancy,
                serverId,
                nodeType);
        }

        public void AddExistingInstances(
            IEnumerable<Instance> instances,
            IEnumerable<NodeGroupNode> nodes,
            IEnumerable<Disk> disks,
            string projectId)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                //
                // NB. Instances.list returns the disks associated with each
                // instance, but lacks the information about the source image.
                // Therefore, we load disks first and then join the data.
                //
                var sourceImagesByDisk = disks
                    .EnsureNotNull()
                    .ToDictionary(d => d.SelfLink, d => d.SourceImage);

                ApplicationTraceSources.Default.TraceVerbose("Found {0} existing disks", sourceImagesByDisk.Count());
                ApplicationTraceSources.Default.TraceVerbose("Found {0} existing instances", instances.Count());

                foreach (var instance in instances)
                {
                    ApplicationTraceSources.Default.TraceVerbose("Adding {0}", instance.Id);

                    var bootDiskUrl = instance.Disks
                        .EnsureNotNull()
                        .Where(d => d.Boot != null && d.Boot.Value)
                        .EnsureNotNull()
                        .Select(d => d.Source)
                        .EnsureNotNull()
                        .FirstOrDefault();
                    ImageLocator image = null;
                    if (bootDiskUrl != null &&
                        sourceImagesByDisk.TryGetValue(bootDiskUrl, out string imageUrl) &&
                        imageUrl != null)
                    {
                        image = ImageLocator.FromString(imageUrl);
                    }

                    var instanceLocator = new InstanceLocator(
                            projectId,
                            ShortZoneIdFromUrl(instance.Zone),
                            instance.Name);

                    if (instance.Scheduling.NodeAffinities != null && instance.Scheduling.NodeAffinities.Any())
                    {
                        // This VM runs on a sole-tenant node.
                        var node = nodes.FirstOrDefault(n => n.Instances
                            .EnsureNotNull()
                            .Select(uri => InstanceLocator.FromString(uri))
                            .Any(locator => locator == instanceLocator));
                        if (node == null)
                        {
                            ApplicationTraceSources.Default.TraceWarning(
                                "Could not identify node {0} is scheduled on",
                                instanceLocator);
                        }

                        AddExistingInstance(
                            (ulong)instance.Id.Value,
                            instanceLocator,
                            image,
                            instance.Status == "RUNNING"
                                ? InstanceState.Running
                                : InstanceState.Terminated,
                            this.EndDate,
                            Tenancies.SoleTenant,
                            node?.ServerId,
                            node?.NodeType != null
                                ? NodeTypeLocator.FromString(node.NodeType)
                                : null);
                    }
                    else
                    {
                        // Fleet VM.
                        AddExistingInstance(
                            (ulong)instance.Id.Value,
                            instanceLocator,
                            image,
                            instance.Status == "RUNNING"
                                ? InstanceState.Running
                                : InstanceState.Terminated,
                            this.EndDate,
                            Tenancies.Fleet,
                            null,
                            null);
                    }
                }
            }
        }

        public InstanceSetHistory Build()
        {
            return new InstanceSetHistory(
                this.StartDate,
                this.EndDate,
                this.instanceBuilders.Values.Select(b => b.Build(this.StartDate)).ToList());
        }

        //---------------------------------------------------------------------
        // IEventProcessor
        //---------------------------------------------------------------------

        public EventOrder ExpectedOrder => InstanceHistoryBuilder.ProcessingOrder;

        public IEnumerable<string> SupportedSeverities => InstanceHistoryBuilder.ProcessingSeverities;

        public IEnumerable<string> SupportedMethods => InstanceHistoryBuilder.ProcessingMethods;

        public void Process(EventBase e)
        {
            // NB. Some events (such as recreateInstance) might not have an instance ID.
            // These are useless for our purpose.
            if (e is VmInstanceEventBase instanceEvent && instanceEvent.InstanceId != 0)
            {
                GetInstanceHistoryBuilder(instanceEvent.InstanceId).Process(e);
            }
        }
    }
}
