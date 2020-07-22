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
using Google.Solutions.Common.ApiExtensions.Request;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.ApiExtensions.Instance
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
                new InstanceLocator(project, zone, instance),
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
            InstanceLocator instanceRef,
            string key)
        {
            var instance = await resource.Get(
                instanceRef.ProjectId,
                instanceRef.Zone,
                instanceRef.Name).ExecuteAsync().ConfigureAwait(false);

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
        public static Task AddMetadataAsync(
            this InstancesResource resource,
            InstanceLocator instanceRef,
            string key,
            string value,
            CancellationToken token)
        {
            return AddMetadataAsync(
                resource,
                instanceRef,
                new Metadata()
                {
                    Items = new List<Metadata.ItemsData>()
                    {
                        new Metadata.ItemsData()
                        {
                            Key = key,
                            Value = value
                        }
                    }
                },
                token);
        }

        /// <summary>
        /// Adds or overwrites a metadata key/value pair to a GCE 
        /// instance. Any existing metadata is kept as is.
        /// </summary>
        public static async Task AddMetadataAsync(
            this InstancesResource resource,
            InstanceLocator instanceRef,
            Metadata metadata,
            CancellationToken token)
        {
            using (TraceSources.Common.TraceMethod().WithParameters(instanceRef))
            {
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    TraceSources.Common.TraceVerbose("Adding metadata {0} on {1}...", metadata, instanceRef.Name);

                    //
                    // NB. Metadata must be updated all-at-once. Therefore,
                    // fetch the existing entries first before merging them
                    // with the new entries.
                    //

                    var instance = await resource.Get(
                        instanceRef.ProjectId,
                        instanceRef.Zone,
                        instanceRef.Name).ExecuteAsync(token).ConfigureAwait(false);

                    var mergedMetadata = instance.Metadata;
                    mergedMetadata.Add(metadata);

                    try
                    {
                        await resource.SetMetadata(
                            mergedMetadata,
                            instanceRef.ProjectId,
                            instanceRef.Zone,
                            instanceRef.Name).ExecuteAndAwaitOperationAsync(instanceRef.ProjectId, token).ConfigureAwait(false);
                        break;
                    }
                    catch (GoogleApiException e)
                    {
                        if (e.Error != null && e.Error.Code == 412)
                        {
                            // Fingerprint mismatch - that happens when somebody else updated metadata
                            // in patallel. 

                            int backoff = 100;
                            TraceSources.Common.TraceWarning(
                                "SetMetadata failed with {0} - retrying after {1}ms", e.Message,
                                e.Error?.Code,
                                backoff);

                            await Task.Delay(backoff).ConfigureAwait(false);
                        }
                        else
                        {
                            TraceSources.Common.TraceWarning(
                                "Setting metdata failed {0} (code error {1})", e.Message,
                                e.Error?.Code);

                            throw;
                        }
                    }
                }
            }
        }
    }
}
