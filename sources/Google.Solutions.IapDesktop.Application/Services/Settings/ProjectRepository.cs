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

using Google.Solutions.IapDesktop.Application.Services.Integration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Settings
{
    public class ProjectRepository : IProjectRepository
    {
        protected readonly RegistryKey baseKey;
        private readonly IEventService eventService;

        public ProjectRepository(
            RegistryKey baseKey,
            IEventService eventService)
        {
            this.baseKey = baseKey;
            this.eventService = eventService;
        }

        public async Task AddProjectAsync(string projectId)
        {
            using (this.baseKey.CreateSubKey(projectId))
            { }

            await this.eventService
                .FireAsync(new ProjectAddedEvent(projectId))
                .ConfigureAwait(false);
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            this.baseKey.DeleteSubKeyTree(projectId, false);

            await this.eventService
                .FireAsync(new ProjectDeletedEvent(projectId))
                .ConfigureAwait(false);
        }

        public Task<IEnumerable<Project>> ListProjectsAsync()
        {
            var projects = this.baseKey.GetSubKeyNames()
                .Select(projectId => new Project(projectId));
            return Task.FromResult(projects);
        }

        public RegistryKey OpenRegistryKey(string projectId)
        {
            return this.baseKey.OpenSubKey(projectId, true);
        }

        public RegistryKey OpenRegistryKey(string projectId, string subkey, bool create)
        {
            // Make sure the parent key actually exists.
            using (var parentKey = OpenRegistryKey(projectId))
            {
                if (parentKey == null)
                {
                    throw new KeyNotFoundException(projectId);
                }

                return create
                ? parentKey.CreateSubKey(subkey, true)
                : parentKey.OpenSubKey(subkey, true);
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
                this.baseKey.Dispose();
            }
        }
    }
}
