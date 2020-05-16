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
using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.LogAnalysis.History
{
    public class InstanceSetHistoryBuilder : IEventProcessor
    {
        private readonly IDictionary<long, InstanceHistoryBuilder> instanceBuilders =
            new Dictionary<long, InstanceHistoryBuilder>();

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
            if (e is VmInstanceEventBase instanceEvent)
            {
                GetBuilder(instanceEvent.InstanceId).Process(e);
            }
        }
    }
}
