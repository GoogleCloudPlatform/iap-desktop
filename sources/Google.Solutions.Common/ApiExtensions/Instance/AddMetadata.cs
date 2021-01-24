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
using System;
using System.Collections.Generic;
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
        /// Modifies existing GCE metadata.
        /// </summary>
        public static async Task UpdateMetadataAsync(
            this InstancesResource resource,
            InstanceLocator instanceRef,
            Action<Metadata> updateMetadata,
            CancellationToken token)
        {
            using (CommonTraceSources.Default.TraceMethod().WithParameters(instanceRef))
            {
                var maxAttempts = 6;
                for (int attempt = 1; ; attempt++)
                {
                    CommonTraceSources.Default.TraceVerbose(
                        "Adding metadata on {0}...", 
                        instanceRef.Name);

                    //
                    // NB. Metadata must be updated all-at-once. Therefore,
                    // fetch the existing entries first before merging them
                    // with the new entries.
                    //

                    var instance = await resource.Get(
                        instanceRef.ProjectId,
                        instanceRef.Zone,
                        instanceRef.Name).ExecuteAsync(token).ConfigureAwait(false);

                    //
                    // Apply whatever update the caller wants to make.
                    //
                    var updatedMetadata = instance.Metadata;
                    updateMetadata(instance.Metadata);

                    try
                    {
                        await resource.SetMetadata(
                                updatedMetadata,
                                instanceRef.ProjectId,
                                instanceRef.Zone,
                                instanceRef.Name)
                            .ExecuteAndAwaitOperationAsync(
                                instanceRef.ProjectId, 
                                token)
                            .ConfigureAwait(false);
                        break;
                    }
                    catch (GoogleApiException e)
                    {
                        if (attempt == maxAttempts)
                        {
                            //
                            // That's enough, give up.
                            //
                            CommonTraceSources.Default.TraceWarning(
                                "SetMetadata failed with {0} (code error {1})", e.Message,
                                e.Error?.Code);

                            throw;
                        }

                        if (e.Error != null && e.Error.Code == 412)
                        {
                            // Fingerprint mismatch - that happens when somebody else updated metadata
                            // in patallel. 

                            int backoff = 100;
                            CommonTraceSources.Default.TraceWarning(
                                "SetMetadata failed with {0} (code error {1}) - retrying after {2}ms", e.Message,
                                e.Error?.Code,
                                backoff);

                            await Task.Delay(backoff).ConfigureAwait(false);
                        }
                        else
                        {
                            CommonTraceSources.Default.TraceWarning(
                                "Setting metdata failed {0} (code error {1})", e.Message,
                                e.Error?.Code);

                            throw;
                        }
                    }
                }
            }
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

        public static Task AddMetadataAsync(
           this InstancesResource resource,
           InstanceLocator instanceRef,
           Metadata metadata,
           CancellationToken token)
        {
            return UpdateMetadataAsync(
                resource,
                instanceRef,
                existingMetadata =>
                {
                    existingMetadata.Add(metadata);
                },
                token);
        }
    }
}
