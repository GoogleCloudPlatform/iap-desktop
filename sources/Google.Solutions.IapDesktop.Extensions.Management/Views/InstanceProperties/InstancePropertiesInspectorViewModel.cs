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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.IapDesktop.Extensions.Management.Services.Inventory;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Cache;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.InstanceProperties
{
    [Service]
    public class InstancePropertiesInspectorViewModel
        : ModelCachingViewModelBase<IProjectModelNode, InstancePropertiesInspectorModel>, 
          IPropertiesInspectorViewModel
    {
        private const string OsInventoryNotAvailableWarning = "OS inventory data not available";
        private const int ModelCacheCapacity = 5;
        internal const string DefaultWindowTitle = "VM instance";

        private readonly IJobService jobService;
        private readonly Service<IInventoryService> inventoryService;
        private readonly Service<IComputeEngineAdapter> computeEngineAdapter;


        public InstancePropertiesInspectorViewModel(IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.inventoryService = serviceProvider.GetService<Service<IInventoryService>>();
            this.computeEngineAdapter = serviceProvider.GetService<Service<IComputeEngineAdapter>>();

            this.informationText = ObservableProperty.Build<string>(null);
            this.inspectedObject = ObservableProperty.Build<object>(null);
            this.windowTitle = ObservableProperty.Build(DefaultWindowTitle);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        private readonly ObservableProperty<string> informationText;
        private readonly ObservableProperty<object> inspectedObject;
        private readonly ObservableProperty<string> windowTitle;

        public IObservableProperty<string> InformationText => this.informationText;
        public IObservableProperty<object> InspectedObject => this.inspectedObject;
        public IObservableProperty<string> WindowTitle => this.windowTitle;

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

        protected override async Task<InstancePropertiesInspectorModel> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(node))
            {
                if (node is IProjectModelInstanceNode vmNode)
                {
                    //
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    //
                    return await this.jobService
                        .RunInBackground(
                            new JobDescription(
                                $"Loading information about {vmNode.Instance.Name}",
                                JobUserFeedbackType.BackgroundFeedback),
                            async jobToken =>
                            {
                                using (var combinedTokenSource = jobToken.Combine(token))
                                {
                                    return await InstancePropertiesInspectorModel
                                        .LoadAsync(
                                            vmNode.Instance,
                                            this.computeEngineAdapter.GetInstance(),
                                            this.inventoryService.GetInstance(),
                                            combinedTokenSource.Token)
                                        .ConfigureAwait(false);
                                }
                            })
                        .ConfigureAwait(true);  // Back to original (UI) thread.
                }
                else
                {
                    //
                    // Unknown/unsupported node.
                    //
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
                    //
                    // Unsupported node.
                    //
                    this.informationText.Value = null;
                    this.inspectedObject.Value = null;
                    this.windowTitle.Value = DefaultWindowTitle;
                }
                else
                {
                    this.informationText.Value = !this.Model.IsOsInventoryInformationPopulated
                        ? OsInventoryNotAvailableWarning
                        : null;
                    this.inspectedObject.Value = this.Model;
                    this.windowTitle.Value = DefaultWindowTitle + $": {this.Model.InstanceName}";
                }
            }
        }
    }
}
