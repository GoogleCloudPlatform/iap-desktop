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

using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ProjectModel
{
    /// <summary>
    /// Repository that underlies the project model and 
    /// keeps track of a user's projects.
    /// </summary>
    public interface IProjectRepository : IDisposable
    {
        /// <summary>
        /// Add a project and save changes.
        /// </summary>
        void AddProject(ProjectLocator projectId);

        /// <summary>
        /// Remove a project and save changes.
        /// </summary>
        void RemoveProject(ProjectLocator projectId);

        /// <summary>
        /// List all projects.
        /// </summary>
        Task<IEnumerable<ProjectLocator>> ListProjectsAsync();
    }
}
