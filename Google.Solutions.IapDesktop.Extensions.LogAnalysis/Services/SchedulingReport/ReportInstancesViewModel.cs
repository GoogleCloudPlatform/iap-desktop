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

using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    internal class ReportInstancesViewModel : ReportItemsViewModelBase
    {
        private Tenancies tenancies = Tenancies.SoleTenant | Tenancies.Fleet;
        private OperatingSystemTypes osTypes = OperatingSystemTypes.Windows;
        private LicenseTypes licenseTypes = LicenseTypes.Byol | LicenseTypes.Spla;

        protected override void Repopulate()
        {
            // Get instances that match the selected OS/license criteria.
            var instances = this.model.GetInstances(this.osTypes, this.licenseTypes);

            // Derive the set of nodes that were used by those instances.
            var nodeSet = NodeSetHistory.FromInstancyHistory(
                instances,
                this.tenancies);

            // Create histogram, disregarding the date selection.
            this.Histogram = nodeSet.MaxInstancePlacementsByDay;

            // For the list of instances, apply the date selection.
            this.Instances.Clear();
            this.Instances.AddRange(nodeSet.Nodes
                .SelectMany(n => n.Placements)
                .Where(p => p.From <= this.Selection.EndDate && p.To >= this.Selection.StartDate));
        }

        public ReportInstancesViewModel(ReportArchive model)
            : base(model)
        {
            Repopulate();
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<NodePlacement> Instances { get; }
            = new RangeObservableCollection<NodePlacement>();

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public bool IncludeSoleTenantInstances
        {
            get => this.tenancies.HasFlag(Tenancies.SoleTenant);
            set
            {
                this.tenancies |= Tenancies.SoleTenant;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeFleetInstances
        {
            get => this.tenancies.HasFlag(Tenancies.Fleet);
            set
            {
                this.tenancies |= Tenancies.Fleet;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeLinuxInstances
        {
            get => this.osTypes.HasFlag(OperatingSystemTypes.Linux);
            set
            {
                this.osTypes |= OperatingSystemTypes.Linux;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeWindowsInstances
        {
            get => this.osTypes.HasFlag(OperatingSystemTypes.Windows);
            set
            {
                this.osTypes |= OperatingSystemTypes.Windows;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeByolInstances
        {
            get => this.osTypes.HasFlag(LicenseTypes.Byol);
            set
            {
                this.licenseTypes |= LicenseTypes.Byol;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeSplaInstances
        {
            get => this.osTypes.HasFlag(LicenseTypes.Spla);
            set
            {
                this.licenseTypes |= LicenseTypes.Spla;

                Repopulate();
                RaisePropertyChange();
            }
        }
    }
}
