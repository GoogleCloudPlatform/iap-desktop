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
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Services;
using Google.Solutions.Compute.Auth;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Google.Solutions.IapDesktop.Application.Services;

namespace Google.Solutions.IapDesktop.Application.Adapters
{
    public class ResourceManagerAdapter
    {
        private readonly CloudResourceManagerService service;

        public ResourceManagerAdapter(IAuthorizationService authService)
        {
            var assemblyName = typeof(ResourceManagerAdapter).Assembly.GetName();

            this.service = new CloudResourceManagerService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = authService.Authorization.Credential,
                    ApplicationName = $"{assemblyName.Name}/{assemblyName.Version}"
                });
        }

        public ResourceManagerAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationService>())
        {
        }

        public async Task<IEnumerable<Project>> QueryProjects(string filter)
        {
            var projects = await PageHelper.JoinPagesAsync<ProjectsResource.ListRequest, ListProjectsResponse, Project>(
                new ProjectsResource.ListRequest(this.service)
                {
                    Filter = filter
                },
                page => page.Projects,
                response => response.NextPageToken,
                (request, token) => { request.PageToken = token; });

            // Filter projects in deleted/pending delete state.
            return projects.Where(p => p.LifecycleState == "ACTIVE");
        }

        public Task<IEnumerable<Project>> QueryProjectsByPrefix(string idOrNamePrefix)
        {
            return QueryProjects($"name:\"{idOrNamePrefix}*\" OR id:\"{idOrNamePrefix}*\"");
        }

        public Task<IEnumerable<Project>> QueryProjectsById(string projectId)
        {
            return QueryProjects($"id:\"{projectId}\"");
        }
    }
}
