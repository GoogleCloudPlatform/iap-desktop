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
    internal class ReportNodesTabViewModel : ReportItemsViewModelBase
    {
        private readonly ReportViewModel parent;

        private NodeSetHistory currentNodeSet;
        private NodeHistory selectedNode;

        internal override void Repopulate()
        {
            this.currentNodeSet = this.parent.GetNodes();

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
