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
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    public partial class ReportView : Form
    {
        private readonly ReportViewModel viewModel;

        internal ReportView(ReportViewModel videModel)
        {
            this.viewModel = videModel;
            this.components = new System.ComponentModel.Container();

            InitializeComponent();

            // Remove space between bars.
            this.instancesChart.Series[0]["PointWidth"] = "1";

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

            this.components.Add(this.viewModel.InstanceReportPane.OnPropertyChange(
                v => v.Histogram,
                dataPoints =>
                {
                    this.instancesChart.Series[0].Points.Clear();
                    foreach (var dp in dataPoints)
                    {
                        this.instancesChart.Series[0].Points.AddXY(dp.Timestamp, dp.Value);
                    }
                }));

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

            this.viewModel.Repopulate();
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

        public class InstancesListView : BindableListView<NodePlacement>
        { }
    }
}
