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

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    partial class ProjectExplorerWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectExplorerWindow));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.addButton = new System.Windows.Forms.ToolStripButton();
            this.osDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.windowsInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.linuxInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vmToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.vsToolStripExtender = new WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender(this.components);
            this.vs2015LightTheme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.treeView = new BindableTreeView<ProjectExplorerViewModel.ViewModelNode>();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshAllProjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unloadProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.iapSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.openInCloudConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureIapAccessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cloudConsoleSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.toolStrip.SuspendLayout();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshButton,
            this.addButton,
            this.osDropDownButton,
            this.vmToolStripSeparator});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // refreshButton
            // 
            this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshButton.Image = ((System.Drawing.Image)(resources.GetObject("refreshButton.Image")));
            this.refreshButton.ImageTransparentColor = System.Drawing.Color.White;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(23, 22);
            this.refreshButton.Text = "Refresh";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // addButton
            // 
            this.addButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.addButton.Image = ((System.Drawing.Image)(resources.GetObject("addButton.Image")));
            this.addButton.ImageTransparentColor = System.Drawing.Color.White;
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(23, 22);
            this.addButton.Text = "Add project";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // osDropDownButton
            // 
            this.osDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.osDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.windowsInstancesToolStripMenuItem,
            this.linuxInstancesToolStripMenuItem});
            this.osDropDownButton.Image = ((System.Drawing.Image)(resources.GetObject("osDropDownButton.Image")));
            this.osDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.osDropDownButton.Name = "osDropDownButton";
            this.osDropDownButton.Size = new System.Drawing.Size(29, 22);
            this.osDropDownButton.Text = "Filter VM instances by operating system";
            // 
            // windowsInstancesToolStripMenuItem
            // 
            this.windowsInstancesToolStripMenuItem.CheckOnClick = true;
            this.windowsInstancesToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Computer_16;
            this.windowsInstancesToolStripMenuItem.Name = "windowsInstancesToolStripMenuItem";
            this.windowsInstancesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.windowsInstancesToolStripMenuItem.Text = "Windows instances";
            // 
            // linuxInstancesToolStripMenuItem
            // 
            this.linuxInstancesToolStripMenuItem.CheckOnClick = true;
            this.linuxInstancesToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.ComputerTerminal_161;
            this.linuxInstancesToolStripMenuItem.Name = "linuxInstancesToolStripMenuItem";
            this.linuxInstancesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.linuxInstancesToolStripMenuItem.Text = "Linux instances";
            // 
            // vmToolStripSeparator
            // 
            this.vmToolStripSeparator.Name = "vmToolStripSeparator";
            this.vmToolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // vsToolStripExtender
            // 
            this.vsToolStripExtender.DefaultRenderer = null;
            // 
            // treeView
            // 
            this.treeView.ContextMenuStrip = this.contextMenu;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.HideSelection = false;
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageList;
            this.treeView.Location = new System.Drawing.Point(0, 25);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.Size = new System.Drawing.Size(800, 425);
            this.treeView.TabIndex = 1;
            this.treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.refreshAllProjectsToolStripMenuItem,
            this.unloadProjectToolStripMenuItem,
            this.iapSeparatorToolStripMenuItem,
            this.openInCloudConsoleToolStripMenuItem,
            this.configureIapAccessToolStripMenuItem,
            this.cloudConsoleSeparatorToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(198, 126);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Refresh_16;
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.refreshToolStripMenuItem.Text = "&Refresh project";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // refreshAllProjectsToolStripMenuItem
            // 
            this.refreshAllProjectsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Refresh_161;
            this.refreshAllProjectsToolStripMenuItem.Name = "refreshAllProjectsToolStripMenuItem";
            this.refreshAllProjectsToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.refreshAllProjectsToolStripMenuItem.Text = "Refresh &all projects";
            this.refreshAllProjectsToolStripMenuItem.Click += new System.EventHandler(this.refreshAllProjectsToolStripMenuItem_Click);
            // 
            // unloadProjectToolStripMenuItem
            // 
            this.unloadProjectToolStripMenuItem.Name = "unloadProjectToolStripMenuItem";
            this.unloadProjectToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.unloadProjectToolStripMenuItem.Text = "&Unload project";
            this.unloadProjectToolStripMenuItem.Click += new System.EventHandler(this.unloadProjectToolStripMenuItem_Click);
            // 
            // iapSeparatorToolStripMenuItem
            // 
            this.iapSeparatorToolStripMenuItem.Name = "iapSeparatorToolStripMenuItem";
            this.iapSeparatorToolStripMenuItem.Size = new System.Drawing.Size(194, 6);
            // 
            // openInCloudConsoleToolStripMenuItem
            // 
            this.openInCloudConsoleToolStripMenuItem.Name = "openInCloudConsoleToolStripMenuItem";
            this.openInCloudConsoleToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.openInCloudConsoleToolStripMenuItem.Text = "Open in Cloud Consol&e";
            this.openInCloudConsoleToolStripMenuItem.Click += new System.EventHandler(this.openInCloudConsoleToolStripMenuItem_Click);
            // 
            // configureIapAccessToolStripMenuItem
            // 
            this.configureIapAccessToolStripMenuItem.Name = "configureIapAccessToolStripMenuItem";
            this.configureIapAccessToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.configureIapAccessToolStripMenuItem.Text = "Configure IAP a&ccess";
            this.configureIapAccessToolStripMenuItem.Click += new System.EventHandler(this.configureIapAccessToolStripMenuItem_Click);
            // 
            // cloudConsoleSeparatorToolStripMenuItem
            // 
            this.cloudConsoleSeparatorToolStripMenuItem.Name = "cloudConsoleSeparatorToolStripMenuItem";
            this.cloudConsoleSeparatorToolStripMenuItem.Size = new System.Drawing.Size(194, 6);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "Cloud.ico");
            this.imageList.Images.SetKeyName(1, "Project.ico");
            this.imageList.Images.SetKeyName(2, "Region.ico");
            this.imageList.Images.SetKeyName(3, "Zone.ico");
            this.imageList.Images.SetKeyName(4, "Computer_16.png");
            this.imageList.Images.SetKeyName(5, "ComputerBlue_16.png");
            this.imageList.Images.SetKeyName(6, "ComputerStopped_16.png");
            this.imageList.Images.SetKeyName(7, "ComputerTerminal_16.png");
            this.imageList.Images.SetKeyName(8, "ComputerTerminalBlue_16.png");
            // 
            // ProjectExplorerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.toolStrip);
            this.Name = "ProjectExplorerWindow";
            this.Text = "Project Explorer";
            this.Shown += new System.EventHandler(this.ProjectExplorerWindow_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ProjectExplorerWindow_KeyDown);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender vsToolStripExtender;
        private WeifenLuo.WinFormsUI.Docking.VS2015LightTheme vs2015LightTheme;
        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.ToolStripButton addButton;
        private BindableTreeView<ProjectExplorerViewModel.ViewModelNode> treeView;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshAllProjectsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unloadProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openInCloudConsoleToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator cloudConsoleSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator iapSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureIapAccessToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator vmToolStripSeparator;
        private System.Windows.Forms.ToolStripDropDownButton osDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem windowsInstancesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem linuxInstancesToolStripMenuItem;
    }
}