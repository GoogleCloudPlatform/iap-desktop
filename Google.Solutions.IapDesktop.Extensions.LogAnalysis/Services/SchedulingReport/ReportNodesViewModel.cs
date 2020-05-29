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
    internal class ReportNodesViewModel : ReportItemsViewModelBase
    {
        private OperatingSystemTypes osTypes = OperatingSystemTypes.Windows;
        private LicenseTypes licenseTypes = LicenseTypes.Byol | LicenseTypes.Spla;

        private NodeSetHistory currentNodeSet;
        private NodeHistory selectedNode;

        protected override void Repopulate()
        {
            // Get instances that match the selected OS/license criteria.
            var instances = this.model.GetInstances(this.osTypes, this.licenseTypes);

            // Derive the set of nodes that were used by those instances.
            this.currentNodeSet = NodeSetHistory.FromInstancyHistory(
                instances,
                Tenancies.SoleTenant);  // Sole tenant nodes only.

            // Create histogram, disregarding the date selection.
            this.Histogram = this.currentNodeSet.MaxNodesByDay;

            // For the list of nodes, apply the date selection.
            this.Nodes.Clear();
            this.Nodes.AddRange(this.currentNodeSet.Nodes
                .Where(n => n.FirstUse <= this.Selection.EndDate && n.LastUse >= this.Selection.StartDate));

            RepopulateNodePlacements();
        }
        private void RepopulateNodePlacements()
        {
            if (this.selectedNode == null)
            {
                this.NodePlacements.Clear();
            }
            else
            {
                this.NodePlacements.AddRange(this.selectedNode.Placements
                    .Where(p => p.From <= this.Selection.EndDate && p.From >= this.Selection.StartDate));
            }
        }

        public ReportNodesViewModel(ReportArchive model)
            : base(model)
        {
            Repopulate();
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<NodeHistory> Nodes { get; }
            = new RangeObservableCollection<NodeHistory>();

        public RangeObservableCollection<NodePlacement> NodePlacements { get; }
            = new RangeObservableCollection<NodePlacement>();

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public NodeHistory SelectedNode
        {
            get => this.selectedNode;
            set
            {
                this.selectedNode = value;

                RepopulateNodePlacements();

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
