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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    internal class ResourceManagerAdapter
    {
        private readonly CloudResourceManagerService service;

        public static ResourceManagerAdapter Create(ICredential credential)
        {
            var assemblyName = typeof(ResourceManagerAdapter).Assembly.GetName();
            return new ResourceManagerAdapter(
                new CloudResourceManagerService(
                    new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = $"{assemblyName.Name}/{assemblyName.Version}"
                    }));
        }

        private ResourceManagerAdapter(CloudResourceManagerService service)
        {
            this.service = service;
        }

        public async Task<IEnumerable<Project>> QueryProjects(string filter)
        {
            return await PageHelper.JoinPagesAsync<ProjectsResource.ListRequest, ListProjectsResponse, Project>(
                new ProjectsResource.ListRequest(this.service)
                {
                    Filter = filter
                },
                page => page.Projects,
                response => response.NextPageToken,
                (request, token) => { request.PageToken = token; });
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
