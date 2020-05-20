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
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Extensions
{
    /// <summary>
    /// Extend 'InstancesResource' by a 'AddMetadataAsync' method. 
    /// </summary>
    public static class AddMetadataExtensions
    {
        /// <summary>
        /// Adds or overwrites a metadata key/value pair to a GCE 
        /// instance. Any existing metadata is kept as is.
        /// </summary>
        public static Task AddMetadataAsync(
            this InstancesResource resource,
            string project,
            string zone,
            string instance,
            string key,
            string value,
            CancellationToken token)
        {
            return AddMetadataAsync(
                resource,
                new VmInstanceReference(project, zone, instance),
                key,
                value,
                token);
        }

        /// <summary>
        /// Query a metadata entry for an instance.
        /// </summary>
        /// <returns>null if not set/found</returns>
        public static async Task<Metadata.ItemsData> QueryMetadataKeyAsync(
            this InstancesResource resource,
            VmInstanceReference instanceRef,
            string key)
        {
            var instance = await resource.Get(
                instanceRef.ProjectId,
                instanceRef.Zone,
                instanceRef.InstanceName).ExecuteAsync().ConfigureAwait(false);

            if (instance.Metadata == null || instance.Metadata.Items == null)
            {
                return null;
            }

            return instance.Metadata.Items
                .Where(i => i.Key == key)
                .FirstOrDefault();
        }

        /// <summary>
        /// Adds or overwrites a metadata key/value pair to a GCE 
        /// instance. Any existing metadata is kept as is.
        /// </summary>
        public static async Task AddMetadataAsync(
            this InstancesResource resource,
            VmInstanceReference instanceRef,
            string key,
            string value,
            CancellationToken token)
        {
            var instance = await resource.Get(
                instanceRef.ProjectId,
                instanceRef.Zone,
                instanceRef.InstanceName).ExecuteAsync(token).ConfigureAwait(false);
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

            TraceSources.Compute.TraceVerbose("Setting metdata {0} on {1}...", key, instanceRef.InstanceName);

            try
            {
                await resource.SetMetadata(
                    metadata,
                    instanceRef.ProjectId,
                    instanceRef.Zone,
                    instanceRef.InstanceName).ExecuteAndAwaitOperationAsync(instanceRef.ProjectId, token);
            }
            catch (GoogleApiException e)
            {
                TraceSources.Compute.TraceWarning(
                    "Setting metdata failed {0} (code error {1})", e.Message, 
                    e.Error?.Code);

                throw;
            }
        }
    }
}
