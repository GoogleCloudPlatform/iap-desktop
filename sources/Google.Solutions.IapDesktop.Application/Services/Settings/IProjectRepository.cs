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

using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for Project-related settings.
    /// </summary>
    public interface IProjectRepository
    {
        void AddProject(string projectId);
        void RemoveProject(string projectId);

        // TODO: change signature to ProjectLocator
        Task<IEnumerable<Project>> ListProjectsAsync();

        RegistryKey OpenRegistryKey(string projectId);

        RegistryKey OpenRegistryKey(string projectId, string subkey, bool create);
    }

    public class Project
    {
        public string ProjectId { get; }

        internal Project(string projectId)
        {
            this.ProjectId = projectId;
        }
    }

    //---------------------------------------------------------------------
    // Events.
    //---------------------------------------------------------------------

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
