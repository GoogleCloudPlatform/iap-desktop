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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Solutions.Compute;
using Google.Solutions.Compute.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    /// <summary>
    /// Adapter class for the Compute Engine API.
    /// </summary>
    internal class ComputeEngineAdapter
    {
        private const string WindowsCloudLicenses = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses";

        private readonly ComputeService service;

        public static ComputeEngineAdapter Create(ICredential credential)
        {
            var assemblyName = typeof(ComputeEngineAdapter).Assembly.GetName();
            return new ComputeEngineAdapter(new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = $"{assemblyName.Name}/{assemblyName.Version}"
            }));
        }

        private ComputeEngineAdapter(ComputeService service)
        {
            this.service = service;
        }

        private async Task<IEnumerable<TValue>> QueryPagedResourceAsync<TRequest, TResponse, TValue>(
            TRequest request,
            Func<TResponse, IEnumerable<TValue>> mapFunc,
            Func<TResponse, string> getNextPageTokenFunc,
            Action<TRequest, string> setPageTokenFunc)
            where TRequest : IClientServiceRequest<TResponse>
        {
            TResponse response;
            var allValues = new List<TValue>();
            do
            {
                response = await request.ExecuteAsync();

                IEnumerable<TValue> pageValues = mapFunc(response);
                if (pageValues != null)
                {
                    allValues.AddRange(pageValues);
                }

                setPageTokenFunc(request, getNextPageTokenFunc(response));
            }
            while (getNextPageTokenFunc(response) != null);

            return allValues;
        }

        public async Task<IEnumerable<string>> QueryZonesAsync(string projectId)
        {
            var zones = await QueryPagedResourceAsync<ZonesResource.ListRequest, ZoneList, Zone>(
                 this.service.Zones.List(projectId),
                 zone => zone.Items,
                 response => response.NextPageToken,
                 (request, token) => { request.PageToken = token; });
            return zones.Select(z => z.Name);
        }

        public Task<IEnumerable<Instance>> QueryInstancesAsync(string projectId, string zone)
        {
            return QueryPagedResourceAsync<InstancesResource.ListRequest, InstanceList, Instance>(
                 this.service.Instances.List(projectId, zone),
                 instances => instances.Items,
                 response => response.NextPageToken,
                 (request, token) => { request.PageToken = token; });
        }

        public async Task<IEnumerable<Instance>> QueryInstancesAsync(string projectId)
        {
            var zones = await QueryPagedResourceAsync<
                        InstancesResource.AggregatedListRequest, 
                        InstanceAggregatedList, 
                        InstancesScopedList>(
                this.service.Instances.AggregatedList(projectId),
                instances => instances.Items.Values.Where(v => v != null),
                response => response.NextPageToken,
                (request, token) => { request.PageToken = token; });

            return zones
                .Where(z => z.Instances != null)    // API returns null for empty zones.
                .SelectMany(zone => zone.Instances);
        }

        public Task<Instance> QueryInstanceAsync(string projectId, string zone, string instanceName)
        {
            return this.service.Instances.Get(projectId, zone, instanceName).ExecuteAsync();
        }

        public Task<Instance> QueryInstanceAsync(VmInstanceReference instanceRef)
        {
            return QueryInstanceAsync(instanceRef.ProjectId, instanceRef.Zone, instanceRef.InstanceName);
        }

        public SerialPortStream GetSerialPortOutput(VmInstanceReference instanceRef)
        {
            return this.service.Instances.GetSerialPortOutputStream(instanceRef, 1);
        }

        public Task<NetworkCredential> ResetWindowsUserAsync(
            VmInstanceReference instanceRef, 
            string username)
        {
            return this.service.Instances.ResetWindowsUserAsync(instanceRef, username);
        }

        private static bool IsWindowsInstanceByGuestOsFeature(Instance instance)
        {
            // For an instance to be a valid Windows instance, at least one of the disks
            // (the boot disk) has to be marked as "WINDOWS". 
            // Note that older disks might lack this feature.
            return instance.Disks
                .Where(d => d.GuestOsFeatures != null)
                .SelectMany(d => d.GuestOsFeatures)
                .Any(f => f.Type == "WINDOWS");
        }

        private static bool IsWindowsInstanceByLicense(Instance instance)
        {
            // For an instance to be a valid Windows instance, at least one of the disks
            // has to have an associated Windows license. This is also true for
            // BYOL'ed instances.
            return instance.Disks
                .Where(d => d.Licenses != null)
                .SelectMany(d => d.Licenses)
                .Any(l => l.StartsWith(WindowsCloudLicenses));
        }

        public static bool IsWindowsInstance(Instance instance)
        {
            return IsWindowsInstanceByGuestOsFeature(instance) ||
                   IsWindowsInstanceByLicense(instance);
        }
    }
}
