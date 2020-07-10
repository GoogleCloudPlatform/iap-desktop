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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Testbed
{
    public class InstanceRequest
    {
        internal const string GuestAttributeNamespace = "boot";
        internal const string GuestAttributeKey = "completed";

        private readonly IEnumerable<Metadata.ItemsData> metadata;
        private readonly string machineType;
        private readonly string imageFamily;

        public Func<Task<InstanceLocator>> GetInstanceAsync { get; }
        public InstanceLocator Locator { get; }

        private Task AwaitReady(ComputeEngine engine, InstanceLocator instanceRef)
        {
            return Task.Run(async () =>
            {
                for (int i = 0; i < 60; i++)
                {
                    try
                    {
                        var instance = await engine.Service.Instances.Get(
                                instanceRef.ProjectId, instanceRef.Zone, instanceRef.Name)
                            .ExecuteAsync();

                        if (await IsReadyAsync(engine, instanceRef, instance))
                        {
                            return;
                        }
                    }
                    catch (Exception)
                    { }

                    await Task.Delay(5 * 1000);
                }

                throw new TimeoutException($"Timeout waiting for {instanceRef} to become ready");
            });
        }

        private async Task<bool> IsReadyAsync(
            ComputeEngine engine,
            InstanceLocator instanceRef,
            Instance instance)
        {
            var request = engine.Service.Instances.GetGuestAttributes(
                    instanceRef.ProjectId,
                    instanceRef.Zone,
                    instanceRef.Name);
            request.QueryPath = GuestAttributeNamespace + "/";
            var guestAttributes = await request.ExecuteAsync();

            return guestAttributes
                .QueryValue
                .Items
                .Where(i => i.Namespace__ == GuestAttributeNamespace && i.Key == GuestAttributeKey)
                .Any();
        }

        private async Task<InstanceLocator> CreateOrStartInstanceAsync(InstanceLocator vmRef)
        {
            var computeEngine = ComputeEngine.Connect();

            try
            {
                var instance = await computeEngine.Service.Instances
                    .Get(vmRef.ProjectId, vmRef.Zone, vmRef.Name)
                    .ExecuteAsync();

                if (instance.Status == "STOPPED")
                {
                    await computeEngine.Service.Instances.Start(
                        vmRef.ProjectId, vmRef.Zone, vmRef.Name)
                        .ExecuteAsync();
                }

                await AwaitReady(computeEngine, vmRef);
            }
            catch (Exception)
            {
                var metadata = new List<Metadata.ItemsData>(this.metadata.ToList());

                // Add metdata that marks this instance as temporary.
                metadata.Add(new Metadata.ItemsData()
                {
                    Key = "type",
                    Value = "auto-cleanup"
                });
                metadata.Add(new Metadata.ItemsData()
                {
                    Key = "ttl",
                    Value = "120" // minutes
                });

                await computeEngine.Service.Instances.Insert(
                    new Apis.Compute.v1.Data.Instance()
                    {
                        Name = vmRef.Name,
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
                        Metadata = new Metadata()
                        {
                            Items = metadata
                        },
                        NetworkInterfaces = new[]
                        {
                            new NetworkInterface()
                            {
                                AccessConfigs = new []
                                {
                                    new AccessConfig()
                                }
                            }
                        }
                    },
                    vmRef.ProjectId,
                    vmRef.Zone).ExecuteAsync();

                await AwaitReady(computeEngine, vmRef);
            }

            return vmRef;
        }

        public InstanceRequest(
            InstanceLocator instance,
            string machineType,
            string imageFamily,
            IEnumerable<Metadata.ItemsData> metadata)
        {
            this.Locator = instance;
            this.machineType = machineType;
            this.imageFamily = imageFamily;
            this.metadata = metadata;
        }

        public override string ToString()
        {
            return this.Locator.ToString();
        }

        public async Task AwaitReady()
        {
            await GetInstanceAsync();
        }
    }
}
