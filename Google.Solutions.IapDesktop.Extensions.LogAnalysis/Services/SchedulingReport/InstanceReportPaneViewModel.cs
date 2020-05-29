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
    internal class InstanceReportPaneViewModel : ReportPaneViewModelBase
    {
        private bool includeSoleTenantInstances = true;
        private bool includeFleetInstances = true;

        protected override void Repopulate()
        {
            var nodeSet = NodeSetHistory.FromInstancyHistory(
                this.instanceSet.Instances,
                this.includeFleetInstances,
                this.includeSoleTenantInstances);

            this.Histogram = nodeSet.MaxInstancePlacementsByDay;

            this.Instances.Clear();
            this.Instances.AddRange(nodeSet.Nodes
                .SelectMany(n => n.Placements)
                .Where(p => p.From <= this.Selection.EndDate && p.To >= this.Selection.StartDate));
        }

        public InstanceReportPaneViewModel(InstanceSetHistory instanceSetHistory)
            : base(instanceSetHistory)
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
            get => this.includeSoleTenantInstances;
            set
            {
                this.includeSoleTenantInstances = value;

                Repopulate();
                RaisePropertyChange();
            }
        }

        public bool IncludeFleetInstances
        {
            get => this.includeFleetInstances;
            set
            {
                this.includeFleetInstances = value;

                Repopulate();
                RaisePropertyChange();
            }
        }
    }
}
