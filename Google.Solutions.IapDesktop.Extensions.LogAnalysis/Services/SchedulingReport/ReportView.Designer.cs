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

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    partial class ReportView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportView));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.includeTenancyMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeSoleTenantInstancesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeFleetInstancesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeOsMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeWindowsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeLinuxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeLicenseMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeByolMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeSplaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.theme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.tabs = new Google.Solutions.IapDesktop.Application.Services.Windows.FlatVerticalTabControl();
            this.instancesTab = new System.Windows.Forms.TabPage();
            this.nodesTab = new System.Windows.Forms.TabPage();
            this.licensesTab = new System.Windows.Forms.TabPage();
            this.toolStrip.SuspendLayout();
            this.tabs.SuspendLayout();
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
            this.includeTenancyMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.includeTenancyMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeSoleTenantInstancesMenuItem,
            this.includeFleetInstancesMenuItem});
            this.includeTenancyMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("includeTenancyMenuItem.Image")));
            this.includeTenancyMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.includeTenancyMenuItem.Name = "includeTenancyMenuItem";
            this.includeTenancyMenuItem.Size = new System.Drawing.Size(63, 22);
            this.includeTenancyMenuItem.Text = "Tenancy";
            // 
            // includeSoleTenantInstancesMenuItem
            // 
            this.includeSoleTenantInstancesMenuItem.Name = "includeSoleTenantInstancesMenuItem";
            this.includeSoleTenantInstancesMenuItem.Size = new System.Drawing.Size(208, 22);
            this.includeSoleTenantInstancesMenuItem.Text = "Sole-tenant VM instances";
            // 
            // includeFleetInstancesMenuItem
            // 
            this.includeFleetInstancesMenuItem.Name = "includeFleetInstancesMenuItem";
            this.includeFleetInstancesMenuItem.Size = new System.Drawing.Size(208, 22);
            this.includeFleetInstancesMenuItem.Text = "Fleet VM instances";
            // 
            // includeOsMenuItem
            // 
            this.includeOsMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.includeOsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeWindowsMenuItem,
            this.includeLinuxMenuItem});
            this.includeOsMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("includeOsMenuItem.Image")));
            this.includeOsMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.includeOsMenuItem.Name = "includeOsMenuItem";
            this.includeOsMenuItem.Size = new System.Drawing.Size(35, 22);
            this.includeOsMenuItem.Text = "OS";
            // 
            // includeWindowsMenuItem
            // 
            this.includeWindowsMenuItem.Name = "includeWindowsMenuItem";
            this.includeWindowsMenuItem.Size = new System.Drawing.Size(123, 22);
            this.includeWindowsMenuItem.Text = "Windows";
            // 
            // includeLinuxMenuItem
            // 
            this.includeLinuxMenuItem.Name = "includeLinuxMenuItem";
            this.includeLinuxMenuItem.Size = new System.Drawing.Size(123, 22);
            this.includeLinuxMenuItem.Text = "Linux";
            // 
            // includeLicenseMenuItem
            // 
            this.includeLicenseMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.includeLicenseMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeByolMenuItem,
            this.includeSplaMenuItem});
            this.includeLicenseMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("includeLicenseMenuItem.Image")));
            this.includeLicenseMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.includeLicenseMenuItem.Name = "includeLicenseMenuItem";
            this.includeLicenseMenuItem.Size = new System.Drawing.Size(59, 22);
            this.includeLicenseMenuItem.Text = "License";
            // 
            // includeByolMenuItem
            // 
            this.includeByolMenuItem.Name = "includeByolMenuItem";
            this.includeByolMenuItem.Size = new System.Drawing.Size(199, 22);
            this.includeByolMenuItem.Text = "Bring-your-own (BYOL)";
            // 
            // includeSplaMenuItem
            // 
            this.includeSplaMenuItem.Name = "includeSplaMenuItem";
            this.includeSplaMenuItem.Size = new System.Drawing.Size(199, 22);
            this.includeSplaMenuItem.Text = "Pay-as-you-go (SPLA)";
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
            this.tabs.Size = new System.Drawing.Size(800, 425);
            this.tabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabs.TabIndex = 1;
            // 
            // instancesTab
            // 
            this.instancesTab.Location = new System.Drawing.Point(140, 4);
            this.instancesTab.Name = "instancesTab";
            this.instancesTab.Padding = new System.Windows.Forms.Padding(3);
            this.instancesTab.Size = new System.Drawing.Size(656, 417);
            this.instancesTab.TabIndex = 0;
            this.instancesTab.Text = "Instances";
            this.instancesTab.UseVisualStyleBackColor = true;
            // 
            // nodesTab
            // 
            this.nodesTab.Location = new System.Drawing.Point(140, 4);
            this.nodesTab.Name = "nodesTab";
            this.nodesTab.Padding = new System.Windows.Forms.Padding(3);
            this.nodesTab.Size = new System.Drawing.Size(656, 417);
            this.nodesTab.TabIndex = 1;
            this.nodesTab.Text = "Sole-tenant nodes";
            this.nodesTab.UseVisualStyleBackColor = true;
            // 
            // licensesTab
            // 
            this.licensesTab.Location = new System.Drawing.Point(140, 4);
            this.licensesTab.Name = "licensesTab";
            this.licensesTab.Size = new System.Drawing.Size(656, 417);
            this.licensesTab.TabIndex = 2;
            this.licensesTab.Text = "Licenses";
            this.licensesTab.UseVisualStyleBackColor = true;
            // 
            // ReportView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.toolStrip);
            this.Name = "ReportView";
            this.Text = "ReportView";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.tabs.ResumeLayout(false);
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
        private Application.Services.Windows.FlatVerticalTabControl tabs;
        private System.Windows.Forms.TabPage instancesTab;
        private System.Windows.Forms.TabPage nodesTab;
        private System.Windows.Forms.TabPage licensesTab;
    }
}