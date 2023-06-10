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

namespace Google.Solutions.IapDesktop.Application.Windows.Authorization
{
    partial class DeviceFlyoutView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DeviceFlyoutView));
            this.deviceEnrolledIcon = new System.Windows.Forms.PictureBox();
            this.enrollmentStateLabel = new System.Windows.Forms.Label();
            this.detailsLink = new System.Windows.Forms.LinkLabel();
            this.closeButton = new System.Windows.Forms.Button();
            this.deviceNotEnrolledIcon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.deviceEnrolledIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deviceNotEnrolledIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // deviceEnrolledIcon
            // 
            this.deviceEnrolledIcon.Image = ((System.Drawing.Image)(resources.GetObject("deviceEnrolledIcon.Image")));
            this.deviceEnrolledIcon.Location = new System.Drawing.Point(12, 12);
            this.deviceEnrolledIcon.Name = "deviceEnrolledIcon";
            this.deviceEnrolledIcon.Size = new System.Drawing.Size(48, 48);
            this.deviceEnrolledIcon.TabIndex = 2;
            this.deviceEnrolledIcon.TabStop = false;
            // 
            // enrollmentStateLabel
            // 
            this.enrollmentStateLabel.AutoEllipsis = true;
            this.enrollmentStateLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.enrollmentStateLabel.Location = new System.Drawing.Point(66, 11);
            this.enrollmentStateLabel.Name = "enrollmentStateLabel";
            this.enrollmentStateLabel.Size = new System.Drawing.Size(160, 65);
            this.enrollmentStateLabel.TabIndex = 3;
            this.enrollmentStateLabel.Text = " ";
            // 
            // detailsLink
            // 
            this.detailsLink.AutoSize = true;
            this.detailsLink.Location = new System.Drawing.Point(66, 63);
            this.detailsLink.Name = "detailsLink";
            this.detailsLink.Size = new System.Drawing.Size(16, 13);
            this.detailsLink.TabIndex = 4;
            this.detailsLink.TabStop = true;
            this.detailsLink.Text = "...";
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
            // 
            // deviceNotEnrolledIcon
            // 
            this.deviceNotEnrolledIcon.Image = ((System.Drawing.Image)(resources.GetObject("deviceNotEnrolledIcon.Image")));
            this.deviceNotEnrolledIcon.Location = new System.Drawing.Point(12, 12);
            this.deviceNotEnrolledIcon.Name = "deviceNotEnrolledIcon";
            this.deviceNotEnrolledIcon.Size = new System.Drawing.Size(48, 48);
            this.deviceNotEnrolledIcon.TabIndex = 6;
            this.deviceNotEnrolledIcon.TabStop = false;
            // 
            // DeviceFlyoutWindow
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(254, 90);
            this.Controls.Add(this.deviceNotEnrolledIcon);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.detailsLink);
            this.Controls.Add(this.enrollmentStateLabel);
            this.Controls.Add(this.deviceEnrolledIcon);
            this.Name = "DeviceFlyoutWindow";
            this.Text = "UserFlyoutWindow";
            ((System.ComponentModel.ISupportInitialize)(this.deviceEnrolledIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deviceNotEnrolledIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox deviceEnrolledIcon;
        private System.Windows.Forms.Label enrollmentStateLabel;
        private System.Windows.Forms.LinkLabel detailsLink;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.PictureBox deviceNotEnrolledIcon;
    }
}