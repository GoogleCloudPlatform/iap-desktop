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

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Tunnels
{
    partial class TunnelsView
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
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.refreshToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.tunnelsList = new Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Tunnels.TunnelsListView();
            this.instanceHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.projectIdHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.zoneHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.transmittedHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.receivedHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.localEndpointHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.remotePortHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.protocolHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.securityHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.policyHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(862, 25);
            this.toolStrip.TabIndex = 5;
            this.toolStrip.Text = "toolStrip";
            // 
            // refreshToolStripButton
            // 
            this.refreshToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshToolStripButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.Refresh_16;
            this.refreshToolStripButton.Name = "refreshToolStripButton";
            this.refreshToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.refreshToolStripButton.Text = "Refresh";
            // 
            // tunnelsList
            // 
            this.tunnelsList.AutoResizeColumnsOnUpdate = false;
            this.tunnelsList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tunnelsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.instanceHeader,
            this.projectIdHeader,
            this.zoneHeader,
            this.transmittedHeader,
            this.receivedHeader,
            this.localEndpointHeader,
            this.remotePortHeader,
            this.protocolHeader,
            this.securityHeader,
            this.policyHeader});
            this.tunnelsList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tunnelsList.FullRowSelect = true;
            this.tunnelsList.GridLines = true;
            this.tunnelsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.tunnelsList.HideSelection = false;
            this.tunnelsList.Location = new System.Drawing.Point(0, 25);
            this.tunnelsList.MultiSelect = false;
            this.tunnelsList.Name = "tunnelsList";
            this.tunnelsList.SelectedModelItem = null;
            this.tunnelsList.Size = new System.Drawing.Size(862, 339);
            this.tunnelsList.TabIndex = 6;
            this.tunnelsList.UseCompatibleStateImageBehavior = false;
            this.tunnelsList.View = System.Windows.Forms.View.Details;
            // 
            // instanceHeader
            // 
            this.instanceHeader.Text = "Instance";
            this.instanceHeader.Width = 130;
            // 
            // projectIdHeader
            // 
            this.projectIdHeader.Text = "Project ID";
            this.projectIdHeader.Width = 130;
            // 
            // zoneHeader
            // 
            this.zoneHeader.Text = "Zone";
            this.zoneHeader.Width = 130;
            // 
            // transmittedHeader
            // 
            this.transmittedHeader.Text = "Transmitted";
            this.transmittedHeader.Width = 80;
            // 
            // receivedHeader
            // 
            this.receivedHeader.Text = "Received";
            this.receivedHeader.Width = 80;
            // 
            // localEndpointHeader
            // 
            this.localEndpointHeader.Text = "Local port";
            this.localEndpointHeader.Width = 110;
            // 
            // remotePortHeader
            // 
            this.remotePortHeader.Text = "Remote port";
            this.remotePortHeader.Width = 80;
            // 
            // protocolHeader
            // 
            this.protocolHeader.Text = "Protocol";
            // 
            // securityHeader
            // 
            this.securityHeader.Text = "Security";
            // 
            // policyHeader
            // 
            this.policyHeader.Text = "Access policy";
            // 
            // TunnelsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(862, 364);
            this.ControlBox = false;
            this.Controls.Add(this.tunnelsList);
            this.Controls.Add(this.toolStrip);
            this.Name = "TunnelsView";
            this.ShowIcon = false;
            this.Text = "Active IAP tunnels";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip;
        private TunnelsListView tunnelsList;
        private System.Windows.Forms.ColumnHeader instanceHeader;
        private System.Windows.Forms.ColumnHeader projectIdHeader;
        private System.Windows.Forms.ColumnHeader zoneHeader;
        private System.Windows.Forms.ColumnHeader localEndpointHeader;
        private System.Windows.Forms.ColumnHeader transmittedHeader;
        private System.Windows.Forms.ColumnHeader receivedHeader;
        private System.Windows.Forms.ToolStripButton refreshToolStripButton;
        private System.Windows.Forms.ColumnHeader remotePortHeader;
        private System.Windows.Forms.ColumnHeader protocolHeader;
        private System.Windows.Forms.ColumnHeader securityHeader;
        private System.Windows.Forms.ColumnHeader policyHeader;
    }
}