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

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    partial class UserFlyoutWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserFlyoutWindow));
            this.userIcon = new System.Windows.Forms.PictureBox();
            this.emailHeaderLabel = new System.Windows.Forms.Label();
            this.emailLabel = new System.Windows.Forms.Label();
            this.manageLink = new System.Windows.Forms.LinkLabel();
            this.closeButton = new System.Windows.Forms.Button();
            this.managedByLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.userIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // userIcon
            // 
            this.userIcon.Image = ((System.Drawing.Image)(resources.GetObject("userIcon.Image")));
            this.userIcon.Location = new System.Drawing.Point(12, 12);
            this.userIcon.Name = "userIcon";
            this.userIcon.Size = new System.Drawing.Size(48, 41);
            this.userIcon.TabIndex = 2;
            this.userIcon.TabStop = false;
            // 
            // emailHeaderLabel
            // 
            this.emailHeaderLabel.AutoSize = true;
            this.emailHeaderLabel.Location = new System.Drawing.Point(66, 11);
            this.emailHeaderLabel.Name = "emailHeaderLabel";
            this.emailHeaderLabel.Size = new System.Drawing.Size(68, 13);
            this.emailHeaderLabel.TabIndex = 3;
            this.emailHeaderLabel.Text = "Signed in as:";
            // 
            // emailLabel
            // 
            this.emailLabel.AutoEllipsis = true;
            this.emailLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.emailLabel.Location = new System.Drawing.Point(67, 26);
            this.emailLabel.Name = "emailLabel";
            this.emailLabel.Size = new System.Drawing.Size(160, 13);
            this.emailLabel.TabIndex = 3;
            this.emailLabel.Text = " ";
            // 
            // manageLink
            // 
            this.manageLink.AutoSize = true;
            this.manageLink.Location = new System.Drawing.Point(66, 63);
            this.manageLink.Name = "manageLink";
            this.manageLink.Size = new System.Drawing.Size(86, 13);
            this.manageLink.TabIndex = 4;
            this.manageLink.TabStop = true;
            this.manageLink.Text = "Account settings";
            this.manageLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.manageLink_LinkClicked);
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.closeButton.Location = new System.Drawing.Point(227, 2);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(24, 24);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "✖";
            this.closeButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // managedByLabel
            // 
            this.managedByLabel.AutoEllipsis = true;
            this.managedByLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.managedByLabel.Location = new System.Drawing.Point(66, 41);
            this.managedByLabel.Name = "managedByLabel";
            this.managedByLabel.Size = new System.Drawing.Size(160, 13);
            this.managedByLabel.TabIndex = 3;
            this.managedByLabel.Text = " ";
            // 
            // UserFlyoutWindow
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(254, 90);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.manageLink);
            this.Controls.Add(this.managedByLabel);
            this.Controls.Add(this.emailLabel);
            this.Controls.Add(this.emailHeaderLabel);
            this.Controls.Add(this.userIcon);
            this.Name = "UserFlyoutWindow";
            this.Text = "UserFlyoutWindow";
            ((System.ComponentModel.ISupportInitialize)(this.userIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox userIcon;
        private System.Windows.Forms.Label emailHeaderLabel;
        private System.Windows.Forms.Label emailLabel;
        private System.Windows.Forms.LinkLabel manageLink;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label managedByLabel;
    }
}