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
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceAccount = Google.Apis.Compute.v1.Data.ServiceAccount;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.Common.Test.Integration
{
    public static class InstanceFactory
    {
        internal const string GuestAttributeNamespace = "boot";
        internal const string GuestAttributeToAwaitKey = "guest-attribute-to-await";

        private static async Task AwaitInstanceCreatedAndReady(
            InstancesResource resource,
            InstanceLocator locator)
        {
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    var instance = await resource.Get(
                            locator.ProjectId,
                            locator.Zone,
                            locator.Name)
                        .ExecuteAsync();

                    // Determine the name of the guest attribute we need to await. 
                    var guestAttributeToAwait = instance.Metadata.Items
                        .EnsureNotNull()
                        .FirstOrDefault(item => item.Key == GuestAttributeToAwaitKey)
                        .Value;

                    var request = resource.GetGuestAttributes(
                        locator.ProjectId,
                        locator.Zone,
                        locator.Name);
                    request.QueryPath = GuestAttributeNamespace + "/";
                    var guestAttributes = await request.ExecuteAsync();

                    if (guestAttributes
                        .QueryValue
                        .Items
                        .Where(item => item.Namespace__ == GuestAttributeNamespace &&
                                       item.Key == guestAttributeToAwait)
                        .Any())
                    {
                        return;
                    }
                }
                catch (Exception)
                { }

                TraceSources.Common.TraceVerbose(
                    "Waiting for instance {0} to become ready...", locator.Name);

                await Task.Delay(5 * 1000);
            }

            throw new TimeoutException($"Timeout waiting for {locator} to become ready");
        }

        private static async Task<string> GetComputeEngineDefaultServiceAccount()
        {
            var iamService = TestProject.CreateIamService();
            var allServiceAccounts = await iamService.Projects.ServiceAccounts
                .List($"projects/{TestProject.ProjectId}")
                .ExecuteAsync();
            return allServiceAccounts
                .Accounts
                .First(sa => sa.Email.EndsWith("compute@developer.gserviceaccount.com"))
                .Email;
        }

        public static async Task<InstanceLocator> CreateOrStartInstanceAsync(
            string name,
            string machineType,
            string imageFamily,
            InstanceServiceAccount serviceAccount,
            IEnumerable<Metadata.ItemsData> metadataItems)
        {
            var computeEngine = TestProject.CreateComputeService();

            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>(metadataItems.ToList())
            };

            // Add metdata that marks this instance as temporary.
            metadata.Add("type", "auto-cleanup");
            metadata.Add("ttl", "120"); // minutes

            var locator = new InstanceLocator(
                TestProject.ProjectId,
                TestProject.Zone,
                name);

            try
            {
                TraceSources.Common.TraceVerbose(
                    "Trying to create new instance {0}...", name);

                IList<ServiceAccount> serviceAccounts = null;
                if (serviceAccount == InstanceServiceAccount.ComputeDefault)
                {
                    serviceAccounts = new List<ServiceAccount>()
                    {
                        new ServiceAccount()
                        {
                            Email = await GetComputeEngineDefaultServiceAccount(),
                            Scopes = new [] { "https://www.googleapis.com/auth/cloud-platform" }
                        }
                    };
                }

                await computeEngine.Instances.Insert(
                    new Apis.Compute.v1.Data.Instance()
                    {
                        Name = name,
                        MachineType = $"zones/{locator.Zone}/machineTypes/{machineType}",
                        Disks = new[]
                        {
                            new AttachedDisk()
                            {
                                AutoDelete = true,
                                Boot = true,
                                InitializeParams = new AttachedDiskInitializeParams()
                                {
                                    SourceImage = imageFamily
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
                        },
                        ServiceAccounts = serviceAccounts
                    },
                    locator.ProjectId,
                    locator.Zone).ExecuteAsync();

                await AwaitInstanceCreatedAndReady(
                    computeEngine.Instances,
                    locator);

                return locator;
            }
            catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 409)
            {
                // Instance already exists - make sure it's running then.
                var instance = await computeEngine.Instances
                    .Get(
                        locator.ProjectId,
                        locator.Zone,
                        locator.Name)
                    .ExecuteAsync();

                if (instance.Status == "RUNNING" ||
                    instance.Status == "PROVISIONING" ||
                    instance.Status == "STAGING")
                {
                    TraceSources.Common.TraceVerbose(
                        "Instance {0} exists and is running...", locator.Name);

                    await AwaitInstanceCreatedAndReady(
                        computeEngine.Instances,
                        locator);
                    return locator;
                }
                else if (instance.Status == "TERMINATED")
                {
                    TraceSources.Common.TraceVerbose(
                        "Instance {0} exists, but is TERMINATED, starting...", locator.Name);

                    // Reapply metadata.
                    await computeEngine.Instances.AddMetadataAsync(
                            locator,
                            metadata,
                            CancellationToken.None);

                    await computeEngine.Instances.Start(
                            locator.ProjectId,
                            locator.Zone,
                            locator.Name)
                        .ExecuteAsync();

                    await AwaitInstanceCreatedAndReady(
                        computeEngine.Instances,
                        locator);
                    return locator;
                }
                else
                {
                    TraceSources.Common.TraceError(
                        "Creating instance {0} failed, current status is {1}",
                        locator.Name,
                        instance.Status);
                    TraceSources.Common.TraceError(e);
                    throw;
                }
            }
        }
    }
}
