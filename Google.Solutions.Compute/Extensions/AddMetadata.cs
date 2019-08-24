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

using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Extensions
{
    public static class AddMetadataExtensions
    {
        public static Task AddMetadataAsync(
            this InstancesResource resource,
            string project,
            string zone,
            string instance,
            string key,
            string value)
        {
            return AddMetadataAsync(
                resource,
                new VmInstanceReference(project, zone, instance),
                key,
                value);
        }

        public static async Task AddMetadataAsync(
            this InstancesResource resource,
            VmInstanceReference instanceRef,
            string key, 
            string value)
        {
            var instance = await resource.Get(
                instanceRef.ProjectId, 
                instanceRef.Zone, 
                instanceRef.InstanceName).ExecuteAsync().ConfigureAwait(false);
            var metadata = instance.Metadata;

            if (metadata.Items == null)
            {
                metadata.Items = new List<Metadata.ItemsData>();
            }

            var existingEntry = metadata.Items
                .Where(i => i.Key == key)
                .FirstOrDefault();
            if (existingEntry != null)
            {
                existingEntry.Value = value;
            }
            else
            {
                metadata.Items.Add(new Metadata.ItemsData()
                {
                    Key = key,
                    Value = value
                });
            }

            await resource.SetMetadata(
                metadata,
                instanceRef.ProjectId,
                instanceRef.Zone,
                instanceRef.InstanceName).ExecuteAsync().ConfigureAwait(false);
        }
    }
}
