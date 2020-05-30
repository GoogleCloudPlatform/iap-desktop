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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{ 
    internal class ReportViewModel : ViewModelBase
    {
        internal ReportArchive Model { get; }

        private int selectedTabIndex;

        private bool isTenancyMenuEnabled = true;
        private bool isOsMenuEnabled = true;
        private bool isLicenseMenuEnabled = true;

        private Tenancies tenancies = Tenancies.SoleTenant | Tenancies.Fleet;
        private OperatingSystemTypes osTypes = OperatingSystemTypes.Windows | OperatingSystemTypes.Unknown;
        private LicenseTypes licenseTypes = LicenseTypes.Byol | LicenseTypes.Spla | LicenseTypes.Unknown;

        public ReportNodesTabViewModel NodeReportPane { get; }
        public ReportInstancesTabViewModel InstanceReportPane { get; }

        public void Repopulate()
        {
            switch (this.selectedTabIndex)
            {
                case 0:
                    this.InstanceReportPane.Repopulate();
                    break;

                case 1:
                    this.NodeReportPane.Repopulate();
                    break;
            }
        }

        internal IEnumerable<InstanceHistory> GetInstancesMatchingCurrentFilters()
        {
            // Get instances that match the selected OS/license criteria.
            return this.Model.GetInstances(this.osTypes, this.licenseTypes);
        }

        internal IEnumerable<NodePlacement> GetNodePlacementsMatchingCurrentFilters(NodeHistory node)
        {
            return node.Placements
                .Where(p => this.Model.IsInstanceAnnotatedAs(
                    p.Instance, this.osTypes, this.licenseTypes));
        }

        internal NodeSetHistory GetNodes()
        { 
            // Derive the set of nodes that were used by those instances.
            return NodeSetHistory.FromInstancyHistory(
                GetInstancesMatchingCurrentFilters(),
                this.tenancies);
        }

        public ReportViewModel(ReportArchive model)
        {
            this.Model = model;
            this.NodeReportPane = new ReportNodesTabViewModel(this);
            this.InstanceReportPane = new ReportInstancesTabViewModel(this);
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        private void SetAndRaisePropertyChange<TEnum>(
            ref TEnum previousValue,
            TEnum flag,
            bool enable,
            [CallerMemberName] string propertyName = null)
            where TEnum : struct
        {
            var previousValueInt = (int)(object)previousValue;
            var flagInt = (int)(object)flag;

            var result = enable
                ? previousValueInt | flagInt
                : previousValueInt & ~flagInt;

            previousValue = (TEnum)(object)result;

            Repopulate();
            RaisePropertyChange(propertyName);
        }

        public void SelectInstancesTab()
        {
            this.SelectedTabIndex = 0;
        }

        public void SelectNodeTab()
        {
            this.SelectedTabIndex = 1;
        }

        public int SelectedTabIndex
        {
            get => this.selectedTabIndex;
            set
            {
                this.selectedTabIndex = value;
                
                this.IsTenancyMenuEnabled = value == 0;
                this.isLicenseMenuEnabled = value <= 1;
                this.IsOsMenuEnabled = value <= 1;

                Repopulate();
                RaisePropertyChange();
            }
        }


        public bool IncludeSoleTenantInstances
        {
            get => this.tenancies.HasFlag(Tenancies.SoleTenant);
            set => SetAndRaisePropertyChange(
                ref this.tenancies, 
                Tenancies.SoleTenant, 
                value);
        }

        public bool IncludeFleetInstances
        {
            get => this.tenancies.HasFlag(Tenancies.Fleet);
            set => SetAndRaisePropertyChange(
                ref this.tenancies,
                Tenancies.Fleet,
                value);
        }

        public bool IncludeLinuxInstances
        {
            get => this.osTypes.HasFlag(OperatingSystemTypes.Linux);
            set => SetAndRaisePropertyChange(
                ref this.osTypes,
                OperatingSystemTypes.Linux,
                value);
        }

        public bool IncludeWindowsInstances
        {
            get => this.osTypes.HasFlag(OperatingSystemTypes.Windows);
            set => SetAndRaisePropertyChange(
                ref this.osTypes,
                OperatingSystemTypes.Windows,
                value);
        }

        public bool IncludeUnknownOsInstances
        {
            get => this.osTypes.HasFlag(OperatingSystemTypes.Unknown);
            set => SetAndRaisePropertyChange(
                ref this.osTypes,
                OperatingSystemTypes.Unknown,
                value);
        }

        public bool IncludeByolInstances
        {
            get => this.licenseTypes.HasFlag(LicenseTypes.Byol);
            set => SetAndRaisePropertyChange(
                ref this.licenseTypes,
                LicenseTypes.Byol,
                value);
        }

        public bool IncludeSplaInstances
        {
            get => this.licenseTypes.HasFlag(LicenseTypes.Spla);
            set => SetAndRaisePropertyChange(
                ref this.licenseTypes,
                LicenseTypes.Spla,
                value);
        }

        public bool IncludeUnknownLicensedInstances
        {
            get => this.licenseTypes.HasFlag(LicenseTypes.Unknown);
            set => SetAndRaisePropertyChange(
                ref this.licenseTypes,
                LicenseTypes.Unknown,
                value);
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public bool IsTenancyMenuEnabled
        {
            get => this.isTenancyMenuEnabled;
            set
            {
                this.isTenancyMenuEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsOsMenuEnabled
        {
            get => this.isOsMenuEnabled;
            set
            {
                this.isOsMenuEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsLicenseMenuEnabled
        {
            get => this.isLicenseMenuEnabled;
            set
            {
                this.isLicenseMenuEnabled = value;
                RaisePropertyChange();
            }
        }
    }
}
