//
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
using Google.Solutions.Common.Util;
using Google.Apis.Compute.v1;
using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Solutions.Common;

namespace Google.Solutions.LogAnalysis.History
{
    public class InstanceSetHistoryBuilder : IEventProcessor
    {
        private readonly IDictionary<ulong, InstanceHistoryBuilder> instanceBuilders =
            new Dictionary<ulong, InstanceHistoryBuilder>();

        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        private static string ShortZoneIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        private InstanceHistoryBuilder GetInstanceHistoryBuilder(ulong instanceId)
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
                startDate.Kind != DateTimeKind.Utc)
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
            VmInstanceReference reference,
            GlobalResourceReference image,
            InstanceState state,
            DateTime lastSeen,
            Tenancy tenancy)
        {
            this.instanceBuilders[instanceId] = InstanceHistoryBuilder.ForExistingInstance(
                instanceId,
                reference,
                image,
                state,
                lastSeen,
                tenancy);
        }

        public async Task AddExistingInstances(
            InstancesResource instancesResource,
            DisksResource disksResource,
            string projectId)
        {
            // Instances.list returns the disks associated with each
            // instance, but lacks the information about the source image.
            // Therefore, load disks separately.

            // TODO: Tracing

            // TODO: Paging
            var disks = await disksResource.AggregatedList(projectId).ExecuteAsync();
            var sourceImagaesByDisk = disks.Items == null
                ? new Dictionary<string, string>()
                : disks.Items.Values
                    .Where(v => v.Disks != null)
                    .EnsureNotNull()
                    .SelectMany(v => v.Disks)
                    .EnsureNotNull()
                    .ToDictionary(d => d.SelfLink, d => d.SourceImage);

            // TODO: Paging
            var instances = await instancesResource.AggregatedList(projectId).ExecuteAsync();
            if (instances.Items != null)
            {
                foreach (var list in instances.Items.Values)
                {
                    if (list.Instances == null)
                    {
                        continue;
                    }

                    foreach (var instance in list.Instances)
                    {
                        var bootDiskUrl = instance.Disks
                            .EnsureNotNull()
                            .Where(d => d.Boot != null && d.Boot.Value)
                            .EnsureNotNull()
                            .Select(d => d.Source)
                            .EnsureNotNull()
                            .FirstOrDefault();
                        GlobalResourceReference image = null;
                        if (bootDiskUrl != null &&
                            sourceImagaesByDisk.TryGetValue(bootDiskUrl, out string imageUrl) &&
                            imageUrl != null)
                        {
                            image = GlobalResourceReference.FromString(imageUrl);
                        }

                        AddExistingInstance(
                            (ulong)instance.Id.Value,
                            new VmInstanceReference(
                                projectId,
                                ShortZoneIdFromUrl(instance.Zone),
                                instance.Name),
                            image,
                            instance.Status == "RUNNING"
                                ? InstanceState.Running
                                : InstanceState.Terminated,
                            DateTime.Now,
                            instance.Scheduling.NodeAffinities != null && instance.Scheduling.NodeAffinities.Any()
                                ? Tenancy.SoleTenant
                                : Tenancy.Fleet);
                    }
                }
            }
        }

        public InstanceSetHistory Build()
        {
            return new InstanceSetHistory(
                this.StartDate,
                this.EndDate,
                this.instanceBuilders.Values.Select(b => b.Build()).ToList());
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
