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

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory
{
    partial class PackageInventoryViewBase
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PackageInventoryViewBase));
            this.packageList = new Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory.PackageList();
            this.panel = new NotificationBarPanel();
            ((System.ComponentModel.ISupportInitialize)(this.panel)).BeginInit();
            this.panel.Panel1.SuspendLayout();
            this.panel.Panel2.SuspendLayout();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // packageList
            // 
            this.packageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packageList.Loading = true;
            this.packageList.Location = new System.Drawing.Point(0, 0);
            this.packageList.MultiSelect = true;
            this.packageList.Name = "packageList";
            this.packageList.SearchOnKeyDown = false;
            this.packageList.SearchTerm = "";
            this.packageList.Size = new System.Drawing.Size(800, 424);
            this.packageList.TabIndex = 0;
            // 
            // splitContainer
            // 
            this.panel.Name = "splitContainer";
            // 
            // splitContainer.Panel2
            // 
            this.panel.Panel2.Controls.Add(this.packageList);
            this.panel.Size = new System.Drawing.Size(800, 450);
            this.panel.TabIndex = 0;
            // 
            // PackageInventoryViewBase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel);
            this.Name = "PackageInventoryViewBase";
            this.Text = "PackageInventoryWindow";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PackageInventoryWindow_KeyDown);
            this.panel.Panel1.ResumeLayout(false);
            this.panel.Panel1.PerformLayout();
            this.panel.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panel)).EndInit();
            this.panel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private PackageList packageList;
        private NotificationBarPanel panel;
    }
}