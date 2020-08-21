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
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Views.UsageReport
{
    internal class ReportNodesTabViewModel : ReportItemsViewModelBase
    {
        private readonly ReportViewModel parent;

        private NodeSetHistory currentNodeSet;
        private NodeHistory selectedNode;

        internal override void Repopulate()
        {
            // Get instances, filtered by whatever filter applies,
            // then derive sole tenant nodes (ignoring fleet).
            this.currentNodeSet = NodeSetHistory.FromInstancyHistory(
                this.parent.GetInstancesMatchingCurrentFilters(),
                Tenancies.SoleTenant);

            // Create histogram, disregarding the date selection.
            this.Histogram = this.currentNodeSet.MaxNodesByDay;

            // Normally, all nodes should have a proper Server ID. But if a VM
            // as been freshly placed on a node, the Server ID might not be known
            // yet (null). 
            Debug.Assert(this.currentNodeSet.Nodes.All(
                n => n.ServerId != null ||
                     n.Placements.All(p => p.From == p.To)));

            // For the list of nodes, apply the date selection.
            this.Nodes.Clear();
            this.Nodes.AddRange(this.currentNodeSet.Nodes
                .Where(n => n.FirstUse <= this.Selection.EndDate && n.LastUse >= this.Selection.StartDate));

            this.selectedNode = null;
            RepopulateNodePlacements();
        }

        private void RepopulateNodePlacements()
        {
            this.NodePlacements.Clear();
            if (this.selectedNode != null)
            {
                this.NodePlacements.AddRange(
                    this.parent.GetNodePlacementsMatchingCurrentFilters(this.selectedNode)
                        .Where(p => p.From <= this.Selection.EndDate && p.To >= this.Selection.StartDate));
            }
        }

        public ReportNodesTabViewModel(ReportViewModel parent)
            : base(parent.Model)
        {
            this.parent = parent;
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
    }
}
