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

namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
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
            this.vmToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.connectToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openSettingsButton = new System.Windows.Forms.ToolStripButton();
            this.generateCredentialsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.showSerialLogToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.vsToolStripExtender = new WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender(this.components);
            this.vs2015LightTheme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.treeView = new System.Windows.Forms.TreeView();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.generateCredentialsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showSerialLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshAllProjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unloadProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.iapSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.configureIapAccessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cloudConsoleSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.openInCloudConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openlogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.vmToolStripSeparator,
            this.connectToolStripButton,
            this.openSettingsButton,
            this.generateCredentialsToolStripButton,
            this.showSerialLogToolStripButton});
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
            // vmToolStripSeparator
            // 
            this.vmToolStripSeparator.Name = "vmToolStripSeparator";
            this.vmToolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // connectToolStripButton
            // 
            this.connectToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.connectToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("connectToolStripButton.Image")));
            this.connectToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.connectToolStripButton.Name = "connectToolStripButton";
            this.connectToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.connectToolStripButton.Text = "Connect to remote desktop";
            this.connectToolStripButton.Click += new System.EventHandler(this.connectToolStripButton_Click);
            // 
            // openSettingsButton
            // 
            this.openSettingsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openSettingsButton.Image = ((System.Drawing.Image)(resources.GetObject("openSettingsButton.Image")));
            this.openSettingsButton.ImageTransparentColor = System.Drawing.Color.White;
            this.openSettingsButton.Name = "openSettingsButton";
            this.openSettingsButton.Size = new System.Drawing.Size(23, 22);
            this.openSettingsButton.Text = "Settings";
            this.openSettingsButton.Click += new System.EventHandler(this.openSettingsButton_Click);
            // 
            // generateCredentialsToolStripButton
            // 
            this.generateCredentialsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.generateCredentialsToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("generateCredentialsToolStripButton.Image")));
            this.generateCredentialsToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.generateCredentialsToolStripButton.Name = "generateCredentialsToolStripButton";
            this.generateCredentialsToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.generateCredentialsToolStripButton.Text = "Generate Windows logon credentials";
            this.generateCredentialsToolStripButton.Click += new System.EventHandler(this.generateCredentialsToolStripButton_Click);
            // 
            // showSerialLogToolStripButton
            // 
            this.showSerialLogToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.showSerialLogToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("showSerialLogToolStripButton.Image")));
            this.showSerialLogToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.showSerialLogToolStripButton.Name = "showSerialLogToolStripButton";
            this.showSerialLogToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.showSerialLogToolStripButton.Text = "Show serial &log";
            this.showSerialLogToolStripButton.Click += new System.EventHandler(this.showSerialLogToolStripButton_Click);
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
            this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
            this.treeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseClick);
            this.treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectToolStripMenuItem,
            this.generateCredentialsToolStripMenuItem,
            this.showSerialLogToolStripMenuItem,
            this.refreshToolStripMenuItem,
            this.refreshAllProjectsToolStripMenuItem,
            this.unloadProjectToolStripMenuItem,
            this.propertiesToolStripMenuItem,
            this.iapSeparatorToolStripMenuItem,
            this.configureIapAccessToolStripMenuItem,
            this.cloudConsoleSeparatorToolStripMenuItem,
            this.openInCloudConsoleToolStripMenuItem,
            this.openlogsToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(277, 236);
            // 
            // connectToolStripMenuItem
            // 
            this.connectToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Connect_16;
            this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.connectToolStripMenuItem.Text = "&Connect";
            this.connectToolStripMenuItem.Click += new System.EventHandler(this.connectToolStripMenuItem_Click);
            // 
            // generateCredentialsToolStripMenuItem
            // 
            this.generateCredentialsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Password_16;
            this.generateCredentialsToolStripMenuItem.Name = "generateCredentialsToolStripMenuItem";
            this.generateCredentialsToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.generateCredentialsToolStripMenuItem.Text = "Generate &Windows logon credentials...";
            this.generateCredentialsToolStripMenuItem.Click += new System.EventHandler(this.generateCredentialsToolStripMenuItem_Click);
            // 
            // showSerialLogToolStripMenuItem
            // 
            this.showSerialLogToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Log_16;
            this.showSerialLogToolStripMenuItem.Name = "showSerialLogToolStripMenuItem";
            this.showSerialLogToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.showSerialLogToolStripMenuItem.Text = "Show serial &log";
            this.showSerialLogToolStripMenuItem.Click += new System.EventHandler(this.showSerialLogToolStripMenuItem_Click);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Refresh_16;
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.refreshToolStripMenuItem.Text = "&Refresh project";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // refreshAllProjectsToolStripMenuItem
            // 
            this.refreshAllProjectsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Refresh_161;
            this.refreshAllProjectsToolStripMenuItem.Name = "refreshAllProjectsToolStripMenuItem";
            this.refreshAllProjectsToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.refreshAllProjectsToolStripMenuItem.Text = "Refresh &all projects";
            this.refreshAllProjectsToolStripMenuItem.Click += new System.EventHandler(this.refreshAllProjectsToolStripMenuItem_Click);
            // 
            // unloadProjectToolStripMenuItem
            // 
            this.unloadProjectToolStripMenuItem.Name = "unloadProjectToolStripMenuItem";
            this.unloadProjectToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.unloadProjectToolStripMenuItem.Text = "&Unload project";
            this.unloadProjectToolStripMenuItem.Click += new System.EventHandler(this.unloadProjectToolStripMenuItem_Click);
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Settings_16;
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.propertiesToolStripMenuItem.Text = "&Settings";
            this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.propertiesToolStripMenuItem_Click);
            // 
            // iapSeparatorToolStripMenuItem
            // 
            this.iapSeparatorToolStripMenuItem.Name = "iapSeparatorToolStripMenuItem";
            this.iapSeparatorToolStripMenuItem.Size = new System.Drawing.Size(273, 6);
            // 
            // configureIapAccessToolStripMenuItem
            // 
            this.configureIapAccessToolStripMenuItem.Name = "configureIapAccessToolStripMenuItem";
            this.configureIapAccessToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.configureIapAccessToolStripMenuItem.Text = "Configure IAP a&ccess...";
            this.configureIapAccessToolStripMenuItem.Click += new System.EventHandler(this.configureIapAccessToolStripMenuItem_Click);
            // 
            // cloudConsoleSeparatorToolStripMenuItem
            // 
            this.cloudConsoleSeparatorToolStripMenuItem.Name = "cloudConsoleSeparatorToolStripMenuItem";
            this.cloudConsoleSeparatorToolStripMenuItem.Size = new System.Drawing.Size(273, 6);
            // 
            // openInCloudConsoleToolStripMenuItem
            // 
            this.openInCloudConsoleToolStripMenuItem.Name = "openInCloudConsoleToolStripMenuItem";
            this.openInCloudConsoleToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.openInCloudConsoleToolStripMenuItem.Text = "Open in Cloud Consol&e...";
            this.openInCloudConsoleToolStripMenuItem.Click += new System.EventHandler(this.openInCloudConsoleToolStripMenuItem_Click);
            // 
            // openlogsToolStripMenuItem
            // 
            this.openlogsToolStripMenuItem.Name = "openlogsToolStripMenuItem";
            this.openlogsToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.openlogsToolStripMenuItem.Text = "Open &logs...";
            this.openlogsToolStripMenuItem.Click += new System.EventHandler(this.openlogsToolStripMenuItem_Click);
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
            this.imageList.Images.SetKeyName(6, "Vm.ico");
            this.imageList.Images.SetKeyName(7, "VmBlue.ico");
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
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshAllProjectsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unloadProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton openSettingsButton;
        private System.Windows.Forms.ToolStripMenuItem openInCloudConsoleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openlogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator cloudConsoleSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator iapSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureIapAccessToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem generateCredentialsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator vmToolStripSeparator;
        private System.Windows.Forms.ToolStripButton generateCredentialsToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton connectToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem showSerialLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton showSerialLogToolStripButton;
    }
}