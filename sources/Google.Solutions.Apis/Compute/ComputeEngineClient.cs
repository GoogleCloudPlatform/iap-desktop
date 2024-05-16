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

using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Compute
{
    public sealed class ComputeEngineClient : ApiClientBase, IComputeEngineClient, IDisposable
    {
        private readonly ComputeService service;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ComputeEngineClient(
            ServiceEndpoint<ComputeEngineClient> endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
            : base(endpoint, authorization, userAgent)
        {
            this.service = new ComputeService(this.Initializer);
        }

        public static ServiceEndpoint<ComputeEngineClient> CreateEndpoint(
            ServiceRoute? route = null)
        {
            return new ServiceEndpoint<ComputeEngineClient>(
                route ?? ServiceRoute.Public,
                "https://compute.googleapis.com/compute/v1/");
        }

        //---------------------------------------------------------------------
        // IComputeEngineClient.
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public async Task<Project> GetProjectAsync(
            ProjectLocator project,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(project))
            {
                try
                {
                    return await this.service.Projects
                        .Get(project.Name).ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDeniedByVpcServiceControlPolicy())
                {
                    throw new ResourceAccessDeniedByVpcScPolicyException(e);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        $"You do not have sufficient permissions to access project {project.Name}. " +
                        "You need the 'Compute Viewer' role (or an equivalent custom role) " +
                        "to perform this action.",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The project {project.Name} does not exist",
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        public async Task<IEnumerable<Instance>> ListInstancesAsync(
            ProjectLocator project,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(project))
            {
                try
                {
                    var request = this.service.Instances.AggregatedList(project.Name);

                    //
                    // Ignore zones or regions that are currently unavailable,
                    // cf. go/drd-cp.
                    //
                    request.ReturnPartialSuccess = true;

                    var instancesByZone = await new PageStreamer<
                        InstancesScopedList,
                        InstancesResource.AggregatedListRequest,
                        InstanceAggregatedList,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items.Values.Where(v => v != null))
                        .FetchAllAsync(
                            request,
                            cancellationToken)
                        .ConfigureAwait(false);

                    var result = instancesByZone
                        .Where(z => z.Instances != null)    // API returns null for empty zones.
                        .SelectMany(zone => zone.Instances);

                    ApiTraceSource.Log.TraceVerbose("Found {0} instances", result.Count());

                    return result;
                }
                catch (GoogleApiException e) when (e.IsAccessDeniedByVpcServiceControlPolicy())
                {
                    throw new ResourceAccessDeniedByVpcScPolicyException(e);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to list VM instances in " +
                        $"project {project.Name}. " +
                        "You need the 'Compute Viewer' role (or an equivalent custom role) " +
                        "to perform this action.",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The project {project.Name} does not exist",
                        e);
                }
            }
        }

        public async Task<IEnumerable<Instance>> ListInstancesAsync(
            ZoneLocator zoneLocator,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(zoneLocator))
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

                    ApiTraceSource.Log.TraceVerbose("Found {0} instances", result.Count);

                    return result;
                }
                catch (GoogleApiException e) when (e.IsAccessDeniedByVpcServiceControlPolicy())
                {
                    throw new ResourceAccessDeniedByVpcScPolicyException(e);
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
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The zone {zoneLocator} does not exist",
                        e);
                }
            }
        }

        public async Task<Instance> GetInstanceAsync(
            InstanceLocator instance,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(instance))
            {
                try
                {
                    return await this.service.Instances.Get(
                        instance.ProjectId,
                        instance.Zone,
                        instance.Name).ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDeniedByVpcServiceControlPolicy())
                {
                    throw new ResourceAccessDeniedByVpcScPolicyException(e);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to access " +
                            $"VM instance {instance.Name} in project {instance.ProjectId}",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The VM instance {instance.Name} does not exist " +
                            $"in project {instance.ProjectId}",
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Guest attributes.
        //---------------------------------------------------------------------

        public async Task<GuestAttributes?> GetGuestAttributesAsync(
            InstanceLocator instance,
            string queryPath,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(instance))
            {
                try
                {
                    var request = this.service.Instances.GetGuestAttributes(
                        instance.ProjectId,
                        instance.Zone,
                        instance.Name);
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
                        "You do not have sufficient permissions to access the guest attributes " +
                            $"of VM instance {instance.Name} in project {instance.ProjectId}",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Serial port.
        //---------------------------------------------------------------------

        public IAsyncReader<string> GetSerialPortOutput(
            InstanceLocator instanceRef,
            ushort portNumber)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(instanceRef))
            {
                return new SerialPortReader(
                    this.service.Instances,
                    instanceRef,
                    portNumber);
            }
        }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        public async Task UpdateMetadataAsync(
            InstanceLocator instance,
            Action<Metadata> updateMetadata,
            CancellationToken token)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(instance))
            {
                try
                {
                    await this.service.Instances.UpdateMetadataAsync(
                            instance,
                            updateMetadata,
                            token)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied() || e.Error == null)
                {
                    //
                    // NB. Sometimes the error info is missing in 403 errors.
                    //
                    throw new ResourceAccessDeniedException(
                        $"You don't have sufficient permissions to modify " +
                            $"the metadata of VM instance {instance.Name} in project " +
                            $"{instance.ProjectId}",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The VM instance {instance.Name} does not exist " +
                            $"in project {instance.ProjectId}",
                        e);
                }
            }
        }

        public async Task UpdateCommonInstanceMetadataAsync(
            string projectId,
            Action<Metadata> updateMetadata,
            CancellationToken token)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    await this.service.Projects.UpdateMetadataAsync(
                            projectId,
                            updateMetadata,
                            token)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied() || e.Error == null)
                {
                    //
                    // NB. Sometimes the error info is missing in 403 errors.
                    //
                    throw new ResourceAccessDeniedException(
                        "You don't have sufficient permissions to modify " +
                            $"the metadata of project {projectId}",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The project {projectId} does not exist",
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Permission check.
        //---------------------------------------------------------------------

        public async Task<bool> IsAccessGrantedAsync(
            InstanceLocator instanceLocator,
            string permission)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(permission))
            {
                try
                {
                    var response = await this.service.Instances
                        .TestIamPermissions(
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
                catch (Exception e) when (e.IsAccessDeniedError())
                {
                    //
                    // NB. testPermission requires the 'compute.instances.list'
                    // permission. Fail open if the caller does not have that
                    // permission.
                    //
                    ApiTraceSource.Log.TraceWarning(
                        "Permission check failed because caller does not have " +
                        "the permission to test permissions");
                    return true;
                }
            }
        }

        //---------------------------------------------------------------------
        // Control instance lifecycle.
        //---------------------------------------------------------------------

        public async Task ControlInstanceAsync(
           InstanceLocator instance,
           InstanceControlCommand command,
           CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod()
                .WithParameters(instance, command))
            {
                try
                {
                    ClientServiceRequest<Operation> request = command switch
                    {
                        InstanceControlCommand.Start => this.service.Instances.Start(
                            instance.ProjectId,
                            instance.Zone,
                            instance.Name),
                        InstanceControlCommand.Stop => this.service.Instances.Stop(
                            instance.ProjectId,
                            instance.Zone,
                            instance.Name),
                        InstanceControlCommand.Suspend => this.service.Instances.Suspend(
                            instance.ProjectId,
                            instance.Zone,
                            instance.Name),
                        InstanceControlCommand.Resume => this.service.Instances.Resume(
                            instance.ProjectId,
                            instance.Zone,
                            instance.Name),
                        InstanceControlCommand.Reset => this.service.Instances.Reset(
                            instance.ProjectId,
                            instance.Zone,
                            instance.Name),
                        _ => throw new ArgumentException("The command is not supported"),
                    };
                    await request
                        .ExecuteAndAwaitOperationAsync(
                            instance.ProjectId,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        $"The VM instance {instance.Name} does not exist " +
                            $"in project {instance.ProjectId}",
                        e);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to control the " +
                            $"VM instance {instance.Name} in project {instance.ProjectId}",
                        HelpTopics.ProjectAccessControl,
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.service.Dispose();
        }
    }
}
