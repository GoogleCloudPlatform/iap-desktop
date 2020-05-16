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
using Google.Apis.Compute.v1;
using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.History
{
    public class InstanceSetHistoryBuilder : IEventProcessor
    {
        private readonly IDictionary<long, InstanceHistoryBuilder> instanceBuilders =
            new Dictionary<long, InstanceHistoryBuilder>();

        private static string ShortZoneIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        private InstanceHistoryBuilder GetBuilder(long instanceId)
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

        public void AddExistingInstance(
            long instanceId,
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
            var sourceImagaesByDisk = disks.Items.Values
                .Where(v => v.Disks != null)
                .EnsureNotNull()
                .SelectMany(v => v.Disks)
                .EnsureNotNull()
                .ToDictionary(d => d.SelfLink, d => d.SourceImage);

            // TODO: Paging
            var instances = await instancesResource.AggregatedList(projectId).ExecuteAsync();
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
                        (long)instance.Id.Value,
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

        public InstanceSetHistory Build()
        {
            var complete = this.instanceBuilders.Values.Where(b => !b.IsMoreInformationNeeded);
            var incomplete = this.instanceBuilders.Values.Where(b => b.IsMoreInformationNeeded);

            Debug.Assert(complete.All(i => i.Tenancy != Tenancy.Unknown));

            return new InstanceSetHistory(
                complete.Select(b => b.Build()),
                incomplete.Select(b => b.Build()));
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
                GetBuilder(instanceEvent.InstanceId).Process(e);
            }
        }
    }
}
