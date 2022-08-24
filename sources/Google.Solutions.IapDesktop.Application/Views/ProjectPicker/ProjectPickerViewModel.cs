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

        private string filter;

        public ProjectPickerViewModel(
            IResourceManagerAdapter resourceManager)
        {
            this.resourceManager = resourceManager;

            this.FilteredProjects = new RangeObservableCollection<Project>();
            this.IsLoading = ObservableProperty.Build(false);
            this.LoadingError = ObservableProperty.Build<Exception>(null);
            this.StatusText = ObservableProperty.Build<string>(null);
            this.IsStatusTextVisible = ObservableProperty.Build(
                this.StatusText, 
                t => t != null);
            this.SelectedProjects = ObservableProperty.Build<IEnumerable<Project>>(null);
            this.IsProjectSelected = ObservableProperty.Build(
                this.SelectedProjects,
                p => p != null && p.Any());
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<Project> FilteredProjects { get; }

        public ObservableProperty<bool> IsLoading { get; }

        public ObservableFunc<bool> IsProjectSelected { get; }

        public ObservableProperty<IEnumerable<Project>> SelectedProjects { get; }

        public ObservableProperty<Exception> LoadingError { get; }

        public ObservableProperty<string> StatusText { get; }

        public ObservableFunc<bool> IsStatusTextVisible { get; }

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

            this.IsLoading.Value = true;
            this.SelectedProjects.Value = null;
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
                    this.StatusText.Value =
                        $"Over {result.Projects.Count()} projects found, " +
                            "use search to refine selection";
                }
                else
                {
                    this.StatusText.Value =
                        $"{result.Projects.Count()} projects found";
                }
            }
            catch (Exception e)
            {
                this.LoadingError.Value = e;
            }

            this.IsLoading.Value = false;

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
