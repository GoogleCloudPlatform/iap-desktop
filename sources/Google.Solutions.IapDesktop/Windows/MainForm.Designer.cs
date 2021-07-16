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
            this.fileSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.signoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableloggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.reportIssueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportInternalIssueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vsToolStripExtender = new WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender(this.components);
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.backgroundJobLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.dockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.howtoSeparatorStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.cancelBackgroundJobsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripSignInStateButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripDeviceStateButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.addProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projectExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openIapDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openIapFirewallDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openIapAccessDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.overviewSeparatorStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.openSecureConnectDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.toolsToolStripMenuItem,
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
            // fileSeparatorToolStripMenuItem
            // 
            this.fileSeparatorToolStripMenuItem.Name = "fileSeparatorToolStripMenuItem";
            this.fileSeparatorToolStripMenuItem.Size = new System.Drawing.Size(218, 6);
            // 
            // signoutToolStripMenuItem
            // 
            this.signoutToolStripMenuItem.Name = "signoutToolStripMenuItem";
            this.signoutToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.signoutToolStripMenuItem.Text = "Sign &out and exit";
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
            this.projectExplorerToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableloggingToolStripMenuItem,
            this.optionsToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // enableloggingToolStripMenuItem
            // 
            this.enableloggingToolStripMenuItem.Name = "enableloggingToolStripMenuItem";
            this.enableloggingToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.enableloggingToolStripMenuItem.Text = "Enable &logging";
            this.enableloggingToolStripMenuItem.Click += new System.EventHandler(this.enableloggingToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.optionsToolStripMenuItem.Text = "&Options...";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewHelpToolStripMenuItem,
            this.overviewSeparatorStripMenuItem,
            this.openIapDocsToolStripMenuItem,
            this.openSecureConnectDocsToolStripMenuItem,
            this.howtoSeparatorStripMenuItem,
            this.openIapFirewallDocsToolStripMenuItem,
            this.openIapAccessDocsToolStripMenuItem,
            this.aboutSeparatorToolStripMenuItem,
            this.reportIssueToolStripMenuItem,
            this.reportInternalIssueToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutSeparatorToolStripMenuItem
            // 
            this.aboutSeparatorToolStripMenuItem.Name = "aboutSeparatorToolStripMenuItem";
            this.aboutSeparatorToolStripMenuItem.Size = new System.Drawing.Size(263, 6);
            // 
            // reportIssueToolStripMenuItem
            // 
            this.reportIssueToolStripMenuItem.Name = "reportIssueToolStripMenuItem";
            this.reportIssueToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.reportIssueToolStripMenuItem.Text = "&Report issue...";
            this.reportIssueToolStripMenuItem.Click += new System.EventHandler(this.reportGithubIssueToolStripMenuItem_Click);
            // 
            // reportInternalIssueToolStripMenuItem
            // 
            this.reportInternalIssueToolStripMenuItem.Name = "reportInternalIssueToolStripMenuItem";
            this.reportInternalIssueToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.reportInternalIssueToolStripMenuItem.Text = "Report &issue (Google internal)...";
            this.reportInternalIssueToolStripMenuItem.Click += new System.EventHandler(this.reportInternalIssueToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // vsToolStripExtender
            // 
            this.vsToolStripExtender.DefaultRenderer = null;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.backgroundJobLabel,
            this.cancelBackgroundJobsButton,
            this.toolStripStatus,
            this.toolStripSignInStateButton,
            this.toolStripDeviceStateButton});
            this.statusStrip.Location = new System.Drawing.Point(0, 639);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(984, 22);
            this.statusStrip.TabIndex = 5;
            this.statusStrip.Text = "statusStrip";
            // 
            // backgroundJobLabel
            // 
            this.backgroundJobLabel.Name = "backgroundJobLabel";
            this.backgroundJobLabel.Size = new System.Drawing.Size(16, 17);
            this.backgroundJobLabel.Text = "...";
            // 
            // toolStripStatus
            // 
            this.toolStripStatus.Name = "toolStripStatus";
            this.toolStripStatus.Size = new System.Drawing.Size(734, 17);
            this.toolStripStatus.Spring = true;
            this.toolStripStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            // howtoSeparatorStripMenuItem
            // 
            this.howtoSeparatorStripMenuItem.Name = "howtoSeparatorStripMenuItem";
            this.howtoSeparatorStripMenuItem.Size = new System.Drawing.Size(263, 6);
            // 
            // cancelBackgroundJobsButton
            // 
            this.cancelBackgroundJobsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.cancelBackgroundJobsButton.Image = ((System.Drawing.Image)(resources.GetObject("cancelBackgroundJobsButton.Image")));
            this.cancelBackgroundJobsButton.ImageTransparentColor = System.Drawing.Color.Blue;
            this.cancelBackgroundJobsButton.Name = "cancelBackgroundJobsButton";
            this.cancelBackgroundJobsButton.ShowDropDownArrow = false;
            this.cancelBackgroundJobsButton.Size = new System.Drawing.Size(20, 20);
            this.cancelBackgroundJobsButton.Text = "toolStripDropDownButton1";
            this.cancelBackgroundJobsButton.Click += new System.EventHandler(this.cancelBackgroundJobsButton_Click);
            // 
            // toolStripSignInStateButton
            // 
            this.toolStripSignInStateButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSignInStateButton.Image")));
            this.toolStripSignInStateButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripSignInStateButton.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.toolStripSignInStateButton.Name = "toolStripSignInStateButton";
            this.toolStripSignInStateButton.ShowDropDownArrow = false;
            this.toolStripSignInStateButton.Size = new System.Drawing.Size(104, 20);
            this.toolStripSignInStateButton.Text = "(not signed in)";
            this.toolStripSignInStateButton.Click += new System.EventHandler(this.toolStripEmailButton_Click);
            // 
            // toolStripDeviceStateButton
            // 
            this.toolStripDeviceStateButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDeviceStateButton.Image")));
            this.toolStripDeviceStateButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripDeviceStateButton.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.toolStripDeviceStateButton.Name = "toolStripDeviceStateButton";
            this.toolStripDeviceStateButton.ShowDropDownArrow = false;
            this.toolStripDeviceStateButton.Size = new System.Drawing.Size(95, 20);
            this.toolStripDeviceStateButton.Text = "(not verified)";
            this.toolStripDeviceStateButton.Click += new System.EventHandler(this.toolStripDeviceStateButton_Click);
            // 
            // addProjectToolStripMenuItem
            // 
            this.addProjectToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addProjectToolStripMenuItem.Image")));
            this.addProjectToolStripMenuItem.Name = "addProjectToolStripMenuItem";
            this.addProjectToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.addProjectToolStripMenuItem.Text = "&Add Google Cloud project...";
            this.addProjectToolStripMenuItem.Click += new System.EventHandler(this.addProjectToolStripMenuItem_Click);
            // 
            // projectExplorerToolStripMenuItem
            // 
            this.projectExplorerToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("projectExplorerToolStripMenuItem.Image")));
            this.projectExplorerToolStripMenuItem.Name = "projectExplorerToolStripMenuItem";
            this.projectExplorerToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.L)));
            this.projectExplorerToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.projectExplorerToolStripMenuItem.Text = "&Project Explorer";
            this.projectExplorerToolStripMenuItem.Click += new System.EventHandler(this.projectExplorerToolStripMenuItem_Click);
            // 
            // viewHelpToolStripMenuItem
            // 
            this.viewHelpToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("viewHelpToolStripMenuItem.Image")));
            this.viewHelpToolStripMenuItem.Name = "viewHelpToolStripMenuItem";
            this.viewHelpToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.viewHelpToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.viewHelpToolStripMenuItem.Text = "&Documentation";
            this.viewHelpToolStripMenuItem.Click += new System.EventHandler(this.viewHelpToolStripMenuItem_Click);
            // 
            // openIapDocsToolStripMenuItem
            // 
            this.openIapDocsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openIapDocsToolStripMenuItem.Image")));
            this.openIapDocsToolStripMenuItem.Name = "openIapDocsToolStripMenuItem";
            this.openIapDocsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.openIapDocsToolStripMenuItem.Text = "&IAP TCP forwarding overview";
            this.openIapDocsToolStripMenuItem.Click += new System.EventHandler(this.openIapDocsToolStripMenuItem_Click);
            // 
            // openIapFirewallDocsToolStripMenuItem
            // 
            this.openIapFirewallDocsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Properties.Resources.Documentation_16;
            this.openIapFirewallDocsToolStripMenuItem.Name = "openIapFirewallDocsToolStripMenuItem";
            this.openIapFirewallDocsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.openIapFirewallDocsToolStripMenuItem.Text = "How to create a &firewall rule for IAP";
            this.openIapFirewallDocsToolStripMenuItem.Click += new System.EventHandler(this.openIapFirewallDocsToolStripMenuItem_Click);
            // 
            // openIapAccessDocsToolStripMenuItem
            // 
            this.openIapAccessDocsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openIapAccessDocsToolStripMenuItem.Image")));
            this.openIapAccessDocsToolStripMenuItem.Name = "openIapAccessDocsToolStripMenuItem";
            this.openIapAccessDocsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.openIapAccessDocsToolStripMenuItem.Text = "How to &grant permissions to use IAP";
            this.openIapAccessDocsToolStripMenuItem.Click += new System.EventHandler(this.openIapAccessDocsToolStripMenuItem_Click);
            // 
            // overviewSeparatorStripMenuItem
            // 
            this.overviewSeparatorStripMenuItem.Name = "overviewSeparatorStripMenuItem";
            this.overviewSeparatorStripMenuItem.Size = new System.Drawing.Size(263, 6);
            // 
            // openSecureConnectDocsToolStripMenuItem
            // 
            this.openSecureConnectDocsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Properties.Resources.Documentation_16;
            this.openSecureConnectDocsToolStripMenuItem.Name = "openSecureConnectDocsToolStripMenuItem";
            this.openSecureConnectDocsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.openSecureConnectDocsToolStripMenuItem.Text = "Endpoint &Verification overview";
            this.openSecureConnectDocsToolStripMenuItem.Click += new System.EventHandler(this.openSecureConnectDocsToolStripMenuItem_Click);
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
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
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
        private System.Windows.Forms.ToolStripMenuItem reportIssueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableloggingToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatus;
        private System.Windows.Forms.ToolStripDropDownButton cancelBackgroundJobsButton;
        private System.Windows.Forms.ToolStripStatusLabel backgroundJobLabel;
        private System.Windows.Forms.ToolStripMenuItem viewHelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton toolStripSignInStateButton;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDeviceStateButton;
        private System.Windows.Forms.ToolStripMenuItem reportInternalIssueToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator howtoSeparatorStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openIapFirewallDocsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator overviewSeparatorStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openSecureConnectDocsToolStripMenuItem;
    }
}