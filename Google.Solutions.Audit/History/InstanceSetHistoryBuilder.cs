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
    internal class InstanceSetHistoryBuilder
    {
        private readonly IDictionary<long, InstanceHistoryBuilder> instanceBuilders =
            new Dictionary<long, InstanceHistoryBuilder>();

        private InstanceHistoryBuilder GetBuilder(long instanceId, VmInstanceReference reference)
        {
            if (this.instanceBuilders.TryGetValue(instanceId, out InstanceHistoryBuilder builder))
            {
                Debug.Assert(builder.Reference == reference);
                return builder;
            }
            else
            {
                var newBuilder = InstanceHistoryBuilder.ForDeletedInstance(instanceId, reference);
                this.instanceBuilders[instanceId] = newBuilder;
                return newBuilder;
            }
        }

        public void AddExistingInstance(
            long instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image,
            DateTime lastSeen,
            Tenancy tenancy)
        {
            this.instanceBuilders[instanceId] = InstanceHistoryBuilder.ForExistingInstance(
                instanceId,
                reference,
                image,
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
        // Lifecycle events that construct the history.
        //---------------------------------------------------------------------

        public void OnInsert(
            DateTime date, 
            long instanceId,
            VmInstanceReference reference,
            GlobalResourceReference image)
        {
            GetBuilder(instanceId, reference).OnInsert(date, image);
        }

        public void OnStart(
            DateTime date,
            long instanceId,
            VmInstanceReference reference)
        {
            GetBuilder(instanceId, reference).OnStart(date);
        }

        public void OnStop(
            DateTime date,
            long instanceId,
            VmInstanceReference reference)
        {
            GetBuilder(instanceId, reference).OnStop(date);
        }

        public void OnSetPlacement(
            DateTime date,
            string serverId, 
            long instanceId,
            VmInstanceReference reference)
        {
            GetBuilder(instanceId, reference).OnSetPlacement(serverId, date);
        }

        public void OnEvent(VmInstanceEventBase e)
        {
            GetBuilder(e.InstanceId, e.InstanceReference).OnEvent(e);
        }
    }
}
