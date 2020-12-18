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

namespace Google.Solutions.IapDesktop.Application.Views.About
{
    partial class AboutWindow
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
            this.okButton = new System.Windows.Forms.Button();
            this.infoLabel = new System.Windows.Forms.Label();
            this.clrInfoLabel = new System.Windows.Forms.Label();
            this.licenseText = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(317, 235);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // infoLabel
            // 
            this.infoLabel.AutoSize = true;
            this.infoLabel.Location = new System.Drawing.Point(13, 93);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(10, 13);
            this.infoLabel.TabIndex = 1;
            this.infoLabel.Text = "-";
            // 
            // clrInfoLabel
            // 
            this.clrInfoLabel.AutoSize = true;
            this.clrInfoLabel.Location = new System.Drawing.Point(303, 93);
            this.clrInfoLabel.Name = "clrInfoLabel";
            this.clrInfoLabel.Size = new System.Drawing.Size(10, 13);
            this.clrInfoLabel.TabIndex = 1;
            this.clrInfoLabel.Text = "-";
            this.clrInfoLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // licenseText
            // 
            this.licenseText.Location = new System.Drawing.Point(16, 133);
            this.licenseText.Name = "licenseText";
            this.licenseText.ReadOnly = true;
            this.licenseText.Size = new System.Drawing.Size(376, 82);
            this.licenseText.TabIndex = 2;
            this.licenseText.Text = "";
            this.licenseText.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.licenseText_LinkClicked);
            // 
            // AboutWindow
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Google.Solutions.IapDesktop.Application.Properties.Resources.About;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.CancelButton = this.okButton;
            this.ClientSize = new System.Drawing.Size(414, 270);
            this.Controls.Add(this.licenseText);
            this.Controls.Add(this.clrInfoLabel);
            this.Controls.Add(this.infoLabel);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.Label clrInfoLabel;
        private System.Windows.Forms.RichTextBox licenseText;
    }
}