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

namespace Google.Solutions.IapDesktop.Extensions.Activity.Views.UsageReport
{
    partial class ReportPaneView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportPaneView));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.includeTenancyMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeSoleTenantInstancesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeFleetInstancesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeOsMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeWindowsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeLinuxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeUnknownOsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeLicenseMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeByolMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeSplaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeUnknownLicenseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.theme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.tabs = new Google.Solutions.IapDesktop.Application.Controls.FlatVerticalTabControl();
            this.instancesTab = new System.Windows.Forms.TabPage();
            this.noInstancesDataLabel = new System.Windows.Forms.Label();
            this.instancesList = new Google.Solutions.IapDesktop.Extensions.Activity.Views.UsageReport.ReportPaneView.InstancesListView();
            this.instanceIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceNameColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceZoneColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceProjectIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.placedFromColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.placedUntilColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.osColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.licenseColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instancesHeader = new System.Windows.Forms.Label();
            this.instancesChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.nodesTab = new System.Windows.Forms.TabPage();
            this.noNodesDataLabel = new System.Windows.Forms.Label();
            this.nodesSplitContainer = new System.Windows.Forms.SplitContainer();
            this.nodesList = new Google.Solutions.IapDesktop.Extensions.Activity.Views.UsageReport.ReportPaneView.NodesListView();
            this.serverIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodeZoneColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodeProjectIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.firstUseColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lastUseColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.daysUsedColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.peakInstancesColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementsHeadline = new System.Windows.Forms.Label();
            this.nodePlacementsList = new Google.Solutions.IapDesktop.Extensions.Activity.Views.UsageReport.ReportPaneView.NodesPlacementsListView();
            this.nodePlacementInstanceIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementInstanceNameColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementZoneColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementProjectIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementFromColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementToColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodesHeadline = new System.Windows.Forms.Label();
            this.nodesChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.licensesTab = new System.Windows.Forms.TabPage();
            this.nodeTypeInfoLabel = new System.Windows.Forms.Label();
            this.infoIcon = new System.Windows.Forms.PictureBox();
            this.noLicenseDataLabel = new System.Windows.Forms.Label();
            this.licenseChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.byolHeadline = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            this.tabs.SuspendLayout();
            this.instancesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.instancesChart)).BeginInit();
            this.nodesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodesSplitContainer)).BeginInit();
            this.nodesSplitContainer.Panel1.SuspendLayout();
            this.nodesSplitContainer.Panel2.SuspendLayout();
            this.nodesSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodesChart)).BeginInit();
            this.licensesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.infoIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.licenseChart)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeTenancyMenuItem,
            this.includeOsMenuItem,
            this.includeLicenseMenuItem});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip";
            // 
            // includeTenancyMenuItem
            // 
            this.includeTenancyMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeSoleTenantInstancesMenuItem,
            this.includeFleetInstancesMenuItem});
            this.includeTenancyMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("includeTenancyMenuItem.Image")));
            this.includeTenancyMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.includeTenancyMenuItem.Name = "includeTenancyMenuItem";
            this.includeTenancyMenuItem.Size = new System.Drawing.Size(79, 22);
            this.includeTenancyMenuItem.Text = "Tenancy";
            // 
            // includeSoleTenantInstancesMenuItem
            // 
            this.includeSoleTenantInstancesMenuItem.Name = "includeSoleTenantInstancesMenuItem";
            this.includeSoleTenantInstancesMenuItem.Size = new System.Drawing.Size(147, 22);
            this.includeSoleTenantInstancesMenuItem.Text = "Sole tenant";
            this.includeSoleTenantInstancesMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeFleetInstancesMenuItem
            // 
            this.includeFleetInstancesMenuItem.Name = "includeFleetInstancesMenuItem";
            this.includeFleetInstancesMenuItem.Size = new System.Drawing.Size(147, 22);
            this.includeFleetInstancesMenuItem.Text = "Shared tenant";
            this.includeFleetInstancesMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeOsMenuItem
            // 
            this.includeOsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeWindowsMenuItem,
            this.includeLinuxMenuItem,
            this.includeUnknownOsMenuItem});
            this.includeOsMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("includeOsMenuItem.Image")));
            this.includeOsMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.includeOsMenuItem.Name = "includeOsMenuItem";
            this.includeOsMenuItem.Size = new System.Drawing.Size(51, 22);
            this.includeOsMenuItem.Text = "OS";
            // 
            // includeWindowsMenuItem
            // 
            this.includeWindowsMenuItem.Name = "includeWindowsMenuItem";
            this.includeWindowsMenuItem.Size = new System.Drawing.Size(143, 22);
            this.includeWindowsMenuItem.Text = "Windows";
            this.includeWindowsMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeLinuxMenuItem
            // 
            this.includeLinuxMenuItem.Name = "includeLinuxMenuItem";
            this.includeLinuxMenuItem.Size = new System.Drawing.Size(143, 22);
            this.includeLinuxMenuItem.Text = "Linux";
            this.includeLinuxMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeUnknownOsMenuItem
            // 
            this.includeUnknownOsMenuItem.Name = "includeUnknownOsMenuItem";
            this.includeUnknownOsMenuItem.Size = new System.Drawing.Size(143, 22);
            this.includeUnknownOsMenuItem.Text = "Unknown OS";
            this.includeUnknownOsMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeLicenseMenuItem
            // 
            this.includeLicenseMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeByolMenuItem,
            this.includeSplaMenuItem,
            this.includeUnknownLicenseMenuItem});
            this.includeLicenseMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("includeLicenseMenuItem.Image")));
            this.includeLicenseMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.includeLicenseMenuItem.Name = "includeLicenseMenuItem";
            this.includeLicenseMenuItem.Size = new System.Drawing.Size(75, 22);
            this.includeLicenseMenuItem.Text = "License";
            // 
            // includeByolMenuItem
            // 
            this.includeByolMenuItem.Name = "includeByolMenuItem";
            this.includeByolMenuItem.Size = new System.Drawing.Size(195, 22);
            this.includeByolMenuItem.Text = "Bring your own (BYOL)";
            this.includeByolMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeSplaMenuItem
            // 
            this.includeSplaMenuItem.Name = "includeSplaMenuItem";
            this.includeSplaMenuItem.Size = new System.Drawing.Size(195, 22);
            this.includeSplaMenuItem.Text = "Pay as you go (SPLA)";
            this.includeSplaMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // includeUnknownLicenseMenuItem
            // 
            this.includeUnknownLicenseMenuItem.Name = "includeUnknownLicenseMenuItem";
            this.includeUnknownLicenseMenuItem.Size = new System.Drawing.Size(195, 22);
            this.includeUnknownLicenseMenuItem.Text = "Unknown license";
            this.includeUnknownLicenseMenuItem.Click += new System.EventHandler(this.menuItemToggle_Click);
            // 
            // tabs
            // 
            this.tabs.Alignment = System.Windows.Forms.TabAlignment.Left;
            this.tabs.Controls.Add(this.instancesTab);
            this.tabs.Controls.Add(this.nodesTab);
            this.tabs.Controls.Add(this.licensesTab);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.ItemSize = new System.Drawing.Size(44, 136);
            this.tabs.Location = new System.Drawing.Point(0, 25);
            this.tabs.Multiline = true;
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(800, 775);
            this.tabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabs.TabIndex = 1;
            // 
            // instancesTab
            // 
            this.instancesTab.Controls.Add(this.noInstancesDataLabel);
            this.instancesTab.Controls.Add(this.instancesList);
            this.instancesTab.Controls.Add(this.instancesHeader);
            this.instancesTab.Controls.Add(this.instancesChart);
            this.instancesTab.Location = new System.Drawing.Point(140, 4);
            this.instancesTab.Name = "instancesTab";
            this.instancesTab.Padding = new System.Windows.Forms.Padding(3);
            this.instancesTab.Size = new System.Drawing.Size(656, 767);
            this.instancesTab.TabIndex = 0;
            this.instancesTab.Text = "Instances";
            this.instancesTab.UseVisualStyleBackColor = true;
            // 
            // noInstancesDataLabel
            // 
            this.noInstancesDataLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.noInstancesDataLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.noInstancesDataLabel.Location = new System.Drawing.Point(330, 160);
            this.noInstancesDataLabel.Name = "noInstancesDataLabel";
            this.noInstancesDataLabel.Size = new System.Drawing.Size(0, 13);
            this.noInstancesDataLabel.TabIndex = 5;
            this.noInstancesDataLabel.Text = "No data to display";
            this.noInstancesDataLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // instancesList
            // 
            this.instancesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.instancesList.AutoResizeColumnsOnUpdate = false;
            this.instancesList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.instanceIdColumnHeader,
            this.instanceNameColumnHeader,
            this.instanceZoneColumnHeader,
            this.instanceProjectIdColumnHeader,
            this.placedFromColumnHeader,
            this.placedUntilColumnHeader,
            this.osColumnHeader,
            this.licenseColumnHeader});
            this.instancesList.FullRowSelect = true;
            this.instancesList.HideSelection = false;
            this.instancesList.Location = new System.Drawing.Point(6, 336);
            this.instancesList.Name = "instancesList";
            this.instancesList.OwnerDraw = true;
            this.instancesList.SelectedModelItem = null;
            this.instancesList.Size = new System.Drawing.Size(642, 428);
            this.instancesList.TabIndex = 2;
            this.instancesList.UseCompatibleStateImageBehavior = false;
            this.instancesList.View = System.Windows.Forms.View.Details;
            // 
            // instanceIdColumnHeader
            // 
            this.instanceIdColumnHeader.Text = "Instance ID";
            this.instanceIdColumnHeader.Width = 120;
            // 
            // instanceNameColumnHeader
            // 
            this.instanceNameColumnHeader.Text = "Instance name";
            this.instanceNameColumnHeader.Width = 130;
            // 
            // instanceZoneColumnHeader
            // 
            this.instanceZoneColumnHeader.Text = "Zone";
            this.instanceZoneColumnHeader.Width = 80;
            // 
            // instanceProjectIdColumnHeader
            // 
            this.instanceProjectIdColumnHeader.Text = "Project Id";
            this.instanceProjectIdColumnHeader.Width = 120;
            // 
            // placedFromColumnHeader
            // 
            this.placedFromColumnHeader.Text = "From (UTC)";
            this.placedFromColumnHeader.Width = 130;
            // 
            // placedUntilColumnHeader
            // 
            this.placedUntilColumnHeader.Text = "To (UTC)";
            this.placedUntilColumnHeader.Width = 130;
            // 
            // osColumnHeader
            // 
            this.osColumnHeader.Text = "OS";
            // 
            // licenseColumnHeader
            // 
            this.licenseColumnHeader.Text = "License";
            this.licenseColumnHeader.Width = 25;
            // 
            // instancesHeader
            // 
            this.instancesHeader.AutoSize = true;
            this.instancesHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.instancesHeader.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.instancesHeader.Location = new System.Drawing.Point(7, 10);
            this.instancesHeader.Name = "instancesHeader";
            this.instancesHeader.Size = new System.Drawing.Size(158, 17);
            this.instancesHeader.TabIndex = 1;
            this.instancesHeader.Text = "Scheduled instances";
            // 
            // instancesChart
            // 
            this.instancesChart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.instancesChart.BackColor = System.Drawing.SystemColors.Control;
            chartArea1.AxisX.MajorGrid.Enabled = false;
            chartArea1.AxisX.ScaleView.Zoomable = false;
            chartArea1.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.SystemColors.ControlDarkDark;
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea1.BackColor = System.Drawing.SystemColors.Control;
            chartArea1.BorderColor = System.Drawing.Color.DimGray;
            chartArea1.CursorX.IsUserSelectionEnabled = true;
            chartArea1.Name = "mainArea";
            chartArea1.Position.Auto = false;
            chartArea1.Position.Height = 94F;
            chartArea1.Position.Width = 100F;
            chartArea1.Position.Y = 3F;
            this.instancesChart.ChartAreas.Add(chartArea1);
            this.instancesChart.Location = new System.Drawing.Point(6, 30);
            this.instancesChart.Name = "instancesChart";
            this.instancesChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Grayscale;
            series1.ChartArea = "mainArea";
            series1.Name = "Series1";
            this.instancesChart.Series.Add(series1);
            this.instancesChart.Size = new System.Drawing.Size(642, 300);
            this.instancesChart.TabIndex = 0;
            this.instancesChart.GetToolTipText += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ToolTipEventArgs>(this.chart_GetToolTipText);
            this.instancesChart.SelectionRangeChanged += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.CursorEventArgs>(this.instancesChart_SelectionRangeChanged);
            // 
            // nodesTab
            // 
            this.nodesTab.Controls.Add(this.noNodesDataLabel);
            this.nodesTab.Controls.Add(this.nodesSplitContainer);
            this.nodesTab.Controls.Add(this.nodesHeadline);
            this.nodesTab.Controls.Add(this.nodesChart);
            this.nodesTab.Location = new System.Drawing.Point(140, 4);
            this.nodesTab.Name = "nodesTab";
            this.nodesTab.Padding = new System.Windows.Forms.Padding(3);
            this.nodesTab.Size = new System.Drawing.Size(656, 767);
            this.nodesTab.TabIndex = 1;
            this.nodesTab.Text = "Sole-tenant nodes";
            this.nodesTab.UseVisualStyleBackColor = true;
            // 
            // noNodesDataLabel
            // 
            this.noNodesDataLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.noNodesDataLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.noNodesDataLabel.Location = new System.Drawing.Point(330, 160);
            this.noNodesDataLabel.Name = "noNodesDataLabel";
            this.noNodesDataLabel.Size = new System.Drawing.Size(219, 13);
            this.noNodesDataLabel.TabIndex = 4;
            this.noNodesDataLabel.Text = "No sole-tenant nodes used";
            this.noNodesDataLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nodesSplitContainer
            // 
            this.nodesSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodesSplitContainer.Location = new System.Drawing.Point(10, 336);
            this.nodesSplitContainer.Name = "nodesSplitContainer";
            this.nodesSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // nodesSplitContainer.Panel1
            // 
            this.nodesSplitContainer.Panel1.Controls.Add(this.nodesList);
            // 
            // nodesSplitContainer.Panel2
            // 
            this.nodesSplitContainer.Panel2.Controls.Add(this.nodePlacementsHeadline);
            this.nodesSplitContainer.Panel2.Controls.Add(this.nodePlacementsList);
            this.nodesSplitContainer.Size = new System.Drawing.Size(861, 602);
            this.nodesSplitContainer.SplitterDistance = 301;
            this.nodesSplitContainer.TabIndex = 3;
            // 
            // nodesList
            // 
            this.nodesList.AutoResizeColumnsOnUpdate = false;
            this.nodesList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.serverIdColumnHeader,
            this.nodeZoneColumnHeader,
            this.nodeProjectIdColumnHeader,
            this.firstUseColumnHeader,
            this.lastUseColumnHeader,
            this.daysUsedColumnHeader,
            this.peakInstancesColumnHeader});
            this.nodesList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nodesList.FullRowSelect = true;
            this.nodesList.HideSelection = false;
            this.nodesList.Location = new System.Drawing.Point(0, 0);
            this.nodesList.Name = "nodesList";
            this.nodesList.OwnerDraw = true;
            this.nodesList.SelectedModelItem = null;
            this.nodesList.Size = new System.Drawing.Size(861, 301);
            this.nodesList.TabIndex = 0;
            this.nodesList.UseCompatibleStateImageBehavior = false;
            this.nodesList.View = System.Windows.Forms.View.Details;
            // 
            // serverIdColumnHeader
            // 
            this.serverIdColumnHeader.Text = "Server ID";
            this.serverIdColumnHeader.Width = 150;
            // 
            // nodeZoneColumnHeader
            // 
            this.nodeZoneColumnHeader.Text = "Zone";
            this.nodeZoneColumnHeader.Width = 80;
            // 
            // nodeProjectIdColumnHeader
            // 
            this.nodeProjectIdColumnHeader.Text = "Project Id";
            this.nodeProjectIdColumnHeader.Width = 120;
            // 
            // firstUseColumnHeader
            // 
            this.firstUseColumnHeader.Text = "First use (UTC)";
            this.firstUseColumnHeader.Width = 130;
            // 
            // lastUseColumnHeader
            // 
            this.lastUseColumnHeader.Text = "Last Use (UTC)";
            this.lastUseColumnHeader.Width = 130;
            // 
            // daysUsedColumnHeader
            // 
            this.daysUsedColumnHeader.Text = "Days used";
            this.daysUsedColumnHeader.Width = 70;
            // 
            // peakInstancesColumnHeader
            // 
            this.peakInstancesColumnHeader.Text = "Peak VMs";
            this.peakInstancesColumnHeader.Width = 173;
            // 
            // nodePlacementsHeadline
            // 
            this.nodePlacementsHeadline.AutoSize = true;
            this.nodePlacementsHeadline.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodePlacementsHeadline.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.nodePlacementsHeadline.Location = new System.Drawing.Point(-3, 4);
            this.nodePlacementsHeadline.Name = "nodePlacementsHeadline";
            this.nodePlacementsHeadline.Size = new System.Drawing.Size(286, 17);
            this.nodePlacementsHeadline.TabIndex = 3;
            this.nodePlacementsHeadline.Text = "Instances scheduled on selected node";
            // 
            // nodePlacementsList
            // 
            this.nodePlacementsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodePlacementsList.AutoResizeColumnsOnUpdate = false;
            this.nodePlacementsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.nodePlacementInstanceIdColumnHeader,
            this.nodePlacementInstanceNameColumnHeader,
            this.nodePlacementZoneColumnHeader,
            this.nodePlacementProjectIdColumnHeader,
            this.nodePlacementFromColumnHeader,
            this.nodePlacementToColumnHeader});
            this.nodePlacementsList.FullRowSelect = true;
            this.nodePlacementsList.HideSelection = false;
            this.nodePlacementsList.Location = new System.Drawing.Point(0, 29);
            this.nodePlacementsList.Name = "nodePlacementsList";
            this.nodePlacementsList.OwnerDraw = true;
            this.nodePlacementsList.SelectedModelItem = null;
            this.nodePlacementsList.Size = new System.Drawing.Size(861, 268);
            this.nodePlacementsList.TabIndex = 1;
            this.nodePlacementsList.UseCompatibleStateImageBehavior = false;
            this.nodePlacementsList.View = System.Windows.Forms.View.Details;
            // 
            // nodePlacementInstanceIdColumnHeader
            // 
            this.nodePlacementInstanceIdColumnHeader.Text = "Instance ID";
            this.nodePlacementInstanceIdColumnHeader.Width = 120;
            // 
            // nodePlacementInstanceNameColumnHeader
            // 
            this.nodePlacementInstanceNameColumnHeader.Text = "Instance name";
            this.nodePlacementInstanceNameColumnHeader.Width = 130;
            // 
            // nodePlacementZoneColumnHeader
            // 
            this.nodePlacementZoneColumnHeader.Text = "Zone";
            this.nodePlacementZoneColumnHeader.Width = 80;
            // 
            // nodePlacementProjectIdColumnHeader
            // 
            this.nodePlacementProjectIdColumnHeader.Text = "Project Id";
            this.nodePlacementProjectIdColumnHeader.Width = 120;
            // 
            // nodePlacementFromColumnHeader
            // 
            this.nodePlacementFromColumnHeader.Text = "From (UTC)";
            this.nodePlacementFromColumnHeader.Width = 120;
            // 
            // nodePlacementToColumnHeader
            // 
            this.nodePlacementToColumnHeader.Text = "To (UTC)";
            this.nodePlacementToColumnHeader.Width = 283;
            // 
            // nodesHeadline
            // 
            this.nodesHeadline.AutoSize = true;
            this.nodesHeadline.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodesHeadline.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.nodesHeadline.Location = new System.Drawing.Point(7, 10);
            this.nodesHeadline.Name = "nodesHeadline";
            this.nodesHeadline.Size = new System.Drawing.Size(240, 17);
            this.nodesHeadline.TabIndex = 2;
            this.nodesHeadline.Text = "Nodes with scheduled instances";
            // 
            // nodesChart
            // 
            this.nodesChart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodesChart.BackColor = System.Drawing.SystemColors.Control;
            chartArea2.AxisX.MajorGrid.Enabled = false;
            chartArea2.AxisX.ScaleView.Zoomable = false;
            chartArea2.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.SystemColors.ControlDarkDark;
            chartArea2.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea2.BackColor = System.Drawing.SystemColors.Control;
            chartArea2.BorderColor = System.Drawing.Color.DimGray;
            chartArea2.CursorX.IsUserSelectionEnabled = true;
            chartArea2.Name = "mainArea";
            chartArea2.Position.Auto = false;
            chartArea2.Position.Height = 94F;
            chartArea2.Position.Width = 100F;
            chartArea2.Position.Y = 3F;
            this.nodesChart.ChartAreas.Add(chartArea2);
            this.nodesChart.Location = new System.Drawing.Point(6, 30);
            this.nodesChart.Name = "nodesChart";
            this.nodesChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Grayscale;
            series2.ChartArea = "mainArea";
            series2.Name = "Series1";
            this.nodesChart.Series.Add(series2);
            this.nodesChart.Size = new System.Drawing.Size(865, 300);
            this.nodesChart.TabIndex = 1;
            this.nodesChart.GetToolTipText += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ToolTipEventArgs>(this.chart_GetToolTipText);
            this.nodesChart.SelectionRangeChanged += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.CursorEventArgs>(this.nodesChart_SelectionRangeChanged);
            // 
            // licensesTab
            // 
            this.licensesTab.Controls.Add(this.nodeTypeInfoLabel);
            this.licensesTab.Controls.Add(this.infoIcon);
            this.licensesTab.Controls.Add(this.noLicenseDataLabel);
            this.licensesTab.Controls.Add(this.licenseChart);
            this.licensesTab.Controls.Add(this.byolHeadline);
            this.licensesTab.Location = new System.Drawing.Point(140, 4);
            this.licensesTab.Name = "licensesTab";
            this.licensesTab.Size = new System.Drawing.Size(656, 767);
            this.licensesTab.TabIndex = 2;
            this.licensesTab.Text = "Licenses";
            this.licensesTab.UseVisualStyleBackColor = true;
            // 
            // nodeTypeInfoLabel
            // 
            this.nodeTypeInfoLabel.AutoSize = true;
            this.nodeTypeInfoLabel.Location = new System.Drawing.Point(56, 339);
            this.nodeTypeInfoLabel.Name = "nodeTypeInfoLabel";
            this.nodeTypeInfoLabel.Size = new System.Drawing.Size(33, 13);
            this.nodeTypeInfoLabel.TabIndex = 9;
            this.nodeTypeInfoLabel.Text = "Note:";
            // 
            // infoIcon
            // 
            this.infoIcon.Image = ((System.Drawing.Image)(resources.GetObject("infoIcon.Image")));
            this.infoIcon.Location = new System.Drawing.Point(34, 336);
            this.infoIcon.Name = "infoIcon";
            this.infoIcon.Size = new System.Drawing.Size(16, 16);
            this.infoIcon.TabIndex = 8;
            this.infoIcon.TabStop = false;
            // 
            // noLicenseDataLabel
            // 
            this.noLicenseDataLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.noLicenseDataLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.noLicenseDataLabel.Location = new System.Drawing.Point(330, 160);
            this.noLicenseDataLabel.Name = "noLicenseDataLabel";
            this.noLicenseDataLabel.Size = new System.Drawing.Size(0, 13);
            this.noLicenseDataLabel.TabIndex = 7;
            this.noLicenseDataLabel.Text = "No Windows BYOL instances used";
            this.noLicenseDataLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // licenseChart
            // 
            this.licenseChart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.licenseChart.BackColor = System.Drawing.SystemColors.Control;
            chartArea3.AxisX.MajorGrid.Enabled = false;
            chartArea3.AxisX.ScaleView.Zoomable = false;
            chartArea3.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea3.AxisY.MajorGrid.LineColor = System.Drawing.SystemColors.ControlDarkDark;
            chartArea3.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea3.BackColor = System.Drawing.SystemColors.Control;
            chartArea3.BorderColor = System.Drawing.Color.DimGray;
            chartArea3.CursorX.IsUserSelectionEnabled = true;
            chartArea3.Name = "mainArea";
            chartArea3.Position.Auto = false;
            chartArea3.Position.Height = 94F;
            chartArea3.Position.Width = 100F;
            chartArea3.Position.Y = 3F;
            this.licenseChart.ChartAreas.Add(chartArea3);
            this.licenseChart.Location = new System.Drawing.Point(6, 30);
            this.licenseChart.Name = "licenseChart";
            this.licenseChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Grayscale;
            series3.ChartArea = "mainArea";
            series3.Name = "Series1";
            this.licenseChart.Series.Add(series3);
            this.licenseChart.Size = new System.Drawing.Size(642, 300);
            this.licenseChart.TabIndex = 6;
            this.licenseChart.GetToolTipText += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ToolTipEventArgs>(this.chart_GetToolTipText);
            // 
            // byolHeadline
            // 
            this.byolHeadline.AutoSize = true;
            this.byolHeadline.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.byolHeadline.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.byolHeadline.Location = new System.Drawing.Point(7, 10);
            this.byolHeadline.Name = "byolHeadline";
            this.byolHeadline.Size = new System.Drawing.Size(243, 17);
            this.byolHeadline.TabIndex = 2;
            this.byolHeadline.Text = "Scheduled Windows BYOL cores";
            // 
            // ReportPaneView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(800, 800);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.toolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ReportPaneView";
            this.Text = "ReportView";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.tabs.ResumeLayout(false);
            this.instancesTab.ResumeLayout(false);
            this.instancesTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.instancesChart)).EndInit();
            this.nodesTab.ResumeLayout(false);
            this.nodesTab.PerformLayout();
            this.nodesSplitContainer.Panel1.ResumeLayout(false);
            this.nodesSplitContainer.Panel2.ResumeLayout(false);
            this.nodesSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodesSplitContainer)).EndInit();
            this.nodesSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nodesChart)).EndInit();
            this.licensesTab.ResumeLayout(false);
            this.licensesTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.infoIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.licenseChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private WeifenLuo.WinFormsUI.Docking.VS2015LightTheme theme;
        private System.Windows.Forms.ToolStripDropDownButton includeTenancyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeSoleTenantInstancesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeFleetInstancesMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton includeOsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeWindowsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeLinuxMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton includeLicenseMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeByolMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeSplaMenuItem;
        private Application.Controls.FlatVerticalTabControl tabs;
        private System.Windows.Forms.TabPage instancesTab;
        private System.Windows.Forms.TabPage nodesTab;
        private System.Windows.Forms.TabPage licensesTab;
        private System.Windows.Forms.DataVisualization.Charting.Chart instancesChart;
        private System.Windows.Forms.Label instancesHeader;
        private InstancesListView instancesList;
        private System.Windows.Forms.ColumnHeader instanceIdColumnHeader;
        private System.Windows.Forms.ColumnHeader instanceNameColumnHeader;
        private System.Windows.Forms.ColumnHeader instanceZoneColumnHeader;
        private System.Windows.Forms.ColumnHeader instanceProjectIdColumnHeader;
        private System.Windows.Forms.ColumnHeader placedFromColumnHeader;
        private System.Windows.Forms.ColumnHeader placedUntilColumnHeader;
        private System.Windows.Forms.ToolStripMenuItem includeUnknownOsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeUnknownLicenseMenuItem;
        private System.Windows.Forms.Label nodesHeadline;
        private System.Windows.Forms.DataVisualization.Charting.Chart nodesChart;
        private System.Windows.Forms.SplitContainer nodesSplitContainer;
        private NodesListView nodesList;
        private System.Windows.Forms.ColumnHeader serverIdColumnHeader;
        private System.Windows.Forms.ColumnHeader nodeZoneColumnHeader;
        private System.Windows.Forms.ColumnHeader nodeProjectIdColumnHeader;
        private System.Windows.Forms.ColumnHeader firstUseColumnHeader;
        private System.Windows.Forms.ColumnHeader lastUseColumnHeader;
        private System.Windows.Forms.ColumnHeader peakInstancesColumnHeader;
        private System.Windows.Forms.ColumnHeader daysUsedColumnHeader;
        private NodesPlacementsListView nodePlacementsList;
        private System.Windows.Forms.ColumnHeader nodePlacementInstanceIdColumnHeader;
        private System.Windows.Forms.ColumnHeader nodePlacementInstanceNameColumnHeader;
        private System.Windows.Forms.ColumnHeader nodePlacementZoneColumnHeader;
        private System.Windows.Forms.ColumnHeader nodePlacementProjectIdColumnHeader;
        private System.Windows.Forms.ColumnHeader nodePlacementFromColumnHeader;
        private System.Windows.Forms.ColumnHeader nodePlacementToColumnHeader;
        private System.Windows.Forms.Label nodePlacementsHeadline;
        private System.Windows.Forms.Label noInstancesDataLabel;
        private System.Windows.Forms.Label noNodesDataLabel;
        private System.Windows.Forms.Label byolHeadline;
        private System.Windows.Forms.Label noLicenseDataLabel;
        private System.Windows.Forms.DataVisualization.Charting.Chart licenseChart;
        private System.Windows.Forms.Label nodeTypeInfoLabel;
        private System.Windows.Forms.PictureBox infoIcon;
        private System.Windows.Forms.ColumnHeader osColumnHeader;
        private System.Windows.Forms.ColumnHeader licenseColumnHeader;
    }
}