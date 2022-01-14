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
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Net;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Views;
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
        Task<Project> GetProjectAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<FilteredProjectList> ListProjectsAsync(
            ProjectFilter filter,
            int? maxResults,
            CancellationToken cancellationToken);

        Task<bool> IsGrantedPermissionAsync(
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

        public ResourceManagerAdapter(IAuthorizationSource authService)
            : this(
                  authService.Authorization.Credential,
                  authService.Authorization.DeviceEnrollment)
        {
        }

        public ResourceManagerAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationSource>())
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

        public async Task<FilteredProjectList> ListProjectsAsync(
            ProjectFilter filter,
            int? maxResults,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(filter))
            {
                var request = new ProjectsResource.ListRequest(this.service)
                {
                    Filter = filter?.ToString(),
                    PageSize = maxResults
                };

                IList<Project> projects;
                bool truncated;
                if (maxResults.HasValue)
                {
                    // Read single page.
                    var response = await request
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);

                    truncated = response.NextPageToken != null;
                    projects = response.Projects;
                }
                else
                {
                    // Read all pages.
                    truncated = false;
                    projects = await new PageStreamer<
                        Project,
                        ProjectsResource.ListRequest,
                        ListProjectsResponse,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Projects)
                        .FetchAllAsync(request, cancellationToken)
                        .ConfigureAwait(false);
                }

                // Filter projects in deleted/pending delete state.
                var activeProjects = projects
                    .EnsureNotNull()
                    .Where(p => p.LifecycleState == "ACTIVE");

                ApplicationTraceSources.Default.TraceVerbose(
                    "Found {0} projects", activeProjects.Count());

                return new FilteredProjectList(activeProjects, truncated);
            }
        }

        public async Task<bool> IsGrantedPermissionAsync(
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

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

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

    public class FilteredProjectList
    {
        public IEnumerable<Project> Projects { get; }
        public bool IsTruncated { get; }

        public FilteredProjectList(
            IEnumerable<Project> projects,
            bool isTruncated)
        {
            this.Projects = projects;
            this.IsTruncated = isTruncated;
        }
    }

    public class ProjectFilter
    {
        private readonly string filter;

        private ProjectFilter(string filter)
        {
            this.filter = filter;
        }

        private static string Sanitize(string filter)
        {
            return filter
                .Replace(":", "")
                .Replace("\"", "")
                .Replace("'", "");
        }

        public static ProjectFilter ByProjectId(string projectId)
        {
            projectId = Sanitize(projectId);
            return new ProjectFilter($"id:\"{projectId}\"");
        }

        public static ProjectFilter ByPrefix(string idOrNamePrefix)
        {
            idOrNamePrefix = Sanitize(idOrNamePrefix);
            return new ProjectFilter($"name:\"{idOrNamePrefix}*\" OR id:\"{idOrNamePrefix}*\"");
        }

        public override string ToString()
        {
            return this.filter;
        }
    }
}
