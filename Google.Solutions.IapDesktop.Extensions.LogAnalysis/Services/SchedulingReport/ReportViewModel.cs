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

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{ 
    internal class ReportViewModel : ViewModelBase
    {
        internal ReportArchive Model { get; }

        private int selectedTabIndex;

        private bool isTenancyMenuEnabled;
        private bool isOsMenuEnabled;
        private bool isLicenseMenuEnabled;

        private Tenancies tenancies = Tenancies.SoleTenant | Tenancies.Fleet;
        private OperatingSystemTypes osTypes = OperatingSystemTypes.Windows;
        private LicenseTypes licenseTypes = LicenseTypes.Byol | LicenseTypes.Spla;

        public ReportNodesTabViewModel NodeReportPane { get; }
        public ReportInstancesTabViewModel InstanceReportPane { get; }

        private void Repopulate()
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

        internal NodeSetHistory GetNodes()
        {
            // Get instances that match the selected OS/license criteria.
            var instances = this.Model.GetInstances(this.osTypes, this.licenseTypes);

            // Derive the set of nodes that were used by those instances.
            return NodeSetHistory.FromInstancyHistory(
                instances,
                this.tenancies);
        }

        public ReportViewModel(ReportArchive model)
        {
            this.Model = model;
            this.NodeReportPane = new ReportNodesTabViewModel(this);
            this.InstanceReportPane = new ReportInstancesTabViewModel(this);
            this.SelectedTabIndex = 0;
            Repopulate();
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

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
            get => this.licenseTypes.HasFlag(LicenseTypes.Byol);
            set
            {
                this.licenseTypes |= LicenseTypes.Byol;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeSplaInstances
        {
            get => this.licenseTypes.HasFlag(LicenseTypes.Spla);
            set
            {
                this.licenseTypes |= LicenseTypes.Spla;

                Repopulate();
                RaisePropertyChange();
            }
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
