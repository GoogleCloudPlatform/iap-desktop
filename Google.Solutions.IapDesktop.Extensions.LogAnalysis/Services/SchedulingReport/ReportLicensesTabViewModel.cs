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

using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    internal class ReportLicensesTabViewModel : ReportItemsViewModelBase
    {
        private readonly ReportViewModel parent;

        internal override void Repopulate()
        {
            // Get nodes with Windows/BYOL placements.
            var nodeSet = NodeSetHistory.FromInstancyHistory(
                this.parent.Model.GetInstances(OperatingSystemTypes.Windows, LicenseTypes.Byol),
                Tenancies.SoleTenant);
            
            // Create histogram, disregarding the date selection.
            this.Histogram = nodeSet.MaxNodesByDay
                .Select(dp => new DataPoint()
                {
                    Timestamp = dp.Timestamp,

                    // XXX: This is only accurate as long as all nodes use the
                    // same node type.
                    Value = dp.Value * NodeAnnotation.Default.PhysicalCores
                });
        }

        public ReportLicensesTabViewModel(ReportViewModel parent)
            : base(parent.Model)
        {
            this.parent = parent;
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public string NodeTypeWarning =>
            $"All nodes are assumed to use the node type {NodeAnnotation.Default.NodeType}" +
            $" ({NodeAnnotation.Default.PhysicalCores} cores)";

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------
    }
}
