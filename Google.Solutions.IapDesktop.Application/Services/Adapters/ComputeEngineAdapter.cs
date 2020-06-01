//
// Copyright 2020 Google LLC
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
using Google.Apis.Services;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IComputeEngineAdapter : IDisposable
    {
        Task<IEnumerable<Instance>> ListInstancesAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<Disk>> ListDisksAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token);

        Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token,
            TimeSpan timeout);

        Task<Image> GetImage(
            ImageLocator image, 
            CancellationToken cancellationToken);

        SerialPortStream GetSerialPortOutput(InstanceLocator instanceRef);
    }

    /// <summary>
    /// Adapter class for the Compute Engine API.
    /// </summary>
    public class ComputeEngineAdapter : IComputeEngineAdapter
    {
        private static readonly TimeSpan DefaultPasswordResetTimeout = TimeSpan.FromSeconds(15);

        private readonly ComputeService service;

        public ComputeEngineAdapter(ICredential credential)
        {
            var assemblyName = typeof(IComputeEngineAdapter).Assembly.GetName();
            this.service = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = $"{assemblyName.Name}/{assemblyName.Version}"
            });
        }

        public ComputeEngineAdapter(IAuthorizationAdapter authService)
            : this(authService.Authorization.Credential)
        {
        }

        public ComputeEngineAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationAdapter>())
        {
        }

        public async Task<IEnumerable<Instance>> ListInstancesAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    var instancesByZone = await PageHelper.JoinPagesAsync<
                                InstancesResource.AggregatedListRequest,
                                InstanceAggregatedList,
                                InstancesScopedList>(
                        this.service.Instances.AggregatedList(projectId),
                        instances => instances.Items.Values.Where(v => v != null),
                        response => response.NextPageToken,
                        (request, token) => { request.PageToken = token; },
                        cancellationToken);

                    var result = instancesByZone
                        .Where(z => z.Instances != null)    // API returns null for empty zones.
                        .SelectMany(zone => zone.Instances);

                    TraceSources.IapDesktop.TraceVerbose("Found {0} instances", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 403)
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to VM instances in project {projectId} has been denied", e);
                }
            }
        }

        public async Task<IEnumerable<Disk>> ListDisksAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    var disksByZone = await PageHelper.JoinPagesAsync<
                                DisksResource.AggregatedListRequest,
                                DiskAggregatedList,
                                DisksScopedList>(
                        this.service.Disks.AggregatedList(projectId),
                        i => i.Items.Values.Where(v => v != null),
                        response => response.NextPageToken,
                        (request, token) => { request.PageToken = token; },
                        cancellationToken);

                    var result = disksByZone
                        .Where(z => z.Disks != null)    // API returns null for empty zones.
                        .SelectMany(zone => zone.Disks);

                    TraceSources.IapDesktop.TraceVerbose("Found {0} disks", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 403)
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to disks in project {projectId} has been denied", e);
                }
            }
        }

        public async Task<Image> GetImage(ImageLocator image, CancellationToken cancellationToken)
        {
            try
            {
                if (image.Name.StartsWith("family/"))
                {
                    return await this.service.Images
                        .GetFromFamily(image.ProjectId, image.Name.Substring(7))
                        .ExecuteAsync(cancellationToken);
                }
                else
                {
                    return await this.service.Images
                        .Get(image.ProjectId, image.Name)
                        .ExecuteAsync(cancellationToken);
                }
            }
            catch (Exception e) when (
                e.Unwrap() is GoogleApiException apiEx &&
                apiEx.Error != null &&
                apiEx.Error.Code == 404)
            {
                throw new ResourceNotFoundException($"Image {image} not found", e);
            }
            catch (Exception e) when (
                e.Unwrap() is GoogleApiException apiEx &&
                apiEx.Error != null &&
                (apiEx.Error.Code == 403))
            {
                throw new ResourceAccessDeniedException($"Access to {image} denied", e);
            }
        }

        public async Task<Instance> GetInstanceAsync(string projectId, string zone, string instanceName)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(projectId, zone, instanceName))
            {
                return await this.service.Instances.Get(projectId, zone, instanceName).ExecuteAsync();
            }
        }

        public async Task<Instance> GetInstanceAsync(InstanceLocator instanceRef)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(instanceRef))
            {
                return await GetInstanceAsync(instanceRef.ProjectId, instanceRef.Zone, instanceRef.Name);
            }
        }

        public SerialPortStream GetSerialPortOutput(InstanceLocator instanceRef)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(instanceRef))
            {
                return this.service.Instances.GetSerialPortOutputStream(instanceRef, 1);
            }
        }

        public Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token)
        {
            return ResetWindowsUserAsync(instanceRef, username, token, DefaultPasswordResetTimeout);
        }

        public async Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token,
            TimeSpan timeout)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(instanceRef, timeout))
            {
                return await this.service.Instances.ResetWindowsUserAsync(
                    instanceRef,
                    username,
                    token,
                    timeout);
            }
        }

        private static bool IsWindowsInstanceByGuestOsFeature(Instance instance)
        {
            // For an instance to be a valid Windows instance, at least one of the disks
            // (the boot disk) has to be marked as "WINDOWS". 
            // Note that older disks might lack this feature.
            return instance.Disks
                .EnsureNotNull()
                .Where(d => d.GuestOsFeatures != null)
                .SelectMany(d => d.GuestOsFeatures)
                .EnsureNotNull()
                .Any(f => f.Type == "WINDOWS");
        }

        private static bool IsWindowsInstanceByLicense(Instance instance)
        {
            // For an instance to be a valid Windows instance, at least one of the disks
            // has to have an associated Windows license. This is also true for
            // BYOL'ed instances.
            return instance.Disks
                .EnsureNotNull()
                .Where(d => d.Licenses != null)
                .SelectMany(d => d.Licenses)
                .EnsureNotNull()
                .Any(l => LicenseLocator.FromString(l).IsWindowsLicense());
        }

        public static bool IsWindowsInstance(Instance instance)
        {
            return IsWindowsInstanceByGuestOsFeature(instance) ||
                   IsWindowsInstanceByLicense(instance);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.service.Dispose();
            }
        }
    }
}
