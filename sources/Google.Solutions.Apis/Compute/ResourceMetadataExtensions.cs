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
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Compute
{
    /// <summary>
    /// Extension methods for mutating project and instance metadata.
    /// </summary>
    public static class ResourceMetadataExtensions
    {
        private const uint DefaultAttempts = 6;

        /// <summary>
        /// Modify metadata using optimistic concurrency control.
        /// </summary>
        private static async Task UpdateMetadataAsync(
            Func<Task<Metadata>> readMetadata,
            Func<Metadata, Task> writeMetadata,
            Action<Metadata> updateMetadata,
            uint maxAttempts)
        {
            for (var attempt = 1; ; attempt++)
            {
                //
                // NB. Metadata must be updated all-at-once. Therefore,
                // fetch the existing entries first before merging them
                // with the new entries.
                //
                var metadata = await readMetadata()
                    .ConfigureAwait(false);

                updateMetadata(metadata);

                try
                {
                    await writeMetadata(metadata)
                        .ConfigureAwait(false);
                    break;
                }
                catch (Exception e) when (e.Unwrap() is GoogleApiException apiException)
                {
                    if (attempt == maxAttempts)
                    {
                        //
                        // That's enough, give up.
                        //
                        CommonTraceSource.Log.TraceWarning(
                            "SetMetadata failed with {0} (code error {1})", e.Message,
                            apiException.Error?.Code);

                        throw;
                    }
                    else if (
                        apiException.HttpStatusCode == HttpStatusCode.ServiceUnavailable ||
                        apiException.Error != null && apiException.Error.Code == 412)
                    {
                        //
                        // 412 indicates a conflict, meaning we lost the
                        // optimisitic concurrency control race agains
                        // someone else. 
                        //
                        // 503 indicates that the API is being flaky.
                        //
                        // In both cases, back off and retry the
                        // read/update/write operation.
                        //
                        var backoff = TimeSpan.FromMilliseconds(10 * attempt);
                        CommonTraceSource.Log.TraceWarning(
                            "SetMetadata failed with {0} (code error {1}) - retrying after {2}", 
                            e.Message,
                            apiException.Error?.Code,
                            backoff);

                        await Task
                            .Delay(backoff)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        CommonTraceSource.Log.TraceWarning(
                            "Setting metadata failed {0} (code error {1})", e.Message,
                            apiException.Error?.Code);

                        throw;
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Extension methods for modifying project metadata.
        //---------------------------------------------------------------------

        /// <summary>
        /// Project/common instance metadata.
        /// </summary>
        public static async Task UpdateMetadataAsync(
            this ProjectsResource resource,
            string projectId,
            Action<Metadata> updateMetadata,
            CancellationToken token,
            uint maxAttempts = DefaultAttempts)
        {
            using (CommonTraceSource.Log.TraceMethod().WithParameters(projectId))
            {
                await UpdateMetadataAsync(
                        async () =>
                        {
                            var project = await resource.Get(projectId)
                                .ExecuteAsync(token)
                                .ConfigureAwait(false);

                            return project.CommonInstanceMetadata;
                        },
                        async metadata =>
                        {
                            await resource.SetCommonInstanceMetadata(
                                    metadata,
                                    projectId)
                                .ExecuteAndAwaitOperationAsync(
                                    projectId,
                                    token)
                                .ConfigureAwait(false);
                        },
                        updateMetadata,
                        maxAttempts)
                    .ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Adds or overwrite a metadata key/value pair.
        /// Existing metadata is kept as is.
        /// </summary>
        public static Task AddMetadataAsync(
            this ProjectsResource resource,
            string projectId,
            string key,
            string value,
            CancellationToken token)
        {
            return AddMetadataAsync(
                resource,
                projectId,
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
           this ProjectsResource resource,
           string projectId,
           Metadata metadata,
           CancellationToken token)
        {
            return UpdateMetadataAsync(
                resource,
                projectId,
                existingMetadata =>
                {
                    existingMetadata.Add(metadata);
                },
                token);
        }

        //---------------------------------------------------------------------
        // Extension methods for modifying instance metadata.
        //---------------------------------------------------------------------

        /// <summary>
        /// Modify instance metadata.
        /// </summary>
        public static async Task UpdateMetadataAsync(
            this InstancesResource resource,
            InstanceLocator instanceRef,
            Action<Metadata> updateMetadata,
            CancellationToken token,
            uint maxAttempts = DefaultAttempts)
        {
            using (CommonTraceSource.Log.TraceMethod().WithParameters(instanceRef))
            {
                await UpdateMetadataAsync(
                        async () =>
                        {
                            var instance = await resource.Get(
                                    instanceRef.ProjectId,
                                    instanceRef.Zone,
                                    instanceRef.Name)
                                .ExecuteAsync(token)
                                .ConfigureAwait(false);

                            return instance.Metadata;
                        },
                        async metadata =>
                        {
                            await resource.SetMetadata(
                                    metadata,
                                    instanceRef.ProjectId,
                                    instanceRef.Zone,
                                    instanceRef.Name)
                                .ExecuteAndAwaitOperationAsync(
                                    instanceRef.ProjectId,
                                    token)
                                .ConfigureAwait(false);
                        },
                        updateMetadata,
                        maxAttempts)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds or overwrite a metadata key/value pair.
        /// Existing metadata is kept as is.
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

        //---------------------------------------------------------------------
        // Extension methods for reading metadata.
        //---------------------------------------------------------------------

        public static bool? GetFlag(this Instance instance, Project project, string flag)
        {
            //
            // NB. The instance value always takes precedence,
            // even if it's false.
            //

            var instanceValue = instance.Metadata.GetFlag(flag);
            if (instanceValue != null)
            {
                return instanceValue.Value;
            }

            var projectValue = project.CommonInstanceMetadata.GetFlag(flag);
            if (projectValue != null)
            {
                return projectValue.Value;
            }

            return null;
        }

        public static bool? GetFlag(this Project project, string flag)
        {
            return project.CommonInstanceMetadata.GetFlag(flag);
        }
    }
}
