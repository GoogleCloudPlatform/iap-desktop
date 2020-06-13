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

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    partial class EventLogWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventLogWindow));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.theme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.lifecycleEventsDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeUserEventsButton = new System.Windows.Forms.ToolStripMenuItem();
            this.includeSystemEventsButton = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshButton,
            this.toolStripSeparator1,
            this.lifecycleEventsDropDown});
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
            this.refreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(23, 22);
            this.refreshButton.Text = "Refresh";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // lifecycleEventsDropDown
            // 
            this.lifecycleEventsDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeUserEventsButton,
            this.includeSystemEventsButton});
            this.lifecycleEventsDropDown.Image = ((System.Drawing.Image)(resources.GetObject("lifecycleEventsDropDown.Image")));
            this.lifecycleEventsDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.lifecycleEventsDropDown.Name = "lifecycleEventsDropDown";
            this.lifecycleEventsDropDown.Size = new System.Drawing.Size(119, 22);
            this.lifecycleEventsDropDown.Text = "Lifecycle events";
            // 
            // includeUserEventsButton
            // 
            this.includeUserEventsButton.CheckOnClick = true;
            this.includeUserEventsButton.Name = "includeUserEventsButton";
            this.includeUserEventsButton.Size = new System.Drawing.Size(180, 22);
            this.includeUserEventsButton.Text = "User events";
            // 
            // includeSystemEventsButton
            // 
            this.includeSystemEventsButton.CheckOnClick = true;
            this.includeSystemEventsButton.Name = "includeSystemEventsButton";
            this.includeSystemEventsButton.Size = new System.Drawing.Size(180, 22);
            this.includeSystemEventsButton.Text = "System events";
            // 
            // ActivityLogWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.toolStrip);
            this.Name = "EventLogWindow";
            this.ShowIcon = false;
            this.Text = "Event Log";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip;
        private WeifenLuo.WinFormsUI.Docking.VS2015LightTheme theme;
        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripDropDownButton lifecycleEventsDropDown;
        private System.Windows.Forms.ToolStripMenuItem includeUserEventsButton;
        private System.Windows.Forms.ToolStripMenuItem includeSystemEventsButton;
    }
}