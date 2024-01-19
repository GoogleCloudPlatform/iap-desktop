//
// Copyright 2022 Google LLC
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

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.SshKeys
{
    partial class AuthorizedPublicKeysView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuthorizedPublicKeysView));
            this.keysList = new Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.SshKeys.AuthorizedPublicKeysList();
            this.panel = new NotificationBarPanel();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.refreshToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.deleteToolStripButton = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.panel)).BeginInit();
            this.panel.Panel1.SuspendLayout();
            this.panel.Panel2.SuspendLayout();
            this.panel.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // keysList
            // 
            this.keysList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.keysList.Loading = true;
            this.keysList.Location = new System.Drawing.Point(0, 0);
            this.keysList.MultiSelect = true;
            this.keysList.Name = "keysList";
            this.keysList.SearchOnKeyDown = false;
            this.keysList.SearchTerm = "";
            this.keysList.Size = new System.Drawing.Size(800, 399);
            this.keysList.TabIndex = 0;
            // 
            // splitContainer
            // 
            this.panel.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            // 
            // splitContainer.Panel2
            // 
            this.panel.Panel2.Controls.Add(this.keysList);
            this.panel.TabIndex = 0;
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripButton,
            this.deleteToolStripButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "toolStrip1";
            // 
            // refreshToolStripButton
            // 
            this.refreshToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshToolStripButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.Refresh_16;
            this.refreshToolStripButton.Name = "refreshToolStripButton";
            this.refreshToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.refreshToolStripButton.Text = "Refresh";
            // 
            // deleteToolStripButton
            // 
            this.deleteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.deleteToolStripButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.DeleteKey_16;
            this.deleteToolStripButton.Name = "deleteToolStripButton";
            this.deleteToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.deleteToolStripButton.Text = "Delete key";
            // 
            // AuthorizedPublicKeysView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.toolStrip);
            this.Name = "AuthorizedPublicKeysView";
            this.Text = "Authorized Keys";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Window_KeyDown);
            this.panel.Panel1.ResumeLayout(false);
            this.panel.Panel1.PerformLayout();
            this.panel.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panel)).EndInit();
            this.panel.ResumeLayout(false);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AuthorizedPublicKeysList keysList;
        private NotificationBarPanel panel;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton refreshToolStripButton;
        private System.Windows.Forms.ToolStripButton deleteToolStripButton;
    }
}