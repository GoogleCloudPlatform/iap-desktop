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

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    partial class AccessInfoFlyoutView
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
            this.dcaLink = new System.Windows.Forms.LinkLabel();
            this.closeButton = new System.Windows.Forms.Button();
            this.headerLabel = new System.Windows.Forms.Label();
            this.pscLabel = new System.Windows.Forms.Label();
            this.pscLink = new System.Windows.Forms.LinkLabel();
            this.dcaLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // dcaLink
            // 
            this.dcaLink.AutoSize = true;
            this.dcaLink.Location = new System.Drawing.Point(150, 52);
            this.dcaLink.Name = "dcaLink";
            this.dcaLink.Size = new System.Drawing.Size(16, 13);
            this.dcaLink.TabIndex = 4;
            this.dcaLink.TabStop = true;
            this.dcaLink.Text = "...";
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.closeButton.Location = new System.Drawing.Point(172, 2);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(24, 24);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "✖";
            this.closeButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(8, 8);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(112, 13);
            this.headerLabel.TabIndex = 6;
            this.headerLabel.Text = "Connection details";
            // 
            // pscLabel
            // 
            this.pscLabel.AutoSize = true;
            this.pscLabel.Location = new System.Drawing.Point(8, 36);
            this.pscLabel.Name = "pscLabel";
            this.pscLabel.Size = new System.Drawing.Size(122, 13);
            this.pscLabel.TabIndex = 7;
            this.pscLabel.Text = "Private Service Connect";
            // 
            // psvValueLabel
            // 
            this.pscLink.AutoSize = true;
            this.pscLink.Location = new System.Drawing.Point(150, 36);
            this.pscLink.Name = "psvValueLabel";
            this.pscLink.Size = new System.Drawing.Size(10, 13);
            this.pscLink.TabIndex = 8;
            this.pscLink.Text = "-";
            // 
            // dcaLabel
            // 
            this.dcaLabel.AutoSize = true;
            this.dcaLabel.Location = new System.Drawing.Point(8, 52);
            this.dcaLabel.Name = "dcaLabel";
            this.dcaLabel.Size = new System.Drawing.Size(123, 13);
            this.dcaLabel.TabIndex = 9;
            this.dcaLabel.Text = "Certificate-based access";
            // 
            // AccessInfoFlyoutView
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(200, 77);
            this.Controls.Add(this.dcaLabel);
            this.Controls.Add(this.pscLink);
            this.Controls.Add(this.pscLabel);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.dcaLink);
            this.Name = "AccessInfoFlyoutView";
            this.Text = "UserFlyoutWindow";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.LinkLabel dcaLink;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.Label pscLabel;
        private System.Windows.Forms.LinkLabel pscLink;
        private System.Windows.Forms.Label dcaLabel;
    }
}