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
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys
{
    public class AuthorizedPublicKeysViewModel
        : ModelCachingViewModelBase<IProjectModelNode, AuthorizedPublicKeysModel>
    {
        private const int ModelCacheCapacity = 5;
        private const string WindowTitlePrefix = "Metadata authorized SSH keys";

        private readonly IServiceProvider serviceProvider;

        private string filter;
        private bool isLoading;
        private bool isListEnabled = false;
        private string windowTitle;
        private string informationBarContent;

        public AuthorizedPublicKeysViewModel(
            IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.serviceProvider = serviceProvider;
        }

        public void ResetWindowTitle()
        {
            this.WindowTitle = WindowTitlePrefix;
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
                    .Where(k => k.Key.Email.Contains(this.filter) || k.Key.KeyType.Contains(this.filter));

                this.FilteredKeys.Clear();
                this.FilteredKeys.AddRange(matches);

                RaisePropertyChange((AuthorizedPublicKeysViewModel m) => m.FilteredKeys);
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Static helpers.
        //---------------------------------------------------------------------

        // TODO: test
        internal static CommandState GetCommandState(IProjectModelNode node)
        {
            return AuthorizedPublicKeysModel.IsNodeSupported(node)
                ? CommandState.Enabled
                : CommandState.Unavailable;
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        // TODO: test
        protected async override Task<AuthorizedPublicKeysModel> LoadModelAsync(
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

                    var jobService = this.serviceProvider.GetService<IJobService>();

                    //
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    //
                    return await jobService.RunInBackground(
                        new JobDescription(
                            $"Loading SSH keys for {node.DisplayName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var computeEngineAdapter = this.serviceProvider.GetService<IComputeEngineAdapter>())
                            using (var resourceManagerAdapter = this.serviceProvider.GetService<IResourceManagerAdapter>())
                            using (var osLoginService = this.serviceProvider.GetService<IOsLoginService>())
                            {
                                return await AuthorizedPublicKeysModel.LoadAsync(
                                        computeEngineAdapter,
                                        resourceManagerAdapter,
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

        // TODO: test
        protected override void ApplyModel(bool cached)
        {
            this.AllKeys.Clear();
            this.FilteredKeys.Clear();

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
