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
using Google.Apis.Services;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
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
    }

    public class ResourceManagerAdapter : IResourceManagerAdapter
    {
        private readonly CloudResourceManagerService service;

        public ResourceManagerAdapter(IAuthorizationAdapter authService)
        {
            this.service = new CloudResourceManagerService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = authService.Authorization.Credential,
                    ApplicationName = Globals.UserAgent.ToApplicationName()
                });
        }

        public ResourceManagerAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationAdapter>())
        {
        }

        public async Task<IEnumerable<Project>> QueryProjects(
            string filter,
            CancellationToken cancellationToken)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(filter))
            {
                var projects = await PageHelper.JoinPagesAsync<
                            ProjectsResource.ListRequest, 
                            ListProjectsResponse, 
                            Project>(
                    new ProjectsResource.ListRequest(this.service)
                    {
                        Filter = filter
                    },
                    page => page.Projects,
                    response => response.NextPageToken,
                    (request, token) => { request.PageToken = token; },
                    cancellationToken);

                // Filter projects in deleted/pending delete state.
                var result = projects.Where(p => p.LifecycleState == "ACTIVE");

                TraceSources.IapDesktop.TraceVerbose("Found {0} projects", result.Count());

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
