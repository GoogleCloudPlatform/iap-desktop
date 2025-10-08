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
using Google.Solutions.IapDesktop.Core.ResourceModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    public class ProjectRepository :
        IProjectSettingsRepository,
        IProjectWorkspaceSettings
    {
        /// <summary>
        /// Multi-SZ value to store ancestry information.
        /// </summary>
        internal const string AncestryValueName = "Ancestry";

        protected RegistryKey BaseKey { get; }

        public ProjectRepository(RegistryKey baseKey)
        {
            this.BaseKey = baseKey;
        }

        //---------------------------------------------------------------------
        // IProjectRepository.
        //---------------------------------------------------------------------

        public void AddProject(ProjectLocator project)
        {
            using (this.BaseKey.CreateSubKey(project.Name))
            { }

            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(this.Projects)));
        }

        public void RemoveProject(ProjectLocator project)
        {
            this.BaseKey.DeleteSubKeyTree(project.Name, false);
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(this.Projects)));
        }

        public Task<IEnumerable<ProjectLocator>> ListProjectsAsync()
        {
            return Task.FromResult(this.Projects);
        }

        //---------------------------------------------------------------------
        // IProjectWorkspaceSettings.
        //---------------------------------------------------------------------

        public event PropertyChangedEventHandler? PropertyChanged;

        public IEnumerable<ProjectLocator> Projects
        {
            get => this.BaseKey
                .GetSubKeyNames()
                .Select(projectId => new ProjectLocator(projectId));
        }

        //---------------------------------------------------------------------
        // IProjectSettingsRepository.
        //---------------------------------------------------------------------

        public RegistryKey OpenRegistryKey(string projectId)
        {
            var key = this.BaseKey.OpenSubKey(projectId, true);
            return key ?? throw new KeyNotFoundException(projectId);
        }

        public RegistryKey OpenRegistryKey(string projectId, string subkey)
        {
            using (var parentKey = OpenRegistryKey(projectId))
            {
                return parentKey.CreateSubKey(subkey, true);
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
                this.BaseKey.Dispose();
            }
        }
    }
}
