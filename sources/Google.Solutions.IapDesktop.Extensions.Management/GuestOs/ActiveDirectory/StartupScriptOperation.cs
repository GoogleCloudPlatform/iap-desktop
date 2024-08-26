//
// Copyright 2022 Google LLC
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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory
{
    /// <summary>
    /// A long-running operation that involves swapping out
    /// an instance's startups scripts and modifying its
    /// metadata.
    /// </summary>
    public interface IStartupScriptOperation : IDisposable
    {
        /// <summary>
        /// Unique ID for this operation.
        /// </summary>
        Guid OperationId { get; }

        /// <summary>
        /// Instance to operate on.
        /// </summary>
        InstanceLocator Instance { get; }

        IComputeEngineClient ComputeClient { get; }

        /// <summary>
        /// Replace existing startup scripts.
        /// </summary>
        Task ReplaceStartupScriptAsync(
            string newStartupScript,
            CancellationToken cancellationToken);

        /// <summary>
        /// Restore all existing startup scripts.
        /// </summary>
        Task RestoreStartupScriptsAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Add a metadata key/value.
        /// </summary>
        Task SetMetadataAsync(
            string key,
            string value,
            CancellationToken cancellationToken);
    }

    internal sealed class StartupScriptOperation : IStartupScriptOperation
    {
        /// <summary>
        /// Name of a metadata key that indicates a concurrent
        /// operation.
        /// </summary>
        private readonly string guardKeyName;

        /// <summary>
        /// List of metadata items that have been temporarily replaced
        /// and must be restored in the end.
        /// </summary>
        private List<Metadata.ItemsData> itemsToRestore;

        /// <summary>
        /// List of metadata keys that have been added and must be
        /// removed in the end.
        /// </summary>
        private readonly List<string> itemsToCleanup = new List<string>();

        public Guid OperationId { get; }
        public InstanceLocator Instance { get; }
        public IComputeEngineClient ComputeClient { get; }

        private async Task<List<Metadata.ItemsData>> ReplaceMetadataItemsAsync(
            ICollection<string> keysToReplace,
            List<Metadata.ItemsData> newItems,
            bool failIfGuardKeyFound,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSource.Log.TraceMethod()
                .WithParameters(string.Join(", ", keysToReplace)))
            {
                List<Metadata.ItemsData>? oldItems = null;
                await this.ComputeClient.UpdateMetadataAsync(
                        this.Instance,
                        metadata =>
                        {
                            if (failIfGuardKeyFound)
                            {
                                //
                                // Fail if the guard key exists.
                                //
                                if (metadata.Items
                                    .EnsureNotNull()
                                    .Any(i => i.Key == this.guardKeyName))
                                {
                                    throw new InvalidOperationException(
                                        $"Found metadata key '{this.guardKeyName}', " +
                                        "indicating that a concurrent operation " +
                                        "is in progress");
                                }
                            }

                            //
                            // Read and remove existing items.
                            //
                            oldItems = metadata.Items
                                .EnsureNotNull()
                                .Where(i => keysToReplace.Contains(i.Key))
                                .ToList();

                            foreach (var item in oldItems)
                            {
                                metadata.Items.Remove(item);
                            }

                            if (metadata.Items == null)
                            {
                                metadata.Items = new List<Metadata.ItemsData>();
                            }

                            //
                            // Add new items.
                            //
                            foreach (var item in newItems)
                            {
                                metadata.Items.Add(item);
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                Debug.Assert(oldItems != null);
                return oldItems;
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        internal StartupScriptOperation(
            Guid operationId,
            InstanceLocator instance,
            string guardKeyName,
            IComputeEngineClient computeClient)
        {
            this.OperationId = operationId;
            this.Instance = instance;
            this.guardKeyName = guardKeyName;
            this.ComputeClient = computeClient;
        }


        public StartupScriptOperation(
            InstanceLocator instance,
            string guardKeyName,
            IComputeEngineClient computeClient)
            : this(Guid.NewGuid(), instance, guardKeyName, computeClient)
        {
        }

        public async Task ReplaceStartupScriptAsync(
            string newStartupScript,
            CancellationToken cancellationToken)
        {
            Debug.Assert(this.itemsToRestore == null);

            //
            // Replace startup scripts and set guard key
            // to block concurrent operations.
            //
            // Fail if there is a guard key in place already.
            //
            this.itemsToRestore = await ReplaceMetadataItemsAsync(
                    MetadataKeys.WindowsStartupScripts,
                    new List<Metadata.ItemsData>
                    {
                        new Metadata.ItemsData()
                        {
                            Key = MetadataKeys.WindowsStartupScriptPs1,
                            Value = newStartupScript
                        },
                        new Metadata.ItemsData()
                        {
                            Key = this.guardKeyName,
                            Value = this.OperationId.ToString()
                        },
                    },
                    true,
                    cancellationToken)
                .ConfigureAwait(false);

            this.itemsToCleanup.Add(MetadataKeys.WindowsStartupScriptPs1);
            this.itemsToCleanup.Add(this.guardKeyName);
        }

        public async Task RestoreStartupScriptsAsync(
            CancellationToken cancellationToken)
        {
            //
            // Restore the previous startup scripts and remove the
            // keys we added.
            //
            await ReplaceMetadataItemsAsync(
                    this.itemsToCleanup,
                    this.itemsToRestore,
                    false,
                    cancellationToken)
                .ConfigureAwait(false);

            this.itemsToCleanup.Clear();
            this.itemsToRestore.Clear();
        }

        public async Task SetMetadataAsync(
            string key,
            string value,
            CancellationToken cancellationToken)
        {
            await ReplaceMetadataItemsAsync(
                    new[] { key },
                    new List<Metadata.ItemsData>
                    {
                        new Metadata.ItemsData()
                        {
                            Key = key,
                            Value = value
                        }
                    },
                    false,
                    cancellationToken)
                .ConfigureAwait(false);

            this.itemsToCleanup.Add(key);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Debug.Assert(!this.itemsToRestore.EnsureNotNull().Any());
            Debug.Assert(!this.itemsToCleanup.EnsureNotNull().Any());
        }

        //---------------------------------------------------------------------
        // Constants.
        //---------------------------------------------------------------------

        internal static class MetadataKeys
        {
            /// <summary>
            /// PowerShell startup script.
            /// </summary>
            public const string WindowsStartupScriptPs1 = "windows-startup-script-ps1";
            public static readonly string[] WindowsStartupScripts = new[]
            {
                WindowsStartupScriptPs1,
                "windows-startup-script-cmd",
                "windows-startup-script-bat",
                "windows-startup-script-url"
            };
        }
    }
}
