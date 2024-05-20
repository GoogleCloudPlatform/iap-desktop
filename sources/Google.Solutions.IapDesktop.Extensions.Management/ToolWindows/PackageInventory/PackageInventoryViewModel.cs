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
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory
{
    [Service]
    public class PackageInventoryViewModel
        : ModelCachingViewModelBase<IProjectModelNode, PackageInventoryModel>
    {
        internal const string OsInventoryNotAvailableWarning = "OS inventory data not available";
        private const int ModelCacheCapacity = 5;

        private readonly IJobService jobService;
        private readonly Service<IGuestOsInventory> packageInventory;

        private string filter;

        private string WindowTitlePrefix =>
                this.InventoryType == PackageInventoryType.AvailablePackages
                    ? "Available updates"
                    : "Installed packages";

        internal PackageInventoryType InventoryType { get; set; }

        public PackageInventoryViewModel(IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.packageInventory = serviceProvider.GetService<Service<IGuestOsInventory>>();

            this.IsPackageListEnabled = ObservableProperty.Build(false);
            this.IsLoading = ObservableProperty.Build(false);
            this.WindowTitle = ObservableProperty.Build<string>(null);
            this.InformationText = ObservableProperty.Build<string>(null);
        }

        public void ResetWindowTitle()
        {
            this.WindowTitle.Value = this.WindowTitlePrefix;
        }

        //---------------------------------------------------------------------
        // Static helpers.
        //---------------------------------------------------------------------

        private static bool IsInstanceMatch(InstanceLocator locator, string term)
        {
            term = term.ToLowerInvariant();

            return locator.Zone.Contains(term) ||
                   locator.Name.Contains(term);
        }

        private static bool IsPackageMatch(IPackage package, string term)
        {
            term = term.ToLowerInvariant();

            return
                (package.PackageType != null && package.PackageType.ToLowerInvariant().Contains(term)) ||
                (package.Architecture != null && package.Architecture.ToLowerInvariant().Contains(term)) ||
                (package.Description != null && package.Description.ToLowerInvariant().Contains(term)) ||
                (package.PackageId != null && package.PackageId.ToLowerInvariant().Contains(term)) ||
                (package.Version != null && package.Version.ToLowerInvariant().Contains(term));
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<PackageInventoryModel.Item> AllPackages { get; }
            = new RangeObservableCollection<PackageInventoryModel.Item>();

        public RangeObservableCollection<PackageInventoryModel.Item> FilteredPackages { get; }
            = new RangeObservableCollection<PackageInventoryModel.Item>();

        public ObservableProperty<bool> IsPackageListEnabled { get; }
        public ObservableProperty<bool> IsLoading { get; }
        public ObservableProperty<string> WindowTitle { get; }
        public ObservableProperty<string> InformationText { get; }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string Filter
        {
            get => this.filter;
            set
            {
                this.filter = value;

                IEnumerable<PackageInventoryModel.Item> candidates = this.AllPackages;

                if (!string.IsNullOrWhiteSpace(this.filter))
                {
                    // Treat filter as an AND-combination of terms.

                    foreach (var term in this.filter
                        .Split(' ')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        candidates = candidates.Where(p =>
                                IsPackageMatch(p.Package, term) ||
                                IsInstanceMatch(p.Instance, term))
                            .ToList();
                    }
                }

                this.FilteredPackages.Clear();
                this.FilteredPackages.AddRange(candidates);

                RaisePropertyChange((PackageInventoryViewModel m) => m.FilteredPackages);
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        protected override async Task<PackageInventoryModel> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(node))
            {
                try
                {
                    this.IsLoading.Value = true;

                    //
                    // Reset window title, otherwise the default or previous title
                    // stays while data is loading.
                    //
                    ResetWindowTitle();

                    //
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    //
                    return await this.jobService.RunAsync(
                        new JobDescription(
                            $"Loading inventory for {node.DisplayName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            return await PackageInventoryModel.LoadAsync(
                                    this.packageInventory.Activate(),
                                    this.InventoryType,
                                    node,
                                    jobToken)
                                .ConfigureAwait(false);
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                }
                finally
                {
                    this.IsLoading.Value = false;
                }
            }
        }

        protected override void ApplyModel(bool cached)
        {
            this.AllPackages.Clear();
            this.FilteredPackages.Clear();

            if (this.Model == null)
            {
                //
                // Unsupported node.
                //
                this.IsPackageListEnabled.Value = false;
                this.InformationText.Value = null;
                this.WindowTitle.Value = this.WindowTitlePrefix;
            }
            else
            {
                this.IsPackageListEnabled.Value = true;
                this.InformationText.Value = !this.Model.IsInventoryDataAvailable
                    ? OsInventoryNotAvailableWarning
                    : null;
                this.WindowTitle.Value = this.WindowTitlePrefix + $": {this.Model.DisplayName}";
                this.AllPackages.AddRange(this.Model.Packages);
            }

            //
            // Reset filter, implicitly populating the FilteredPackages property.
            //
            this.Filter = string.Empty;
        }
    }
}
