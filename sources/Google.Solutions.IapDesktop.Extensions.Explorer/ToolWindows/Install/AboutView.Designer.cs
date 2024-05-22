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

namespace Google.Solutions.IapDesktop.Extensions.Explorer.Windows.About
{
    partial class AboutView
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
            this.authorLabel = new System.Windows.Forms.Label();
            this.licenseText = new MarkdownViewer();
            this.authorLink = new System.Windows.Forms.LinkLabel();
            this.copyrightLabel = new System.Windows.Forms.Label();
            this.headerLabel = new Google.Solutions.Mvvm.Controls.HeaderLabel();
            this.gradientPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.gradientPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(341, 295);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // infoLabel
            // 
            this.infoLabel.AutoSize = true;
            this.infoLabel.Location = new System.Drawing.Point(15, 70);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(10, 13);
            this.infoLabel.TabIndex = 1;
            this.infoLabel.Text = "-";
            // 
            // authorLabel
            // 
            this.authorLabel.AutoSize = true;
            this.authorLabel.Location = new System.Drawing.Point(288, 70);
            this.authorLabel.Name = "authorLabel";
            this.authorLabel.Size = new System.Drawing.Size(41, 13);
            this.authorLabel.TabIndex = 1;
            this.authorLabel.Text = "Author:";
            this.authorLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // licenseText
            // 
            this.licenseText.BackColor = System.Drawing.Color.White;
            this.licenseText.Location = new System.Drawing.Point(16, 130);
            this.licenseText.Name = "licenseText";
            this.licenseText.Size = new System.Drawing.Size(400, 151);
            this.licenseText.TabIndex = 2;
            this.licenseText.Text = "";
            this.licenseText.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.licenseText_LinkClicked);
            // 
            // authorLink
            // 
            this.authorLink.ActiveLinkColor = System.Drawing.Color.Blue;
            this.authorLink.AutoSize = true;
            this.authorLink.Location = new System.Drawing.Point(328, 70);
            this.authorLink.Name = "authorLink";
            this.authorLink.Size = new System.Drawing.Size(10, 13);
            this.authorLink.TabIndex = 3;
            this.authorLink.TabStop = true;
            this.authorLink.Text = "-";
            // 
            // copyrightLabel
            // 
            this.copyrightLabel.AutoSize = true;
            this.copyrightLabel.Location = new System.Drawing.Point(288, 85);
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Size = new System.Drawing.Size(51, 13);
            this.copyrightLabel.TabIndex = 1;
            this.copyrightLabel.Text = "Copyright";
            this.copyrightLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(11, 15);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(131, 30);
            this.headerLabel.TabIndex = 4;
            this.headerLabel.Text = "IAP Desktop";
            // 
            // gradientPictureBox
            // 
            this.gradientPictureBox.Image = Resources.AccentGradient_450;
            this.gradientPictureBox.Location = new System.Drawing.Point(0, 332);
            this.gradientPictureBox.Name = "gradientPictureBox";
            this.gradientPictureBox.Size = new System.Drawing.Size(450, 10);
            this.gradientPictureBox.TabIndex = 5;
            this.gradientPictureBox.TabStop = false;
            // 
            // AboutView
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.CancelButton = this.okButton;
            this.ClientSize = new System.Drawing.Size(434, 341);
            this.Controls.Add(this.gradientPictureBox);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.authorLink);
            this.Controls.Add(this.licenseText);
            this.Controls.Add(this.copyrightLabel);
            this.Controls.Add(this.authorLabel);
            this.Controls.Add(this.infoLabel);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            ((System.ComponentModel.ISupportInitialize)(this.gradientPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.Label authorLabel;
        private MarkdownViewer licenseText;
        private System.Windows.Forms.LinkLabel authorLink;
        private System.Windows.Forms.Label copyrightLabel;
        private Mvvm.Controls.HeaderLabel headerLabel;
        private System.Windows.Forms.PictureBox gradientPictureBox;
    }
}