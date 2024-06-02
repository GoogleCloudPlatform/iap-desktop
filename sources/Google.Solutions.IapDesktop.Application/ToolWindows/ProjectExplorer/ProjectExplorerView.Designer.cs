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

using Google.Solutions.Mvvm.Controls;

namespace Google.Solutions.IapDesktop.Application.Windows.ProjectExplorer
{
    partial class ProjectExplorerView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectExplorerView));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.addButton = new System.Windows.Forms.ToolStripButton();
            this.osDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.windowsInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.linuxInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vmToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.treeView = new Google.Solutions.IapDesktop.Application.Windows.ProjectExplorer.ProjectExplorerView.NodeTreeView();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.progressBar = new Google.Solutions.Mvvm.Controls.LinearProgressBar();
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
            this.refreshButton.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Refresh_16;
            this.refreshButton.ImageTransparentColor = System.Drawing.Color.White;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(23, 22);
            this.refreshButton.Text = "Refresh";
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
            this.windowsInstancesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("windowsInstancesToolStripMenuItem.Image")));
            this.windowsInstancesToolStripMenuItem.Name = "windowsInstancesToolStripMenuItem";
            this.windowsInstancesToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.windowsInstancesToolStripMenuItem.Text = "Windows instances";
            // 
            // linuxInstancesToolStripMenuItem
            // 
            this.linuxInstancesToolStripMenuItem.CheckOnClick = true;
            this.linuxInstancesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("linuxInstancesToolStripMenuItem.Image")));
            this.linuxInstancesToolStripMenuItem.Name = "linuxInstancesToolStripMenuItem";
            this.linuxInstancesToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.linuxInstancesToolStripMenuItem.Text = "Linux instances";
            // 
            // vmToolStripSeparator
            // 
            this.vmToolStripSeparator.Name = "vmToolStripSeparator";
            this.vmToolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // treeView
            // 
            this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.treeView.ContextMenuStrip = this.contextMenu;
            this.treeView.HideSelection = false;
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageList;
            this.treeView.Location = new System.Drawing.Point(0, 50);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.Size = new System.Drawing.Size(800, 400);
            this.treeView.TabIndex = 0;
            this.treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);
            // 
            // contextMenu
            // 
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "Cloud_16.png");
            this.imageList.Images.SetKeyName(1, "Project_16.png");
            this.imageList.Images.SetKeyName(2, "Zone_16.png");
            this.imageList.Images.SetKeyName(3, "Zone_16.png");
            this.imageList.Images.SetKeyName(4, "Computer_16.png");
            this.imageList.Images.SetKeyName(5, "ComputerBlue_16.png");
            this.imageList.Images.SetKeyName(6, "ComputerStopped_16.png");
            this.imageList.Images.SetKeyName(7, "ComputerTerminal_16.png");
            this.imageList.Images.SetKeyName(8, "ComputerTerminalBlue_16.png");
            // 
            // searchTextBox
            // 
            this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.searchTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.searchTextBox.Location = new System.Drawing.Point(0, 25);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(800, 20);
            this.searchTextBox.TabIndex = 1;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Indeterminate = true;
            this.progressBar.Location = new System.Drawing.Point(0, 45);
            this.progressBar.Margin = new System.Windows.Forms.Padding(0);
            this.progressBar.Maximum = 100;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(800, 5);
            this.progressBar.Speed = 3;
            this.progressBar.TabIndex = 3;
            this.progressBar.Value = 0;
            // 
            // ProjectExplorerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.toolStrip);
            this.Name = "ProjectExplorerView";
            this.Text = "Project Explorer";
            this.Shown += new System.EventHandler(this.ProjectExplorerWindow_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ProjectExplorerWindow_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.ToolStripButton addButton;
        private NodeTreeView treeView;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripSeparator vmToolStripSeparator;
        private System.Windows.Forms.ToolStripDropDownButton osDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem windowsInstancesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem linuxInstancesToolStripMenuItem;
        private System.Windows.Forms.TextBox searchTextBox;
        private LinearProgressBar progressBar;
    }
}