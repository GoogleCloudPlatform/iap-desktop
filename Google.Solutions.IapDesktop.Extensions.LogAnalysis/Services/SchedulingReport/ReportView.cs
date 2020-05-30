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

using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using DataPoint = Google.Solutions.IapDesktop.Extensions.LogAnalysis.History.DataPoint;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    public partial class ReportView : Form
    {
        private readonly ReportViewModel viewModel;

        public class InstancesListView : BindableListView<NodePlacement>
        { }

        public class NodesListView : BindableListView<NodeHistory>
        { }
        public class NodesPlacementsListView : BindableListView<NodePlacement>
        { }

        internal ReportView(ReportViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.components = new System.ComponentModel.Container();

            InitializeComponent();

            // Remove space between bars.
            this.instancesChart.Series[0]["PointWidth"] = "1";
            this.nodesChart.Series[0]["PointWidth"] = "1";
            this.licenseChart.Series[0]["PointWidth"] = "1";

            this.theme.ApplyTo(this.toolStrip);

            // Bind tab.
            this.tabs.BindProperty(
                t => t.SelectedIndex,
                this.viewModel,
                v => v.SelectedTabIndex,
                this.components);

            // Bind menu.
            this.includeTenancyMenuItem.BindProperty(
                i => i.Enabled,
                this.viewModel,
                v => v.IsTenancyMenuEnabled,
                this.components);
            this.includeOsMenuItem.BindProperty(
                i => i.Enabled,
                this.viewModel,
                v => v.IsOsMenuEnabled,
                this.components);
            this.includeLicenseMenuItem.BindProperty(
                i => i.Enabled,
                this.viewModel,
                v => v.IsLicenseMenuEnabled,
                this.components);

            // Bind 'License' menu items.
            this.includeByolMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeByolInstances,
                this.components);
            this.includeSplaMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeSplaInstances,
                this.components);
            this.includeUnknownLicenseMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeUnknownLicensedInstances,
                this.components);

            // Bind 'OS' menu items.
            this.includeWindowsMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeWindowsInstances,
                this.components);
            this.includeLinuxMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeLinuxInstances,
                this.components);
            this.includeUnknownOsMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeUnknownOsInstances,
                this.components);

            // Bind 'Tenancy' menu items.
            this.includeSoleTenantInstancesMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeSoleTenantInstances,
                this.components);
            this.includeFleetInstancesMenuItem.BindProperty(
                i => i.Checked,
                this.viewModel,
                v => v.IncludeFleetInstances,
                this.components);

            //
            // Instances tab.
            //
            this.instancesList.BindCollection(this.viewModel.InstanceReportPane.Instances);
            this.instancesList.BindColumn(0, n => n.Instance?.InstanceId.ToString());
            this.instancesList.BindColumn(1, n => n.Instance?.Reference?.Name);
            this.instancesList.BindColumn(2, n => n.Instance?.Reference?.Zone);
            this.instancesList.BindColumn(3, n => n.Instance?.Reference?.ProjectId);
            this.instancesList.BindColumn(4, n => n.From.ToString());
            this.instancesList.BindColumn(5, n => n.To.ToString());

            this.components.Add(this.viewModel.InstanceReportPane.OnPropertyChange(
                v => v.Histogram,
                dataPoints =>
                {
                    Plot(dataPoints, this.instancesChart.Series[0]);
                    this.noInstancesDataLabel.Visible = !dataPoints.Any();
                }));

            //
            // Nodes tab.
            //
            this.nodesList.BindCollection(this.viewModel.NodeReportPane.Nodes);
            this.nodesList.BindColumn(0, n => n.ServerId);
            this.nodesList.BindColumn(1, n => n.Zone);
            this.nodesList.BindColumn(2, n => n.ProjectId);
            this.nodesList.BindColumn(3, n => n.FirstUse.ToString());
            this.nodesList.BindColumn(4, n => n.LastUse.ToString());
            this.nodesList.BindColumn(5, n => Math.Ceiling((n.LastUse - n.FirstUse).TotalDays).ToString());
            this.nodesList.BindColumn(6, n => n.PeakConcurrentPlacements.ToString());

            this.nodesList.BindProperty(
                l => l.SelectedModelItem,
                this.viewModel.NodeReportPane,
                v => v.SelectedNode,
                this.components);

            this.nodePlacementsList.BindCollection(this.viewModel.NodeReportPane.NodePlacements);
            this.nodePlacementsList.BindColumn(0, n => n.Instance?.InstanceId.ToString());
            this.nodePlacementsList.BindColumn(1, n => n.Instance?.Reference?.Name);
            this.nodePlacementsList.BindColumn(2, n => n.Instance?.Reference?.Zone);
            this.nodePlacementsList.BindColumn(3, n => n.Instance?.Reference?.ProjectId);
            this.nodePlacementsList.BindColumn(4, n => n.From.ToString());
            this.nodePlacementsList.BindColumn(5, n => n.To.ToString());

            this.components.Add(this.viewModel.NodeReportPane.OnPropertyChange(
                v => v.Histogram,
                dataPoints => 
                {
                    Plot(dataPoints, this.nodesChart.Series[0]);
                    this.noNodesDataLabel.Visible = !dataPoints.Any();
                }));

            //
            // Licenses tab.
            //
            this.nodeTypeInfoLabel.BindProperty(
                l => l.Text,
                this.viewModel,
                v => v.LicensesReportPane.NodeTypeWarning,
                this.components);
            this.components.Add(this.viewModel.LicensesReportPane.OnPropertyChange(
                v => v.Histogram,
                dataPoints =>
                {
                    Plot(dataPoints, this.licenseChart.Series[0]);
                    this.noLicenseDataLabel.Visible = !dataPoints.Any();
                }));

            this.viewModel.Repopulate();
        }

        private static void Plot(IEnumerable<DataPoint> dataPoints, Series series)
        {
            series.Points.Clear();
            foreach (var dp in dataPoints)
            {
                series.Points.AddXY(dp.Timestamp, dp.Value);
            }
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private void menuItemToggle_Click(object sender, System.EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                // Toggle state, this will cause model binding to kick in.
                menuItem.Checked = !menuItem.Checked;
            }
        }

        private void instancesChart_SelectionRangeChanged(object sender, CursorEventArgs e)
        {
            this.viewModel.InstanceReportPane.Selection = new DateSelection()
            {
                StartDate = DateTime.FromOADate(Math.Min(e.NewSelectionStart, e.NewSelectionEnd)),
                EndDate = DateTime.FromOADate(Math.Max(e.NewSelectionStart, e.NewSelectionEnd))
            };
        }

        private void nodesChart_SelectionRangeChanged(object sender, CursorEventArgs e)
        {
            this.viewModel.NodeReportPane.Selection = new DateSelection()
            {
                StartDate = DateTime.FromOADate(Math.Min(e.NewSelectionStart, e.NewSelectionEnd)),
                EndDate = DateTime.FromOADate(Math.Max(e.NewSelectionStart, e.NewSelectionEnd))
            };
        }

        private void chart_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            switch (e.HitTestResult.ChartElementType)
            {
                case ChartElementType.DataPoint:
                    var dataPoint = e.HitTestResult.Series.Points[e.HitTestResult.PointIndex];
                    e.Text = $"{DateTime.FromOADate(dataPoint.XValue):d}: {dataPoint.YValues[0]}";
                    break;
            }
        }
    }
}
