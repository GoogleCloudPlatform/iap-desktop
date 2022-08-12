//
// Copyright 2021 Google LLC
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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    public sealed class ProjectPickerViewModel : ViewModelBase, IDisposable
    {
        private const int MaxResults = 100;

        private readonly IResourceManagerAdapter resourceManager;

        private IEnumerable<Project> selectedProjects;
        private string filter;
        private string statusText;
        private bool isLoading;
        private Exception filteringException;

        public ProjectPickerViewModel(
            IResourceManagerAdapter resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<Project> FilteredProjects { get; }
            = new RangeObservableCollection<Project>();

        public bool IsLoading
        {
            get => this.isLoading;
            private set
            {
                this.isLoading = value;
                RaisePropertyChange();
            }
        }

        public bool IsProjectSelected
            => this.selectedProjects != null && this.selectedProjects.Any();

        public IEnumerable<Project> SelectedProjects
        {
            get => this.selectedProjects;
            set
            {
                this.selectedProjects = value;
                RaisePropertyChange();
                RaisePropertyChange((ProjectPickerViewModel m) => m.IsProjectSelected);
            }
        }

        public Exception LoadingError
        {
            get => this.filteringException;
            private set
            {
                this.filteringException = value;
                RaisePropertyChange();
            }
        }

        public string StatusText
        {
            get => this.statusText;
            private set
            {
                this.statusText = value;
                RaisePropertyChange();
                RaisePropertyChange((ProjectPickerViewModel m) => m.IsStatusTextVisible);
            }
        }

        public bool IsStatusTextVisible
        {
            get => this.statusText != null;
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string Filter
        {
            get => this.filter;
            set
            {
                FilterAsync(value).ContinueWith(t => { });
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public async Task FilterAsync(string filter)
        {
            //
            // Update property synchrounously.
            //
            this.filter = filter;
            RaisePropertyChange((ProjectPickerViewModel m) => m.Filter);

            this.IsLoading = true;
            this.SelectedProjects = null;
            this.FilteredProjects.Clear();

            //
            // Start server-side search asynchronously, then 
            // update remaining properties on original (UI) thread.
            //
            try
            {
                var result = await this.resourceManager.ListProjectsAsync(
                        string.IsNullOrEmpty(this.filter)
                            ? null // All projects.
                            : ProjectFilter.ByPrefix(this.filter),
                        MaxResults,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                // Clear again because multiple filter operations might be running
                // in parallel.
                this.FilteredProjects.Clear();
                this.FilteredProjects.AddRange(result.Projects);
                if (result.IsTruncated)
                {
                    this.StatusText =
                        $"Over {result.Projects.Count()} projects found, " +
                            "use search to refine selection";
                }
                else
                {
                    this.StatusText =
                        $"{result.Projects.Count()} projects found";
                }
            }
            catch (Exception e)
            {
                this.LoadingError = e;
            }

            this.IsLoading = false;

            RaisePropertyChange((ProjectPickerViewModel m) => m.FilteredProjects);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.resourceManager.Dispose();
        }
    }
}
