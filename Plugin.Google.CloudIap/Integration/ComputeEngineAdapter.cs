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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Google.CloudIap.Integration
{
    /// <summary>
    /// Adapter class for the Compute Engine API.
    /// </summary>
    internal class ComputeEngineAdapter
    {
        private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

        private readonly ComputeService service;

        public static ComputeEngineAdapter Create(GoogleCredential credential)
        {
            return new ComputeEngineAdapter(new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "RdcPlugin/0.1",
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
        public Task<Instance> QueryInstanceAsync(string projectId, string zone, string instanceName)
        {
            return this.service.Instances.Get(projectId, zone, instanceName).ExecuteAsync();
        }

        public class SerialPortStream
        {
            private readonly ComputeService service;
            private string lastBuffer = string.Empty;
            public VmInstanceReference Instance { get; private set; }

            public SerialPortStream(ComputeService service, VmInstanceReference instanceRef)
            {
                this.service = service;
                this.Instance = instanceRef;
            }

            public async Task<string> ReadAsync()
            {
                var output = await this.service.Instances.GetSerialPortOutput(
                    this.Instance.ProjectId,
                    this.Instance.Zone,
                    this.Instance.InstanceName).ExecuteAsync();

                // N.B. The first call will return a genuinely new buffer
                // of output. On subsequent calls, we will receive the same
                // output again, potenially with some extra data at the end.
                string newOutput = null;
                if (output.Contents.Length > this.lastBuffer.Length)
                {
                    // New data received. 
                    newOutput = output.Contents.Substring(this.lastBuffer.Length);
                }
                else if (output.Contents == this.lastBuffer)
                {
                    // Nothing happened since last read.
                    return string.Empty;
                }
                else if (output.Contents.Length == this.lastBuffer.Length)
                {
                    // We must have reached the max buffer size. Assuming the buffers
                    // still overlap, we can try to stitch things together.
                    int lastBufferTailLength = Math.Min(128, this.lastBuffer.Length);
                    var lastBufferTail = this.lastBuffer.Substring(
                        this.lastBuffer.Length - lastBufferTailLength, 
                        lastBufferTailLength);

                    int indexOfLastBufferTailInOutput = output.Contents.LastIndexOf(lastBufferTail);
                    if (indexOfLastBufferTailInOutput > 0)
                    {
                        newOutput = output.Contents.Substring(indexOfLastBufferTailInOutput + lastBufferTailLength);
                    }
                    else
                    {
                        // Seems like there is no overlap -- just return everyting then.
                        newOutput = output.Contents;
                    }
                }

                this.lastBuffer = output.Contents;
                return newOutput;
            }
        }

        public SerialPortStream GetSerialPortOutput(VmInstanceReference instanceRef)
        {
            return new SerialPortStream(this.service, instanceRef);
        }

        public static bool IsWindowsInstance(Instance instance)
        {
            // For an instance to be a valid Windows instance, at least one of the disks
            // (the boot disk) has to be marked as "WINDOWS". 
            return instance.Disks
                .Where(d => d.GuestOsFeatures != null)
                .SelectMany(d => d.GuestOsFeatures)
                .Any(f => f.Type == "WINDOWS");
        }
    }
}
