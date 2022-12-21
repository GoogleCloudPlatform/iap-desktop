//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Cache;
using Google.Solutions.Mvvm.Commands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys
{
    public class AuthorizedPublicKeysViewModel
        : ModelCachingViewModelBase<IProjectModelNode, AuthorizedPublicKeysModel>
    {
        private const int ModelCacheCapacity = 5;
        private const string WindowTitlePrefix = "Authorized SSH keys";

        private readonly IConfirmationDialog confirmationDialog;
        private readonly IJobService jobService;
        private readonly Service<IOsLoginService> osLoginService;
        private readonly Service<IComputeEngineAdapter> computeEngineAdapter;
        private readonly Service<IResourceManagerAdapter> resourceManagerAdapter;

        private string filter;
        private bool isLoading;
        private bool isListEnabled = false;
        private string windowTitle;
        private string informationBarContent;
        private AuthorizedPublicKeysModel.Item selectedItem;

        public AuthorizedPublicKeysViewModel(
            IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.confirmationDialog = serviceProvider.GetService<IConfirmationDialog>();
            this.jobService = serviceProvider.GetService<IJobService>();
            this.osLoginService = serviceProvider.GetService<Service<IOsLoginService>>();
            this.computeEngineAdapter = serviceProvider.GetService<Service<IComputeEngineAdapter>>();
            this.resourceManagerAdapter = serviceProvider.GetService<Service<IResourceManagerAdapter>>();
        }

        public void ResetWindowTitleAndInformationBar()
        {
            this.WindowTitle = WindowTitlePrefix;
            this.InformationBarContent = null;
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<AuthorizedPublicKeysModel.Item> AllKeys { get; }
            = new RangeObservableCollection<AuthorizedPublicKeysModel.Item>();

        public RangeObservableCollection<AuthorizedPublicKeysModel.Item> FilteredKeys { get; }
            = new RangeObservableCollection<AuthorizedPublicKeysModel.Item>();

        public bool IsListEnabled
        {
            get => this.isListEnabled;
            set
            {
                this.isListEnabled = value;
                RaisePropertyChange();
            }
        }
        public bool IsLoading
        {
            get => this.isLoading;
            set
            {
                this.isLoading = value;
                RaisePropertyChange();
            }
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set
            {
                this.windowTitle = value;
                RaisePropertyChange();
            }
        }

        public bool IsInformationBarVisible => this.InformationBarContent != null;

        public string InformationBarContent
        {
            get => this.informationBarContent;
            private set
            {
                this.informationBarContent = value;
                RaisePropertyChange();
                RaisePropertyChange((AuthorizedPublicKeysViewModel m) => m.IsInformationBarVisible);
            }
        }

        public AuthorizedPublicKeysModel.Item SelectedItem
        {
            get => this.selectedItem;
            set
            {
                this.selectedItem = value;
                RaisePropertyChange();
                RaisePropertyChange((AuthorizedPublicKeysViewModel m) => m.IsDeleteButtonEnabled);
            }
        }

        public bool IsDeleteButtonEnabled => this.selectedItem != null;

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string Filter
        {
            get => this.filter;
            set
            {
                this.filter = value;

                var matches = this.AllKeys
                    .Where(k => string.IsNullOrEmpty(this.filter) ||
                                k.Key.Email.Contains(this.filter) ||
                                k.Key.KeyType.Contains(this.filter));

                this.FilteredKeys.Clear();
                this.FilteredKeys.AddRange(matches);

                RaisePropertyChange((AuthorizedPublicKeysViewModel m) => m.FilteredKeys);
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Static helpers.
        //---------------------------------------------------------------------

        internal static CommandState GetCommandState(IProjectModelNode node)
        {
            return AuthorizedPublicKeysModel.IsNodeSupported(node)
                ? CommandState.Enabled
                : CommandState.Unavailable;
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public Task RefreshAsync() => InvalidateAsync();

        public async Task DeleteSelectedItemAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(this.selectedItem != null);
            Debug.Assert(this.IsDeleteButtonEnabled);
            Debug.Assert(this.View != null);

            string question = "Are you sure you want to delete this key?";
            if (this.selectedItem.AuthorizationMethod == KeyAuthorizationMethods.ProjectMetadata)
            {
                question += " This change affects all VM instances in the project.";
            }

            if (this.confirmationDialog.Confirm(
                this.View,
                question,
                "Delete key for user " + this.selectedItem.Key.Email,
                "Delete key") != DialogResult.Yes)
            {
                return;
            }

            await this.jobService.RunInBackground<object>(
                new JobDescription(
                    $"Deleting SSH keys for {this.selectedItem.Key.Email}",
                    JobUserFeedbackType.BackgroundFeedback),
                async jobToken =>
                {
                    if (this.selectedItem.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
                    {
                        using (var osLoginService = this.osLoginService.GetInstance())
                        {
                            await AuthorizedPublicKeysModel.DeleteFromOsLoginAsync(
                                    osLoginService,
                                    this.selectedItem,
                                    cancellationToken)
                                .ConfigureAwait(true);
                        }
                    }
                    else
                    {
                        await AuthorizedPublicKeysModel.DeleteFromMetadataAsync(
                                this.computeEngineAdapter.GetInstance(),
                                this.resourceManagerAdapter.GetInstance(),
                                this.ModelKey,
                                this.selectedItem,
                                cancellationToken)
                            .ConfigureAwait(true);
                    }

                    return null;
                }).ConfigureAwait(true);  // Back to original (UI) thread.

            //
            // Refresh list.
            //
            await InvalidateAsync()
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        protected override async Task<AuthorizedPublicKeysModel> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(node))
            {
                try
                {
                    this.IsLoading = true;

                    //
                    // Reset window title, otherwise the default or previous title
                    // stays while data is loading.
                    //
                    this.WindowTitle = WindowTitlePrefix;

                    //
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    //
                    return await this.jobService.RunInBackground(
                        new JobDescription(
                            $"Loading SSH keys for {node.DisplayName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var osLoginService = this.osLoginService.GetInstance())
                            {
                                return await AuthorizedPublicKeysModel.LoadAsync(
                                        this.computeEngineAdapter.GetInstance(),
                                        this.resourceManagerAdapter.GetInstance(),
                                        osLoginService,
                                        node,
                                        jobToken)
                                    .ConfigureAwait(false);
                            }
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                }
                finally
                {
                    this.IsLoading = false;
                }
            }
        }

        protected override void ApplyModel(bool cached)
        {
            this.AllKeys.Clear();
            this.FilteredKeys.Clear();
            this.SelectedItem = null;

            if (this.Model == null)
            {
                // Unsupported node.
                this.IsListEnabled = false;
                this.InformationBarContent = null;
                this.WindowTitle = WindowTitlePrefix;
            }
            else
            {
                this.IsListEnabled = true;
                this.InformationBarContent = this.Model.Warnings.Any()
                    ? string.Join(", ", this.Model.Warnings)
                    : null;
                this.WindowTitle = WindowTitlePrefix + $": {this.Model.DisplayName}";
                this.AllKeys.AddRange(this.Model.Items);
            }

            // Reset filter, implicitly populating the FilteredPackages property.
            this.Filter = string.Empty;
        }
    }
}
