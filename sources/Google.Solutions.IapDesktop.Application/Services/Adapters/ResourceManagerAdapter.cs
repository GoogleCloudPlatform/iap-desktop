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
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Requests;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IResourceManagerAdapter : IDisposable
    {
        Task<IEnumerable<Project>> QueryProjects(
            string filter,
            CancellationToken cancellationToken);

        Task<IEnumerable<Project>> QueryProjectsByPrefix(
            string idOrNamePrefix,
            CancellationToken cancellationToken);

        Task<IEnumerable<Project>> QueryProjectsById(
            string projectId,
            CancellationToken cancellationToken);

        Task<bool> IsGrantedPermission(
            string projectId,
            string permission,
            CancellationToken cancellationToken);
    }

    public class ResourceManagerAdapter : IResourceManagerAdapter
    {
        private const string MtlsBaseUri = "https://cloudresourcemanager.mtls.googleapis.com/";

        private readonly CloudResourceManagerService service;

        public bool IsDeviceCertiticateAuthenticationEnabled
            => this.service.IsMtlsEnabled() && this.service.IsClientCertificateProvided();

        public ResourceManagerAdapter(
            ICredential credential,
            IDeviceEnrollment deviceEnrollment)
        {
            this.service = new CloudResourceManagerService(
                ClientServiceFactory.ForMtlsEndpoint(
                    credential,
                    deviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (deviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == IsDeviceCertiticateAuthenticationEnabled);
        }

        public ResourceManagerAdapter(ICredential credential)
            : this(credential, null)
        {
            // This constructor should only be used for test cases
            Debug.Assert(Globals.IsTestCase);
        }

        public ResourceManagerAdapter(IAuthorizationAdapter authService)
            : this(
                  authService.Authorization.Credential,
                  authService.DeviceEnrollment)
        {
        }

        public ResourceManagerAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationAdapter>())
        {
        }

        public async Task<IEnumerable<Project>> QueryProjects(
            string filter,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(filter))
            {
                var request = new ProjectsResource.ListRequest(this.service)
                {
                    Filter = filter
                };

                var projects = await new PageStreamer<
                    Project,
                    ProjectsResource.ListRequest,
                    ListProjectsResponse,
                    string>(
                        (req, token) => req.PageToken = token,
                        response => response.NextPageToken,
                        response => response.Projects)
                    .FetchAllAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                // Filter projects in deleted/pending delete state.
                var result = projects.Where(p => p.LifecycleState == "ACTIVE");

                ApplicationTraceSources.Default.TraceVerbose("Found {0} projects", result.Count());

                return result;
            }
        }

        public Task<IEnumerable<Project>> QueryProjectsByPrefix(
            string idOrNamePrefix,
            CancellationToken cancellationToken)
        {
            return QueryProjects(
                $"name:\"{idOrNamePrefix}*\" OR id:\"{idOrNamePrefix}*\"",
                cancellationToken);
        }

        public Task<IEnumerable<Project>> QueryProjectsById(
            string projectId,
            CancellationToken cancellationToken)
        {
            return QueryProjects($"id:\"{projectId}\"", cancellationToken);
        }

        public async Task<bool> IsGrantedPermission(
            string projectId,
            string permission,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(permission))
            {
                var response = await this.service.Projects.TestIamPermissions(
                        new TestIamPermissionsRequest()
                        {
                            Permissions = new[] { permission }
                        },
                        projectId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
                return response != null &&
                    response.Permissions != null &&
                    response.Permissions.Any(p => p == permission);
            }
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
