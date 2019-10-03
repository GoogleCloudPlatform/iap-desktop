﻿//
// Copyright 2019 Google LLC
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

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    partial class AuthorizeDialog
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
            this.signInButton = new System.Windows.Forms.Button();
            this.signInLabel = new System.Windows.Forms.Label();
            this.spinner = new System.Windows.Forms.PictureBox();
            this.useGcloudLink = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).BeginInit();
            this.SuspendLayout();
            // 
            // signInButton
            // 
            this.signInButton.Image = global::Google.Solutions.CloudIap.Plugin.Properties.Resources.btn_google_signin_dark_normal_web;
            this.signInButton.Location = new System.Drawing.Point(101, 128);
            this.signInButton.Name = "signInButton";
            this.signInButton.Size = new System.Drawing.Size(191, 46);
            this.signInButton.TabIndex = 0;
            this.signInButton.UseVisualStyleBackColor = true;
            this.signInButton.Visible = false;
            // 
            // signInLabel
            // 
            this.signInLabel.AutoSize = true;
            this.signInLabel.BackColor = System.Drawing.Color.White;
            this.signInLabel.Location = new System.Drawing.Point(38, 79);
            this.signInLabel.Name = "signInLabel";
            this.signInLabel.Size = new System.Drawing.Size(322, 13);
            this.signInLabel.TabIndex = 1;
            this.signInLabel.Text = "Sign in to your Google Cloud account to access your VM instances";
            this.signInLabel.Visible = false;
            // 
            // spinner
            // 
            this.spinner.BackColor = System.Drawing.Color.White;
            this.spinner.Image = global::Google.Solutions.CloudIap.Plugin.Properties.Resources.Spinner;
            this.spinner.Location = new System.Drawing.Point(173, 106);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.TabIndex = 2;
            this.spinner.TabStop = false;
            // 
            // useGcloudLink
            // 
            this.useGcloudLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.useGcloudLink.AutoEllipsis = true;
            this.useGcloudLink.BackColor = System.Drawing.Color.White;
            this.useGcloudLink.Location = new System.Drawing.Point(101, 228);
            this.useGcloudLink.Name = "useGcloudLink";
            this.useGcloudLink.Size = new System.Drawing.Size(287, 13);
            this.useGcloudLink.TabIndex = 3;
            this.useGcloudLink.TabStop = true;
            this.useGcloudLink.Text = "Use my gcloud credentials";
            this.useGcloudLink.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.useGcloudLink.Visible = false;
            // 
            // AuthorizeDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Google.Solutions.CloudIap.Plugin.Properties.Resources.SignonScreen;
            this.ClientSize = new System.Drawing.Size(400, 250);
            this.Controls.Add(this.useGcloudLink);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.signInLabel);
            this.Controls.Add(this.signInButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AuthorizeDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sign in";
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button signInButton;
        private System.Windows.Forms.Label signInLabel;
        private System.Windows.Forms.PictureBox spinner;
        private System.Windows.Forms.LinkLabel useGcloudLink;
    }
}