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

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.ActiveDirectory
{
    partial class JoinView
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
            this.headlineLabel = new Google.Solutions.Mvvm.Controls.HeaderLabel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.domainLabel = new System.Windows.Forms.Label();
            this.domainText = new System.Windows.Forms.TextBox();
            this.computerNameText = new System.Windows.Forms.TextBox();
            this.computerNameLabel = new System.Windows.Forms.Label();
            this.computerNameWarning = new System.Windows.Forms.Label();
            this.domainWarning = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // headlineLabel
            // 
            this.headlineLabel.AutoSize = true;
            this.headlineLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headlineLabel.Location = new System.Drawing.Point(11, 15);
            this.headlineLabel.Name = "headlineLabel";
            this.headlineLabel.Size = new System.Drawing.Size(237, 30);
            this.headlineLabel.TabIndex = 9;
            this.headlineLabel.Text = "Join to Active Directory";
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(54, 217);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(113, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "Restart and join";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(173, 217);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // domainLabel
            // 
            this.domainLabel.AutoSize = true;
            this.domainLabel.Location = new System.Drawing.Point(13, 69);
            this.domainLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.domainLabel.Name = "domainLabel";
            this.domainLabel.Size = new System.Drawing.Size(165, 13);
            this.domainLabel.TabIndex = 13;
            this.domainLabel.Text = "Active Directory domain to join to:";
            // 
            // domainText
            // 
            this.domainText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.domainText.Location = new System.Drawing.Point(16, 89);
            this.domainText.Margin = new System.Windows.Forms.Padding(2);
            this.domainText.MaxLength = 50;
            this.domainText.Multiline = true;
            this.domainText.Name = "domainText";
            this.domainText.Size = new System.Drawing.Size(232, 24);
            this.domainText.TabIndex = 0;
            // 
            // computerNameText
            // 
            this.computerNameText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.computerNameText.Location = new System.Drawing.Point(16, 159);
            this.computerNameText.Margin = new System.Windows.Forms.Padding(2);
            this.computerNameText.MaxLength = 20;
            this.computerNameText.Multiline = true;
            this.computerNameText.Name = "computerNameText";
            this.computerNameText.Size = new System.Drawing.Size(232, 24);
            this.computerNameText.TabIndex = 1;
            // 
            // computerNameLabel
            // 
            this.computerNameLabel.AutoSize = true;
            this.computerNameLabel.Location = new System.Drawing.Point(13, 142);
            this.computerNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.computerNameLabel.Name = "computerNameLabel";
            this.computerNameLabel.Size = new System.Drawing.Size(84, 13);
            this.computerNameLabel.TabIndex = 18;
            this.computerNameLabel.Text = "Computer name:";
            // 
            // computerNameWarning
            // 
            this.computerNameWarning.AutoSize = true;
            this.computerNameWarning.ForeColor = System.Drawing.Color.Red;
            this.computerNameWarning.Location = new System.Drawing.Point(13, 189);
            this.computerNameWarning.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.computerNameWarning.Name = "computerNameWarning";
            this.computerNameWarning.Size = new System.Drawing.Size(204, 13);
            this.computerNameWarning.TabIndex = 19;
            this.computerNameWarning.Text = "The name must not exceed 15 characters";
            // 
            // domainWarning
            // 
            this.domainWarning.AutoSize = true;
            this.domainWarning.ForeColor = System.Drawing.Color.Red;
            this.domainWarning.Location = new System.Drawing.Point(13, 121);
            this.domainWarning.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.domainWarning.Name = "domainWarning";
            this.domainWarning.Size = new System.Drawing.Size(232, 13);
            this.domainWarning.TabIndex = 20;
            this.domainWarning.Text = "Enter the DNS domain name of your AD domain";
            // 
            // JoinView
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(270, 257);
            this.ControlBox = false;
            this.Controls.Add(this.domainWarning);
            this.Controls.Add(this.computerNameWarning);
            this.Controls.Add(this.computerNameLabel);
            this.Controls.Add(this.domainLabel);
            this.Controls.Add(this.computerNameText);
            this.Controls.Add(this.domainText);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.headlineLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "JoinView";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Join to Active Directory";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private HeaderLabel headlineLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label domainLabel;
        private System.Windows.Forms.TextBox domainText;
        private System.Windows.Forms.TextBox computerNameText;
        private System.Windows.Forms.Label computerNameLabel;
        private System.Windows.Forms.Label computerNameWarning;
        private System.Windows.Forms.Label domainWarning;
    }
}