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

using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Requests;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Crm
{
    /// <summary>
    /// Client for Resource Manager (CRM) API.
    /// </summary>
    public interface IResourceManagerClient : IClient
    {
        Task<Project> GetProjectAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<FilteredProjectList> ListProjectsAsync(
            ProjectFilter? filter,
            int? maxResults,
            CancellationToken cancellationToken);

        /// <summary>
        /// Test if all permissions have been granted.
        /// </summary>
        Task<bool> IsAccessGrantedAsync(
            string projectId,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken);
    }

    public class ResourceManagerClient : ApiClientBase, IResourceManagerClient
    {
        private readonly CloudResourceManagerService service;

        public ResourceManagerClient(
            ServiceEndpoint<ResourceManagerClient> endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
            : base(endpoint, authorization, userAgent)
        {
            this.service = new CloudResourceManagerService(this.Initializer);
        }

        public static ServiceEndpoint<ResourceManagerClient> CreateEndpoint(
            ServiceRoute? route = null)
        {
            return new ServiceEndpoint<ResourceManagerClient>(
                route ?? ServiceRoute.Public,
                "https://cloudresourcemanager.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IResourceManagerClient.
        //---------------------------------------------------------------------

        public async Task<Project> GetProjectAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    return await this.service.Projects
                        .Get(projectId)
                        .ExecuteAsync(cancellationToken)
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
            using (ApiTraceSource.Log.TraceMethod().WithParameters(filter))
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

                ApiTraceSource.Log.TraceVerbose(
                    "Found {0} projects", activeProjects.Count());

                return new FilteredProjectList(activeProjects, truncated);
            }
        }

        public async Task<bool> IsAccessGrantedAsync(
            string projectId,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod()
                .WithParameters(string.Join(",", permissions)))
            {
                var response = await this.service.Projects
                    .TestIamPermissions(
                        new TestIamPermissionsRequest()
                        {
                            Permissions = permissions.ToList()
                        },
                        projectId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
                return response != null &&
                    response.Permissions != null &&
                    permissions.All(p => response.Permissions.Contains(p));
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

        /// <summary>
        /// Create filter for a specific project ID.
        /// </summary>
        public static ProjectFilter ByProjectId(string projectId)
        {
            projectId = Sanitize(projectId);
            return new ProjectFilter($"id:\"{projectId}\"");
        }

        /// <summary>
        /// Create filter for projects whose name or ID matches a term.
        /// </summary>
        public static ProjectFilter ByTerm(string term)
        {
            term = Sanitize(term);
            return new ProjectFilter($"name:\"*{term}*\" OR id:\"*{term}*\"");
        }

        public override string ToString()
        {
            return this.filter;
        }
    }
}
