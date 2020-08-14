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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.PackageReport
{
    internal class ReportViewModel : ViewModelBase
    {
        private readonly IEnumerable<InstancePackage> allPackages;

        private string filter;

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
                (package.Architecture != null && package.Architecture.Contains(term)) ||
                (package.Description != null && package.Description.Contains(term)) ||
                (package.PackageId != null && package.PackageId.Contains(term)) ||
                (package.Version != null && package.Version.Contains(term));
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<InstancePackage> FilteredPackages { get; }
            = new RangeObservableCollection<InstancePackage>();

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string Filter
        {
            get => this.filter;
            set
            {
                this.filter = value;

                var candidates = this.allPackages;

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

                RaisePropertyChange((ReportViewModel m) => m.FilteredPackages);
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        internal ReportViewModel(IEnumerable<InstancePackage> packages)
        {
            this.allPackages = packages;
            this.FilteredPackages.AddRange(this.allPackages);
        }

        internal static ReportViewModel FromAvailablePackages(
            IEnumerable<GuestOsInfo> inventory)
        {
            return new ReportViewModel(
                inventory.SelectMany(i => i.AvailablePackages
                    .AllPackages
                    .Select(p => new InstancePackage(i.Instance, p))));
        }

        internal static ReportViewModel FromInstalledPackages(
            IEnumerable<GuestOsInfo> inventory)
        {
            return new ReportViewModel(
                inventory.SelectMany(i => i.InstalledPackages
                    .AllPackages
                    .Select(p => new InstancePackage(i.Instance, p))));
        }
    }
}
