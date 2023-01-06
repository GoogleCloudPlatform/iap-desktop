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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.signoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projectExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableloggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewShortcutsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.overviewSeparatorStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.openIapDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openSecureConnectDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.howtoSeparatorStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.openIapFirewallDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openIapAccessDocsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.releaseNotesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportIssueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportInternalIssueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.backgroundJobLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.cancelBackgroundJobsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.deviceStateButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.profileStateButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.addProfileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.dockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
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
            this.windowToolStripMenuItem,
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
            this.addProjectToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addProjectToolStripMenuItem.Image")));
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
            // windowToolStripMenuItem
            // 
            this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            this.windowToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
            this.windowToolStripMenuItem.Text = "&Window";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewHelpToolStripMenuItem,
            this.viewShortcutsToolStripMenuItem,
            this.overviewSeparatorStripMenuItem,
            this.openIapDocsToolStripMenuItem,
            this.openSecureConnectDocsToolStripMenuItem,
            this.howtoSeparatorStripMenuItem,
            this.openIapFirewallDocsToolStripMenuItem,
            this.openIapAccessDocsToolStripMenuItem,
            this.aboutSeparatorToolStripMenuItem,
            this.releaseNotesToolStripMenuItem,
            this.reportIssueToolStripMenuItem,
            this.reportInternalIssueToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
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
            // viewShortcutsToolStripMenuItem
            // 
            this.viewShortcutsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Properties.Resources.Documentation_16;
            this.viewShortcutsToolStripMenuItem.Name = "viewShortcutsToolStripMenuItem";
            this.viewShortcutsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.viewShortcutsToolStripMenuItem.Text = "Keyboard &shortcuts";
            this.viewShortcutsToolStripMenuItem.Click += new System.EventHandler(this.viewShortcutsToolStripMenuItem_Click);
            // 
            // overviewSeparatorStripMenuItem
            // 
            this.overviewSeparatorStripMenuItem.Name = "overviewSeparatorStripMenuItem";
            this.overviewSeparatorStripMenuItem.Size = new System.Drawing.Size(263, 6);
            // 
            // openIapDocsToolStripMenuItem
            // 
            this.openIapDocsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openIapDocsToolStripMenuItem.Image")));
            this.openIapDocsToolStripMenuItem.Name = "openIapDocsToolStripMenuItem";
            this.openIapDocsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.openIapDocsToolStripMenuItem.Text = "&IAP TCP forwarding overview";
            this.openIapDocsToolStripMenuItem.Click += new System.EventHandler(this.openIapDocsToolStripMenuItem_Click);
            // 
            // openSecureConnectDocsToolStripMenuItem
            // 
            this.openSecureConnectDocsToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Properties.Resources.Documentation_16;
            this.openSecureConnectDocsToolStripMenuItem.Name = "openSecureConnectDocsToolStripMenuItem";
            this.openSecureConnectDocsToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.openSecureConnectDocsToolStripMenuItem.Text = "Using &certificate-based access";
            this.openSecureConnectDocsToolStripMenuItem.Click += new System.EventHandler(this.openSecureConnectDocsToolStripMenuItem_Click);
            // 
            // howtoSeparatorStripMenuItem
            // 
            this.howtoSeparatorStripMenuItem.Name = "howtoSeparatorStripMenuItem";
            this.howtoSeparatorStripMenuItem.Size = new System.Drawing.Size(263, 6);
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
            // aboutSeparatorToolStripMenuItem
            // 
            this.aboutSeparatorToolStripMenuItem.Name = "aboutSeparatorToolStripMenuItem";
            this.aboutSeparatorToolStripMenuItem.Size = new System.Drawing.Size(263, 6);
            // 
            // releaseNotesToolStripMenuItem
            // 
            this.releaseNotesToolStripMenuItem.Name = "releaseNotesToolStripMenuItem";
            this.releaseNotesToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.releaseNotesToolStripMenuItem.Text = "Release &notes";
            this.releaseNotesToolStripMenuItem.Click += new System.EventHandler(this.releaseNotesToolStripMenuItem_Click);
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
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.backgroundJobLabel,
            this.cancelBackgroundJobsButton,
            this.toolStripStatus,
            this.deviceStateButton,
            this.profileStateButton});
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
            // toolStripStatus
            // 
            this.toolStripStatus.Name = "toolStripStatus";
            this.toolStripStatus.Size = new System.Drawing.Size(760, 17);
            this.toolStripStatus.Spring = true;
            this.toolStripStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // deviceStateButton
            // 
            this.deviceStateButton.Image = ((System.Drawing.Image)(resources.GetObject("deviceStateButton.Image")));
            this.deviceStateButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.deviceStateButton.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.deviceStateButton.Name = "deviceStateButton";
            this.deviceStateButton.ShowDropDownArrow = false;
            this.deviceStateButton.Size = new System.Drawing.Size(95, 20);
            this.deviceStateButton.Text = "(not verified)";
            this.deviceStateButton.Click += new System.EventHandler(this.toolStripDeviceStateButton_Click);
            // 
            // profileStateButton
            // 
            this.profileStateButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addProfileToolStripMenuItem,
            this.toolStripMenuItem1});
            this.profileStateButton.Image = ((System.Drawing.Image)(resources.GetObject("profileStateButton.Image")));
            this.profileStateButton.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.profileStateButton.Name = "profileStateButton";
            this.profileStateButton.Size = new System.Drawing.Size(78, 20);
            this.profileStateButton.Text = "(Profile)";
            // 
            // addProfileToolStripMenuItem
            // 
            this.addProfileToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Properties.Resources.NewProfile_16;
            this.addProfileToolStripMenuItem.Name = "addProfileToolStripMenuItem";
            this.addProfileToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.addProfileToolStripMenuItem.Text = "&Add profile...";
            this.addProfileToolStripMenuItem.Click += new System.EventHandler(this.addProfileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(139, 6);
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
            this.dockPanel.ActiveContentChanged += new System.EventHandler(this.dockPanel_ActiveContentChanged);
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
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
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
        private System.Windows.Forms.ToolStripDropDownButton deviceStateButton;
        private System.Windows.Forms.ToolStripMenuItem reportInternalIssueToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator howtoSeparatorStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openIapFirewallDocsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator overviewSeparatorStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openSecureConnectDocsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewShortcutsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem releaseNotesToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton profileStateButton;
        private System.Windows.Forms.ToolStripMenuItem addProfileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
    }
}