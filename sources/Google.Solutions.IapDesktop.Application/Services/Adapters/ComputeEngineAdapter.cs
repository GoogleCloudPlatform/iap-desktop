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
using Google.Apis.Requests;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IComputeEngineAdapter : IDisposable
    {
        Task<Project> GetProjectAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<Instance> GetInstanceAsync(
            InstanceLocator instanceLocator,
            CancellationToken cancellationToken);

        Task<GuestAttributes> GetGuestAttributesAsync(
            InstanceLocator instanceLocator,
            string queryPath,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            ZoneLocator zoneLocator,
            CancellationToken cancellationToken);

        Task<IEnumerable<Disk>> ListDisksAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroup>> ListNodeGroupsAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ZoneLocator zone,
            string nodeGroup,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token);

        Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            TimeSpan timeout,
            CancellationToken token);

        Task<Image> GetImageAsync(
            ImageLocator image,
            CancellationToken cancellationToken);

        IAsyncReader<string> GetSerialPortOutput(
            InstanceLocator instanceRef,
            ushort portNumber);

        Task<bool> IsGrantedPermission(
            InstanceLocator instanceRef,
            string permission);

        Task<bool> IsGrantedPermissionToResetWindowsUser(
            InstanceLocator instanceRef);
    }

    /// <summary>
    /// Adapter class for the Compute Engine API.
    /// </summary>
    public class ComputeEngineAdapter : IComputeEngineAdapter
    {
        private const string MtlsBaseUri = "https://compute.mtls.googleapis.com/compute/v1/projects/";
        private static readonly TimeSpan DefaultPasswordResetTimeout = TimeSpan.FromSeconds(25);

        private readonly ComputeService service;

        public bool IsDeviceCertiticateAuthenticationEnabled
            => this.service.IsMtlsEnabled() && this.service.IsClientCertificateProvided();

        public ComputeEngineAdapter(
            ICredential credential,
            IDeviceEnrollment deviceEnrollment)
        {
            this.service = new ComputeService(
                ClientServiceFactory.ForMtlsEndpoint(
                    credential,
                    deviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (deviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == IsDeviceCertiticateAuthenticationEnabled);
        }

        public ComputeEngineAdapter(ICredential credential)
            : this(credential, null)
        {
            // This constructor should only be used for test cases
            Debug.Assert(Globals.IsTestCase);
        }

        public ComputeEngineAdapter(IAuthorizationAdapter authService)
            : this(
                  authService.Authorization.Credential,
                  authService.DeviceEnrollment)
        {
        }

        public ComputeEngineAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationAdapter>())
        {
        }

        public async Task<Project> GetProjectAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    return await this.service.Projects.Get(
                        projectId).ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"You do not have sufficient permissions to access project {projectId}. " +
                        "You need the 'Compute Viewer' role (or an equivalent custom role) " +
                        "to perform this action.",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
            }
        }

        public async Task<IEnumerable<Instance>> ListInstancesAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    var instancesByZone = await new PageStreamer<
                        InstancesScopedList,
                        InstancesResource.AggregatedListRequest,
                        InstanceAggregatedList,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items.Values.Where(v => v != null))
                        .FetchAllAsync(
                            this.service.Instances.AggregatedList(projectId),
                            cancellationToken)
                        .ConfigureAwait(false);

                    var result = instancesByZone
                        .Where(z => z.Instances != null)    // API returns null for empty zones.
                        .SelectMany(zone => zone.Instances);

                    ApplicationTraceSources.Default.TraceVerbose("Found {0} instances", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to list VM instances in " +
                        $"project {projectId}. " +
                        "You need the 'Compute Viewer' role (or an equivalent custom role) " +
                        "to perform this action.",
                        HelpTopics.ProjectAccessControl, 
                        e);
                }
            }
        }

        public async Task<IEnumerable<Instance>> ListInstancesAsync(
            ZoneLocator zoneLocator,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(zoneLocator))
            {
                try
                {
                    var result = await new PageStreamer<
                        Instance,
                        InstancesResource.ListRequest,
                        InstanceList,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items)
                        .FetchAllAsync(
                            this.service.Instances.List(zoneLocator.ProjectId, zoneLocator.Name),
                            cancellationToken)
                        .ConfigureAwait(false);

                    ApplicationTraceSources.Default.TraceVerbose("Found {0} instances", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to list VM instances in " +
                        $"project {zoneLocator.ProjectId}. " +
                        "You need the 'Compute Viewer' role (or an equivalent custom role) " +
                        "to perform this action.",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
            }
        }

        public async Task<Instance> GetInstanceAsync(
            InstanceLocator instanceLocator,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instanceLocator))
            {
                try
                {
                    return await this.service.Instances.Get(
                        instanceLocator.ProjectId,
                        instanceLocator.Zone,
                        instanceLocator.Name).ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to VM instance {instanceLocator.Name} has been denied",
                        HelpTopics.ProjectAccessControl, 
                        e);
                }
            }
        }

        public async Task<GuestAttributes> GetGuestAttributesAsync(
            InstanceLocator instanceLocator,
            string queryPath,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instanceLocator))
            {
                try
                {
                    var request = this.service.Instances.GetGuestAttributes(
                        instanceLocator.ProjectId,
                        instanceLocator.Zone,
                        instanceLocator.Name);
                    request.QueryPath = queryPath;
                    return await request
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    // No guest attributes present.
                    return null;
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to VM instance {instanceLocator.Name} has been denied",
                        HelpTopics.ProjectAccessControl, 
                        e);
                }
            }
        }

        public async Task<IEnumerable<Disk>> ListDisksAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    var disksByZone = await new PageStreamer<
                        DisksScopedList,
                        DisksResource.AggregatedListRequest,
                        DiskAggregatedList,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items.Values.Where(v => v != null))
                        .FetchAllAsync(
                            this.service.Disks.AggregatedList(projectId),
                            cancellationToken)
                        .ConfigureAwait(false);

                    var result = disksByZone
                        .Where(z => z.Disks != null)    // API returns null for empty zones.
                        .SelectMany(zone => zone.Disks);

                    ApplicationTraceSources.Default.TraceVerbose("Found {0} disks", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to disks in project {projectId} has been denied",
                        HelpTopics.ProjectAccessControl, 
                        e);
                }
            }
        }

        public async Task<IEnumerable<NodeGroup>> ListNodeGroupsAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    var groupsByZone = await new PageStreamer<
                        NodeGroupsScopedList,
                        NodeGroupsResource.AggregatedListRequest,
                        NodeGroupAggregatedList,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items.Values.Where(v => v != null))
                        .FetchAllAsync(
                            this.service.NodeGroups.AggregatedList(projectId),
                            cancellationToken)
                        .ConfigureAwait(false);

                    var result = groupsByZone
                        .Where(z => z.NodeGroups != null)    // API returns null for empty zones.
                        .SelectMany(zone => zone.NodeGroups);

                    ApplicationTraceSources.Default.TraceVerbose("Found {0} node groups", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to node groups in project {projectId} has been denied",
                        HelpTopics.ProjectAccessControl, 
                        e);
                }
            }
        }

        public async Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ZoneLocator zone,
            string nodeGroup,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(zone, nodeGroup))
            {
                try
                {
                    return await new PageStreamer<
                        NodeGroupNode,
                        NodeGroupsResource.ListNodesRequest,
                        NodeGroupsListNodes,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items)
                        .FetchAllAsync(
                            this.service.NodeGroups.ListNodes(zone.ProjectId, zone.Name, nodeGroup),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to nodes in project {zone.ProjectId} has been denied",
                        HelpTopics.ProjectAccessControl, 
                        e);
                }
            }
        }

        public async Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                var nodeGroups = await ListNodeGroupsAsync(
                        projectId,
                        cancellationToken)
                    .ConfigureAwait(false);

                var nodesAcrossGroups = Enumerable.Empty<NodeGroupNode>();

                foreach (var nodeGroup in nodeGroups)
                {
                    nodesAcrossGroups = nodesAcrossGroups.Concat(await ListNodesAsync(
                            ZoneLocator.FromString(nodeGroup.Zone),
                            nodeGroup.Name,
                            cancellationToken)
                        .ConfigureAwait(false));
                }

                return nodesAcrossGroups;
            }
        }

        public async Task<Image> GetImageAsync(ImageLocator image, CancellationToken cancellationToken)
        {
            try
            {
                if (image.Name.StartsWith("family/"))
                {
                    return await this.service.Images
                        .GetFromFamily(image.ProjectId, image.Name.Substring(7))
                        .ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await this.service.Images
                        .Get(image.ProjectId, image.Name)
                        .ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (
                e.Unwrap() is GoogleApiException apiEx && apiEx.IsNotFound())
            {
                throw new ResourceNotFoundException($"Image {image} not found", e);
            }
            catch (Exception e) when (
                e.Unwrap() is GoogleApiException apiEx && apiEx.IsAccessDenied())
            {
                throw new ResourceAccessDeniedException($"Access to {image} denied", e);
            }
        }

        public async Task<Instance> GetInstanceAsync(string projectId, string zone, string instanceName)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId, zone, instanceName))
            {
                return await this.service.Instances.Get(projectId, zone, instanceName)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
        }

        public Task<Instance> GetInstanceAsync(InstanceLocator instanceRef)
            => GetInstanceAsync(instanceRef.ProjectId, instanceRef.Zone, instanceRef.Name);

        public IAsyncReader<string> GetSerialPortOutput(InstanceLocator instanceRef, ushort portNumber)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instanceRef))
            {
                return this.service.Instances.GetSerialPortOutputStream(instanceRef, portNumber);
            }
        }

        public async Task<bool> IsGrantedPermission(
            InstanceLocator instanceLocator,
            string permission)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(permission))
            {
                var response = await this.service.Instances.TestIamPermissions(
                        new TestPermissionsRequest
                        {
                            Permissions = new[] { permission }
                        },
                        instanceLocator.ProjectId,
                        instanceLocator.Zone,
                        instanceLocator.Name)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                return response != null &&
                    response.Permissions != null &&
                    response.Permissions.Any(p => p == permission);
            }
        }

        public Task<bool> IsGrantedPermissionToResetWindowsUser(InstanceLocator instanceRef)
        {
            //
            // Resetting a user requires
            //  (1) compute.instances.setMetadata
            //  (2) iam.serviceAccounts.actAs (if the instance runs as service account)
            //
            // For performance reasons, only check (1).
            //
            return IsGrantedPermission(instanceRef, Permissions.ComputeInstancesSetMetadata);
        }

        public Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token)
        {
            return ResetWindowsUserAsync(instanceRef, username, DefaultPasswordResetTimeout, token);
        }

        public async Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            TimeSpan timeout,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instanceRef, timeout))
            {
                return await this.service.Instances.ResetWindowsUserAsync(
                    instanceRef,
                    username,
                    timeout,
                    token).ConfigureAwait(false);
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
