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
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projectExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableloggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new Google.Solutions.Mvvm.Controls.ActiveStatusStrip();
            this.backgroundJobLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.cancelBackgroundJobsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.accessStateButton = new System.Windows.Forms.ToolStripDropDownButton();
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
            this.viewToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.windowToolStripMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(984, 24);
            this.mainMenu.TabIndex = 3;
            this.mainMenu.Text = "menuStrip1";
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
            this.projectExplorerToolStripMenuItem.Image = global::Google.Solutions.IapDesktop.Properties.Resources.ProjectExplorer_16;
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
            // statusStrip
            // 
            this.statusStrip.Active = false;
            this.statusStrip.ActiveBackColor = System.Drawing.SystemColors.Highlight;
            this.statusStrip.ActiveForeColor = System.Drawing.SystemColors.HighlightText;
            this.statusStrip.BackColor = System.Drawing.SystemColors.Control;
            this.statusStrip.ForeColor = System.Drawing.SystemColors.ControlText;
            this.statusStrip.InactiveBackColor = System.Drawing.SystemColors.Control;
            this.statusStrip.InactiveForeColor = System.Drawing.SystemColors.ControlText;
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.backgroundJobLabel,
            this.cancelBackgroundJobsButton,
            this.toolStripStatus,
            this.accessStateButton,
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
            this.cancelBackgroundJobsButton.Name = "cancelBackgroundJobsButton";
            this.cancelBackgroundJobsButton.ShowDropDownArrow = false;
            this.cancelBackgroundJobsButton.Size = new System.Drawing.Size(20, 20);
            this.cancelBackgroundJobsButton.Text = "toolStripDropDownButton1";
            this.cancelBackgroundJobsButton.Click += new System.EventHandler(this.cancelBackgroundJobsButton_Click);
            // 
            // toolStripStatus
            // 
            this.toolStripStatus.Name = "toolStripStatus";
            this.toolStripStatus.Size = new System.Drawing.Size(835, 17);
            this.toolStripStatus.Spring = true;
            this.toolStripStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // accessStateButton
            // 
            this.accessStateButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.accessStateButton.Image = ((System.Drawing.Image)(resources.GetObject("accessStateButton.Image")));
            this.accessStateButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.accessStateButton.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.accessStateButton.Name = "accessStateButton";
            this.accessStateButton.ShowDropDownArrow = false;
            this.accessStateButton.Size = new System.Drawing.Size(20, 20);
            this.accessStateButton.Text = "Connected";
            this.accessStateButton.Click += new System.EventHandler(this.accessStateButton_Click);
            // 
            // profileStateButton
            // 
            this.profileStateButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addProfileToolStripMenuItem,
            this.toolStripMenuItem1});
            this.profileStateButton.Image = global::Google.Solutions.IapDesktop.Properties.Resources.UserWhite_16;
            this.profileStateButton.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.profileStateButton.Name = "profileStateButton";
            this.profileStateButton.Size = new System.Drawing.Size(78, 20);
            this.profileStateButton.Text = "(Profile)";
            // 
            // addProfileToolStripMenuItem
            // 
            this.addProfileToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addProfileToolStripMenuItem.Image")));
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
        private ActiveStatusStrip statusStrip;
        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanel;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem projectExplorerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableloggingToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatus;
        private System.Windows.Forms.ToolStripDropDownButton cancelBackgroundJobsButton;
        private System.Windows.Forms.ToolStripStatusLabel backgroundJobLabel;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton accessStateButton;
        private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton profileStateButton;
        private System.Windows.Forms.ToolStripMenuItem addProfileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
    }
}