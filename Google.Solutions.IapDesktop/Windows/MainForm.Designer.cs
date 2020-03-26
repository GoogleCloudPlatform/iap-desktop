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

namespace Google.Solutions.IapDesktop.Windows
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.vs2015LightTheme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.signoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projectExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.activeTunnelsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openIapDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openIapAccessDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vsToolStripExtender = new WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender(this.components);
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.dockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.reportIssueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(984, 24);
            this.mainMenu.TabIndex = 3;
            this.mainMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addProjectToolStripMenuItem,
            this.fileSeparatorToolStripMenuItem,
            this.signoutToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // addProjectToolStripMenuItem
            // 
            this.addProjectToolStripMenuItem.Name = "addProjectToolStripMenuItem";
            this.addProjectToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.addProjectToolStripMenuItem.Text = "&Add Google Cloud project...";
            this.addProjectToolStripMenuItem.Click += new System.EventHandler(this.addProjectToolStripMenuItem_Click);
            // 
            // fileSeparatorToolStripMenuItem
            // 
            this.fileSeparatorToolStripMenuItem.Name = "fileSeparatorToolStripMenuItem";
            this.fileSeparatorToolStripMenuItem.Size = new System.Drawing.Size(218, 6);
            // 
            // signoutToolStripMenuItem
            // 
            this.signoutToolStripMenuItem.Name = "signoutToolStripMenuItem";
            this.signoutToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.signoutToolStripMenuItem.Text = "Sign &out";
            this.signoutToolStripMenuItem.Click += new System.EventHandler(this.signoutToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.projectExplorerToolStripMenuItem,
            this.activeTunnelsToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // projectExplorerToolStripMenuItem
            // 
            this.projectExplorerToolStripMenuItem.Name = "projectExplorerToolStripMenuItem";
            this.projectExplorerToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.L)));
            this.projectExplorerToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.projectExplorerToolStripMenuItem.Text = "&Project Explorer";
            this.projectExplorerToolStripMenuItem.Click += new System.EventHandler(this.projectExplorerToolStripMenuItem_Click);
            // 
            // activeTunnelsToolStripMenuItem
            // 
            this.activeTunnelsToolStripMenuItem.Name = "activeTunnelsToolStripMenuItem";
            this.activeTunnelsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.T)));
            this.activeTunnelsToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.activeTunnelsToolStripMenuItem.Text = "Active IAP &tunnels";
            this.activeTunnelsToolStripMenuItem.Click += new System.EventHandler(this.activeTunnelsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openIapDocsToolStripMenuItem,
            this.openIapAccessDocsToolStripMenuItem,
            this.aboutSeparatorToolStripMenuItem,
            this.reportIssueToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // openIapDocsToolStripMenuItem
            // 
            this.openIapDocsToolStripMenuItem.Name = "openIapDocsToolStripMenuItem";
            this.openIapDocsToolStripMenuItem.Size = new System.Drawing.Size(286, 22);
            this.openIapDocsToolStripMenuItem.Text = "Overview of Cloud &IAP TCP forwarding...";
            this.openIapDocsToolStripMenuItem.Click += new System.EventHandler(this.openIapDocsToolStripMenuItem_Click);
            // 
            // openIapAccessDocsToolStripMenuItem
            // 
            this.openIapAccessDocsToolStripMenuItem.Name = "openIapAccessDocsToolStripMenuItem";
            this.openIapAccessDocsToolStripMenuItem.Size = new System.Drawing.Size(286, 22);
            this.openIapAccessDocsToolStripMenuItem.Text = "&Configuring access to Cloud IAP...";
            this.openIapAccessDocsToolStripMenuItem.Click += new System.EventHandler(this.openIapAccessDocsToolStripMenuItem_Click);
            // 
            // aboutSeparatorToolStripMenuItem
            // 
            this.aboutSeparatorToolStripMenuItem.Name = "aboutSeparatorToolStripMenuItem";
            this.aboutSeparatorToolStripMenuItem.Size = new System.Drawing.Size(283, 6);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(286, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // vsToolStripExtender
            // 
            this.vsToolStripExtender.DefaultRenderer = null;
            // 
            // statusStrip
            // 
            this.statusStrip.Location = new System.Drawing.Point(0, 639);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(984, 22);
            this.statusStrip.TabIndex = 5;
            this.statusStrip.Text = "statusStrip1";
            // 
            // dockPanel
            // 
            this.dockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPanel.DockLeftPortion = 0.15D;
            this.dockPanel.DockRightPortion = 0.15D;
            this.dockPanel.Location = new System.Drawing.Point(0, 24);
            this.dockPanel.Name = "dockPanel";
            this.dockPanel.Size = new System.Drawing.Size(984, 615);
            this.dockPanel.TabIndex = 9;
            // 
            // reportIssueToolStripMenuItem
            // 
            this.reportIssueToolStripMenuItem.Name = "reportIssueToolStripMenuItem";
            this.reportIssueToolStripMenuItem.Size = new System.Drawing.Size(286, 22);
            this.reportIssueToolStripMenuItem.Text = "&Report issue...";
            this.reportIssueToolStripMenuItem.Click += new System.EventHandler(this.reportIssueToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.dockPanel);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.mainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.mainMenu;
            this.MinimumSize = new System.Drawing.Size(1000, 700);
            this.Name = "MainForm";
            this.Text = "IAP Desktop - Identity-Aware Proxy for Remote Desktop";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private WeifenLuo.WinFormsUI.Docking.VS2015LightTheme vs2015LightTheme;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender vsToolStripExtender;
        private System.Windows.Forms.StatusStrip statusStrip;
        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanel;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem projectExplorerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openIapDocsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openIapAccessDocsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator aboutSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem activeTunnelsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reportIssueToolStripMenuItem;
    }
}