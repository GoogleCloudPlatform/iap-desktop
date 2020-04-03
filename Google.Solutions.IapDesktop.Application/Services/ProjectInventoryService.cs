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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services
{
    /// <summary>
    /// This service manages the project inventory.
    /// </summary>
    public class ProjectInventoryService
    {
        private readonly InventorySettingsRepository inventorySettings;
        private readonly IEventService eventService;

        public ProjectInventoryService(
            InventorySettingsRepository inventorySettings,
            IEventService eventService)
        {
            this.inventorySettings = inventorySettings;
            this.eventService = eventService;
        }

        public ProjectInventoryService(IServiceProvider provider)
            : this(
                provider.GetService<InventorySettingsRepository>(),
                provider.GetService<IEventService>())
        { }

        public async Task AddProjectAsync(string projectId)
        {
            this.inventorySettings.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = projectId
            });

            await this.eventService.FireAsync(new ProjectAddedEvent(projectId));
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            this.inventorySettings.DeleteProjectSettings(projectId);

            await this.eventService.FireAsync(new ProjectDeletedEvent(projectId));
        }

        public Task<IEnumerable<Project>> ListProjectsAsync()
        {
            return Task.FromResult(this.inventorySettings
                .ListProjectSettings()
                .Select(s => new Project()
                {
                    Name = s.ProjectId
                }));
        }

        public class ProjectAddedEvent
        {
            public string ProjectId { get; }

            public ProjectAddedEvent(string projectId)
            {
                this.ProjectId = projectId;
            }
        }

        public class ProjectDeletedEvent
        {
            public string ProjectId { get; }

            public ProjectDeletedEvent(string projectId)
            {
                this.ProjectId = projectId;
            }
        }
    }
}
