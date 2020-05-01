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

namespace Google.Solutions.IapDesktop.Application.Services.Windows.TunnelsViewer
{
    partial class TunnelsWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TunnelsWindow));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.disconnectToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.tunnelsList = new System.Windows.Forms.ListView();
            this.instanceHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.projectIdHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.zoneHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.localPortHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.disconnectTunnelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip.SuspendLayout();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.disconnectToolStripButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(455, 25);
            this.toolStrip.TabIndex = 5;
            this.toolStrip.Text = "toolStrip";
            // 
            // disconnectToolStripButton
            // 
            this.disconnectToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.disconnectToolStripButton.Enabled = false;
            this.disconnectToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("disconnectToolStripButton.Image")));
            this.disconnectToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.disconnectToolStripButton.Name = "disconnectToolStripButton";
            this.disconnectToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.disconnectToolStripButton.Text = "Disconnect tunnel";
            this.disconnectToolStripButton.Click += new System.EventHandler(this.disconnectToolStripButton_Click);
            // 
            // tunnelsList
            // 
            this.tunnelsList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tunnelsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.instanceHeader,
            this.projectIdHeader,
            this.zoneHeader,
            this.localPortHeader});
            this.tunnelsList.ContextMenuStrip = this.contextMenuStrip;
            this.tunnelsList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tunnelsList.FullRowSelect = true;
            this.tunnelsList.GridLines = true;
            this.tunnelsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.tunnelsList.HideSelection = false;
            this.tunnelsList.Location = new System.Drawing.Point(0, 25);
            this.tunnelsList.MultiSelect = false;
            this.tunnelsList.Name = "tunnelsList";
            this.tunnelsList.Size = new System.Drawing.Size(455, 339);
            this.tunnelsList.TabIndex = 6;
            this.tunnelsList.UseCompatibleStateImageBehavior = false;
            this.tunnelsList.View = System.Windows.Forms.View.Details;
            this.tunnelsList.SelectedIndexChanged += new System.EventHandler(this.tunnelsList_SelectedIndexChanged);
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
            // localPortHeader
            // 
            this.localPortHeader.Text = "Local Port";
            this.localPortHeader.Width = 70;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.disconnectTunnelToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(171, 26);
            // 
            // disconnectTunnelToolStripMenuItem
            // 
            this.disconnectTunnelToolStripMenuItem.Enabled = false;
            this.disconnectTunnelToolStripMenuItem.Name = "disconnectTunnelToolStripMenuItem";
            this.disconnectTunnelToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.disconnectTunnelToolStripMenuItem.Text = "&Disconnect tunnel";
            this.disconnectTunnelToolStripMenuItem.Click += new System.EventHandler(this.disconnectToolStripButton_Click);
            // 
            // TunnelsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(455, 364);
            this.ControlBox = false;
            this.Controls.Add(this.tunnelsList);
            this.Controls.Add(this.toolStrip);
            this.Name = "TunnelsWindow";
            this.ShowIcon = false;
            this.Text = "Active IAP tunnels";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton disconnectToolStripButton;
        private System.Windows.Forms.ListView tunnelsList;
        private System.Windows.Forms.ColumnHeader instanceHeader;
        private System.Windows.Forms.ColumnHeader projectIdHeader;
        private System.Windows.Forms.ColumnHeader zoneHeader;
        private System.Windows.Forms.ColumnHeader localPortHeader;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem disconnectTunnelToolStripMenuItem;
    }
}