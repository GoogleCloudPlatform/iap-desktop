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
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Integration
{
    public class InstanceRequest
    {
        internal const string GuestAttributeNamespace = "boot";

        // Use a new key per test run. This ensures that when an existing
        // instance is started (as opposed to created), we want till the
        // startup script has been re-run.
        internal static readonly string GuestAttributeKey = 
            "completed-" + Guid.NewGuid().ToString().Substring(0, 8);

        private readonly ComputeService computeService;
        private readonly IEnumerable<Metadata.ItemsData> metadata;
        private readonly string machineType;
        private readonly string imageFamily;

        public InstanceLocator Locator { get; }

        private async Task AwaitInstanceCreatedAndReady()
        {
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    var instance = await this.computeService.Instances.Get(
                            this.Locator.ProjectId,
                            this.Locator.Zone,
                            this.Locator.Name)
                        .ExecuteAsync();

                    if (await IsReadyAsync(instance))
                    {
                        return;
                    }
                }
                catch (Exception)
                { }

                TraceSources.Common.TraceVerbose(
                    "Waiting for instance {0} to become ready...", this.Locator.Name);

                await Task.Delay(5 * 1000);
            }

            throw new TimeoutException($"Timeout waiting for {this.Locator} to become ready");
        }

        private async Task<bool> IsReadyAsync(
            Instance instance)
        {
            var request = this.computeService.Instances.GetGuestAttributes(
                    this.Locator.ProjectId,
                    this.Locator.Zone,
                    this.Locator.Name);
            request.QueryPath = GuestAttributeNamespace + "/";
            var guestAttributes = await request.ExecuteAsync();

            return guestAttributes
                .QueryValue
                .Items
                .Where(i => i.Namespace__ == GuestAttributeNamespace && i.Key == GuestAttributeKey)
                .Any();
        }

        private async Task CreateOrStartInstanceAsync()
        {
            var computeEngine = TestProject.CreateComputeService();

            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>(this.metadata.ToList())
            };

            // Add metdata that marks this instance as temporary.
            metadata.Add("type", "auto-cleanup");
            metadata.Add("ttl", "120"); // minutes

            try
            {
                TraceSources.Common.TraceVerbose(
                    "Trying to create new instance {0}...", this.Locator.Name);

                await computeEngine.Instances.Insert(
                    new Apis.Compute.v1.Data.Instance()
                    {
                        Name = this.Locator.Name,
                        MachineType = $"zones/{this.Locator.Zone}/machineTypes/{this.machineType}",
                        Disks = new[]
                        {
                            new AttachedDisk()
                            {
                                AutoDelete = true,
                                Boot = true,
                                InitializeParams = new AttachedDiskInitializeParams()
                                {
                                    SourceImage = this.imageFamily
                                }
                            }
                        },
                        Metadata = metadata,
                        NetworkInterfaces = new[]
                        {
                            new NetworkInterface()
                            {
                                AccessConfigs = new []
                                {
                                    new AccessConfig()
                                }
                            }
                        },
                        Scheduling = new Scheduling()
                        {
                            Preemptible = true
                        }
                    },
                    this.Locator.ProjectId,
                    this.Locator.Zone).ExecuteAsync();

                await AwaitInstanceCreatedAndReady();
            }
            catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 409)
            {
                // Instance already exists - make sure it's running then.
                var instance = await computeEngine.Instances
                    .Get(
                        this.Locator.ProjectId,
                        this.Locator.Zone,
                        this.Locator.Name)
                    .ExecuteAsync();

                if (instance.Status == "RUNNING")
                {
                    TraceSources.Common.TraceVerbose(
                        "Instance {0} exists, checking that it is ready...", this.Locator.Name);
                    
                    await AwaitInstanceCreatedAndReady();
                }
                else if (instance.Status == "TERMINATED")
                {
                    TraceSources.Common.TraceVerbose(
                        "Instance {0} exists, but is TERMINATED, starting...", this.Locator.Name);

                    // Reapply metadata.
                    await computeEngine.Instances.AddMetadataAsync(
                            this.Locator,
                            metadata,
                            CancellationToken.None);

                    await computeEngine.Instances.Start(
                            this.Locator.ProjectId,
                            this.Locator.Zone,
                            this.Locator.Name)
                        .ExecuteAsync();

                    await AwaitInstanceCreatedAndReady();
                }
                else
                {
                    TraceSources.Common.TraceError("Creating instance {0} failed...", this.Locator.Name);
                    TraceSources.Common.TraceError(e);
                    throw;
                }
            }
        }

        public InstanceRequest(
            InstanceLocator instance,
            string machineType,
            string imageFamily,
            IEnumerable<Metadata.ItemsData> metadata)
        {
            this.computeService = TestProject.CreateComputeService();
            this.Locator = instance;
            this.machineType = machineType;
            this.imageFamily = imageFamily;
            this.metadata = metadata;
        }

        public async Task<InstanceLocator> GetInstanceAsync()
        {
            await CreateOrStartInstanceAsync();
            await AwaitInstanceCreatedAndReady();
            return this.Locator;
        }

        public async Task AwaitReady()
        {
            await CreateOrStartInstanceAsync();
            await AwaitInstanceCreatedAndReady();
        }

        public override string ToString()
        {
            return this.Locator.ToString();
        }
    }
}
