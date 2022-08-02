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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.ObjectModel.Commands;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.InstanceProperties
{
    internal class InstancePropertiesInspectorViewModel
        : ModelCachingViewModelBase<IProjectModelNode, InstancePropertiesInspectorModel>, IPropertiesInspectorViewModel
    {
        private const int ModelCacheCapacity = 5;
        internal const string DefaultWindowTitle = "VM instance";

        private readonly IServiceProvider serviceProvider;

        private bool isInformationBarVisible = false;
        private object inspectedObject = null;
        private string windowTitle = DefaultWindowTitle;

        public string InformationText => "OS inventory data not available";

        public InstancePropertiesInspectorViewModel(IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.serviceProvider = serviceProvider;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsInformationBarVisible
        {
            get => isInformationBarVisible;
            private set
            {
                this.isInformationBarVisible = value;
                RaisePropertyChange();
            }
        }

        public object InspectedObject
        {
            get => this.inspectedObject;
            private set
            {
                this.inspectedObject = value;
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

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void SaveChanges()
        {
            Debug.Assert(
                false,
                "All properties are read-only, so this should never be called");
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        public static CommandState GetContextMenuCommandState(IProjectModelNode node)
        {
            return node is IProjectModelInstanceNode
                ? CommandState.Enabled
                : CommandState.Unavailable;
        }

        public static CommandState GetToolbarCommandState(IProjectModelNode node)
        {
            return node is IProjectModelInstanceNode
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        protected async override Task<InstancePropertiesInspectorModel> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(node))
            {
                if (node is IProjectModelInstanceNode vmNode)
                {
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    var jobService = this.serviceProvider.GetService<IJobService>();
                    return await jobService.RunInBackground(
                        new JobDescription(
                            $"Loading information about {vmNode.Instance.Name}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var combinedTokenSource = jobToken.Combine(token))
                            using (var gceAdapter = this.serviceProvider.GetService<IComputeEngineAdapter>())
                            using (var inventoryService = this.serviceProvider.GetService<IInventoryService>())
                            {
                                return await InstancePropertiesInspectorModel.LoadAsync(
                                    vmNode.Instance,
                                    gceAdapter,
                                    inventoryService,
                                    combinedTokenSource.Token)
                                    .ConfigureAwait(false);
                            }
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                }
                else
                {
                    // Unknown/unsupported node.
                    return null;
                }
            }
        }

        protected override void ApplyModel(bool cached)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(this.Model, cached))
            {
                if (this.Model == null)
                {
                    // Unsupported node.
                    this.IsInformationBarVisible = false;
                    this.InspectedObject = null;
                    this.WindowTitle = DefaultWindowTitle;
                }
                else
                {
                    this.IsInformationBarVisible = !this.Model.IsOsInventoryInformationPopulated;
                    this.InspectedObject = this.Model;
                    this.WindowTitle = DefaultWindowTitle + $": {this.Model.InstanceName}";
                }
            }
        }
    }
}
