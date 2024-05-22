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

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.EventLog
{
    partial class EventLogView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventLogView));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openInCloudConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.timeFrameComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.list = new Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.EventLog.EventsListView();
            this.timestampColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceNameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.severityColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.descriptionColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.principalColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.deviceColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.deviceStateColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.accessLevelsColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.lifecycleEventsDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeLifecycleEventsButton = new System.Windows.Forms.ToolStripMenuItem();
            this.includeSystemEventsButton = new System.Windows.Forms.ToolStripMenuItem();
            this.includeAccessEventsButton = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogsButton = new System.Windows.Forms.ToolStripButton();
            this.contextMenuStrip.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openInCloudConsoleToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(235, 26);
            // 
            // openInCloudConsoleToolStripMenuItem
            // 
            this.openInCloudConsoleToolStripMenuItem.Name = "openInCloudConsoleToolStripMenuItem";
            this.openInCloudConsoleToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
            this.openInCloudConsoleToolStripMenuItem.Text = "Show details in Cloud Console";
            this.openInCloudConsoleToolStripMenuItem.Click += new System.EventHandler(this.openInCloudConsoleToolStripMenuItem_Click);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "Success_gray_16.png");
            this.imageList.Images.SetKeyName(1, "Warning_gray_16.png");
            this.imageList.Images.SetKeyName(2, "Error_gray_16.png");
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshButton,
            this.toolStripSeparator1,
            this.lifecycleEventsDropDown,
            this.timeFrameComboBox,
            this.toolStripSeparator2,
            this.openLogsButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // timeFrameComboBox
            // 
            this.timeFrameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.timeFrameComboBox.Name = "timeFrameComboBox";
            this.timeFrameComboBox.Size = new System.Drawing.Size(121, 25);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // list
            // 
            this.list.AutoResizeColumnsOnUpdate = false;
            this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.timestampColumn,
            this.instanceNameColumn,
            this.severityColumn,
            this.descriptionColumn,
            this.principalColumn,
            this.deviceColumn,
            this.deviceStateColumn,
            this.accessLevelsColumn});
            this.list.ContextMenuStrip = this.contextMenuStrip;
            this.list.Dock = System.Windows.Forms.DockStyle.Fill;
            this.list.FullRowSelect = true;
            this.list.GridLines = true;
            this.list.HideSelection = false;
            this.list.Location = new System.Drawing.Point(0, 25);
            this.list.MultiSelect = false;
            this.list.Name = "list";
            this.list.SelectedModelItem = null;
            this.list.Size = new System.Drawing.Size(800, 425);
            this.list.SmallImageList = this.imageList;
            this.list.TabIndex = 1;
            this.list.UseCompatibleStateImageBehavior = false;
            this.list.View = System.Windows.Forms.View.Details;
            this.list.DoubleClick += new System.EventHandler(this.list_DoubleClick);
            // 
            // timestampColumn
            // 
            this.timestampColumn.Text = "Timestamp (UTC)";
            this.timestampColumn.Width = 150;
            // 
            // instanceNameColumn
            // 
            this.instanceNameColumn.Text = "Instance name";
            this.instanceNameColumn.Width = 130;
            // 
            // severityColumn
            // 
            this.severityColumn.Text = "Severity";
            // 
            // descriptionColumn
            // 
            this.descriptionColumn.Text = "Description";
            this.descriptionColumn.Width = 350;
            // 
            // principalColumn
            // 
            this.principalColumn.Text = "Principal";
            this.principalColumn.Width = 150;
            // 
            // deviceColumn
            // 
            this.deviceColumn.Text = "Device";
            // 
            // deviceStateColumn
            // 
            this.deviceStateColumn.Text = "Device state";
            this.deviceStateColumn.Width = 80;
            // 
            // accessLevelsColumn
            // 
            this.accessLevelsColumn.Text = "Access levels";
            this.accessLevelsColumn.Width = 25;
            // 
            // refreshButton
            // 
            this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshButton.Enabled = false;
            this.refreshButton.Image = global::Google.Solutions.IapDesktop.Extensions.Management.Properties.Resources.Refresh_16;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(23, 22);
            this.refreshButton.Text = "Refresh";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // lifecycleEventsDropDown
            // 
            this.lifecycleEventsDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeLifecycleEventsButton,
            this.includeSystemEventsButton,
            this.includeAccessEventsButton});
            this.lifecycleEventsDropDown.Image = ((System.Drawing.Image)(resources.GetObject("lifecycleEventsDropDown.Image")));
            this.lifecycleEventsDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.lifecycleEventsDropDown.Name = "lifecycleEventsDropDown";
            this.lifecycleEventsDropDown.Size = new System.Drawing.Size(96, 22);
            this.lifecycleEventsDropDown.Text = "Event types";
            // 
            // includeLifecycleEventsButton
            // 
            this.includeLifecycleEventsButton.CheckOnClick = true;
            this.includeLifecycleEventsButton.Name = "includeLifecycleEventsButton";
            this.includeLifecycleEventsButton.Size = new System.Drawing.Size(175, 22);
            this.includeLifecycleEventsButton.Text = "VM lifecycle events";
            // 
            // includeSystemEventsButton
            // 
            this.includeSystemEventsButton.CheckOnClick = true;
            this.includeSystemEventsButton.Name = "includeSystemEventsButton";
            this.includeSystemEventsButton.Size = new System.Drawing.Size(175, 22);
            this.includeSystemEventsButton.Text = "VM system events";
            // 
            // includeAccessEventsButton
            // 
            this.includeAccessEventsButton.CheckOnClick = true;
            this.includeAccessEventsButton.Name = "includeAccessEventsButton";
            this.includeAccessEventsButton.Size = new System.Drawing.Size(175, 22);
            this.includeAccessEventsButton.Text = "VM access events";
            // 
            // openLogsButton
            // 
            this.openLogsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openLogsButton.Image = ((System.Drawing.Image)(resources.GetObject("openLogsButton.Image")));
            this.openLogsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openLogsButton.Name = "openLogsButton";
            this.openLogsButton.Size = new System.Drawing.Size(23, 22);
            this.openLogsButton.Text = "Open logs in Cloud Console";
            this.openLogsButton.Click += new System.EventHandler(this.openLogsButton_Click);
            // 
            // EventLogWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.list);
            this.Controls.Add(this.toolStrip);
            this.Name = "EventLogWindow";
            this.ShowIcon = false;
            this.Text = "Event log";
            this.contextMenuStrip.ResumeLayout(false);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripDropDownButton lifecycleEventsDropDown;
        private System.Windows.Forms.ToolStripMenuItem includeLifecycleEventsButton;
        private System.Windows.Forms.ToolStripMenuItem includeSystemEventsButton;
        private EventsListView list;
        private System.Windows.Forms.ColumnHeader timestampColumn;
        private System.Windows.Forms.ColumnHeader severityColumn;
        private System.Windows.Forms.ColumnHeader descriptionColumn;
        private System.Windows.Forms.ColumnHeader principalColumn;
        private System.Windows.Forms.ToolStripComboBox timeFrameComboBox;
        private System.Windows.Forms.ColumnHeader instanceNameColumn;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem openInCloudConsoleToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton openLogsButton;
        private System.Windows.Forms.ToolStripMenuItem includeAccessEventsButton;
        private System.Windows.Forms.ColumnHeader deviceColumn;
        private System.Windows.Forms.ColumnHeader deviceStateColumn;
        private System.Windows.Forms.ColumnHeader accessLevelsColumn;
    }
}