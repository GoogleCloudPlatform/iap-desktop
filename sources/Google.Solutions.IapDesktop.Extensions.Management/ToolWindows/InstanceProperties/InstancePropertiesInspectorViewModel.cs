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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Threading;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ToolWindows.Properties;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Cache;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.InstanceProperties
{
    [Service]
    public class InstancePropertiesInspectorViewModel
        : ModelCachingViewModelBase<IProjectModelNode, InstancePropertiesInspectorModel>,
          IPropertiesInspectorViewModel
    {
        private const string OsInventoryNotAvailableWarning = "OS inventory is disabled";
        private const int ModelCacheCapacity = 5;
        internal const string DefaultWindowTitle = "VM instance";

        private readonly IJobService jobService;
        private readonly Service<IGuestOsInventory> packageInventory;
        private readonly Service<IComputeEngineClient> computeClient;


        public InstancePropertiesInspectorViewModel(IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.packageInventory = serviceProvider.GetService<Service<IGuestOsInventory>>();
            this.computeClient = serviceProvider.GetService<Service<IComputeEngineClient>>();

            this.informationText = ObservableProperty.Build<string?>(null);
            this.inspectedObject = ObservableProperty.Build<object?>(null);
            this.windowTitle = ObservableProperty.Build(DefaultWindowTitle);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        private readonly ObservableProperty<string?> informationText;
        private readonly ObservableProperty<object?> inspectedObject;
        private readonly ObservableProperty<string> windowTitle;

        public IObservableProperty<string?> InformationText => this.informationText;
        public IObservableProperty<object?> InspectedObject => this.inspectedObject;
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

        protected override async Task<InstancePropertiesInspectorModel?> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(node))
            {
                if (node is IProjectModelInstanceNode vmNode)
                {
                    //
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    //
                    return await this.jobService
                        .RunAsync(
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
                                            this.computeClient.Activate(),
                                            this.packageInventory.Activate(),
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
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(this.Model, cached))
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
