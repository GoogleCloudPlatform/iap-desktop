//
// Copyright 2021 Google LLC
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

namespace Google.Solutions.IapDesktop.Application.Windows.ProjectPicker
{
    partial class ProjectPickerView
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
            this.headlineLabel = new HeaderLabel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.pickProjectButton = new System.Windows.Forms.Button();
            this.projectList = new Google.Solutions.IapDesktop.Application.Windows.ProjectPicker.ProjectList();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // headlineLabel
            // 
            this.headlineLabel.AutoSize = true;
            this.headlineLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headlineLabel.Location = new System.Drawing.Point(11, 15);
            this.headlineLabel.Name = "headlineLabel";
            this.headlineLabel.Size = new System.Drawing.Size(125, 30);
            this.headlineLabel.TabIndex = 0;
            this.headlineLabel.Text = "Pick project";
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(390, 425);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // addProjectButton
            // 
            this.pickProjectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pickProjectButton.Location = new System.Drawing.Point(310, 425);
            this.pickProjectButton.Name = "addProjectButton";
            this.pickProjectButton.Size = new System.Drawing.Size(75, 23);
            this.pickProjectButton.TabIndex = 2;
            this.pickProjectButton.Text = "&Pick project";
            this.pickProjectButton.UseVisualStyleBackColor = true;
            this.pickProjectButton.Click += new System.EventHandler(this.addProjectButton_Click);
            // 
            // projectList
            // 
            this.projectList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.projectList.Loading = true;
            this.projectList.Location = new System.Drawing.Point(15, 60);
            this.projectList.MultiSelect = true;
            this.projectList.Name = "projectList";
            this.projectList.SearchOnKeyDown = true;
            this.projectList.SearchTerm = "";
            this.projectList.Size = new System.Drawing.Size(450, 351);
            this.projectList.TabIndex = 1;
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(13, 425);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(16, 13);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "...";
            // 
            // ProjectPickerWindow
            // 
            this.AcceptButton = this.pickProjectButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(484, 461);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.pickProjectButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.projectList);
            this.Controls.Add(this.headlineLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 500);
            this.Name = "ProjectPickerWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Pick project";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private HeaderLabel headlineLabel;
        private ProjectList projectList;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button pickProjectButton;
        private System.Windows.Forms.Label statusLabel;
    }
}