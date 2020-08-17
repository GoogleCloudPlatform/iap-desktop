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

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory
{
    partial class PackageInventoryWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PackageInventoryWindow));
            this.packageList = new Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory.PackageList();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.infoLabel = new System.Windows.Forms.Label();
            this.infoIcon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.infoIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // packageList
            // 
            this.packageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packageList.Loading = true;
            this.packageList.Location = new System.Drawing.Point(0, 0);
            this.packageList.Name = "packageList";
            this.packageList.SearchTerm = "";
            this.packageList.Size = new System.Drawing.Size(800, 450);
            this.packageList.TabIndex = 0;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.IsSplitterFixed = true;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.BackColor = System.Drawing.SystemColors.Info;
            this.splitContainer.Panel1.Controls.Add(this.infoLabel);
            this.splitContainer.Panel1.Controls.Add(this.infoIcon);
            this.splitContainer.Panel1MinSize = 22;
            this.splitContainer.Size = new System.Drawing.Size(800, 450);
            this.splitContainer.SplitterDistance = 25;
            this.splitContainer.SplitterWidth = 1;
            this.splitContainer.TabIndex = 1;

            this.splitContainer.Panel1MinSize = 22;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.packageList);
            this.splitContainer.Size = new System.Drawing.Size(800, 450);
            this.splitContainer.SplitterDistance = 25;
            this.splitContainer.SplitterWidth = 1;
            this.splitContainer.TabIndex = 0;
            // 
            // infoLabel
            // 
            this.infoLabel.AutoSize = true;
            this.infoLabel.Location = new System.Drawing.Point(22, 4);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(86, 13);
            this.infoLabel.TabIndex = 2;
            this.infoLabel.Text = "This is a warning";
            // 
            // infoIcon
            // 
            this.infoIcon.Image = ((System.Drawing.Image)(resources.GetObject("infoIcon.Image")));
            this.infoIcon.Location = new System.Drawing.Point(6, 3);
            this.infoIcon.Name = "infoIcon";
            this.infoIcon.Size = new System.Drawing.Size(16, 16);
            this.infoIcon.TabIndex = 1;
            this.infoIcon.TabStop = false;
            // 
            // PackageInventoryWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer);
            this.Name = "PackageInventoryWindow";
            this.Text = "PackageInventoryWindow";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.infoIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private PackageList packageList;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.PictureBox infoIcon;
    }
}