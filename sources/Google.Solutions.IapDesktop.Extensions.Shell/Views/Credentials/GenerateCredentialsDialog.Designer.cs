//
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

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials
{
    partial class GenerateCredentialsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenerateCredentialsDialog));
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.usernameText = new System.Windows.Forms.TextBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.savePwdNote = new System.Windows.Forms.Label();
            this.statusIcon = new System.Windows.Forms.PictureBox();
            this.headlineLabel = new System.Windows.Forms.Label();
            this.usernameReservedLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.statusIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(263, 211);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 28);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(155, 211);
            this.okButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(100, 28);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // usernameText
            // 
            this.usernameText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.usernameText.Location = new System.Drawing.Point(26, 101);
            this.usernameText.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.usernameText.MaxLength = 20;
            this.usernameText.Multiline = true;
            this.usernameText.Name = "usernameText";
            this.usernameText.Size = new System.Drawing.Size(337, 29);
            this.usernameText.TabIndex = 1;
            this.usernameText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.usernameText_KeyPress);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Location = new System.Drawing.Point(25, 78);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(237, 16);
            this.titleLabel.TabIndex = 4;
            this.titleLabel.Text = "Name of local user to create or update:";
            // 
            // savePwdNote
            // 
            this.savePwdNote.AutoSize = true;
            this.savePwdNote.Location = new System.Drawing.Point(47, 172);
            this.savePwdNote.Name = "savePwdNote";
            this.savePwdNote.Size = new System.Drawing.Size(298, 16);
            this.savePwdNote.TabIndex = 5;
            this.savePwdNote.Text = "Connection settings will be updated automatically";
            // 
            // statusIcon
            // 
            this.statusIcon.Image = ((System.Drawing.Image)(resources.GetObject("statusIcon.Image")));
            this.statusIcon.Location = new System.Drawing.Point(26, 171);
            this.statusIcon.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.statusIcon.Name = "statusIcon";
            this.statusIcon.Size = new System.Drawing.Size(31, 30);
            this.statusIcon.TabIndex = 6;
            this.statusIcon.TabStop = false;
            // 
            // headlineLabel
            // 
            this.headlineLabel.AutoSize = true;
            this.headlineLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headlineLabel.Location = new System.Drawing.Point(19, 18);
            this.headlineLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.headlineLabel.Name = "headlineLabel";
            this.headlineLabel.Size = new System.Drawing.Size(337, 37);
            this.headlineLabel.TabIndex = 8;
            this.headlineLabel.Text = "Generate logon credentials";
            // 
            // usernameReservedLabel
            // 
            this.usernameReservedLabel.AutoSize = true;
            this.usernameReservedLabel.ForeColor = System.Drawing.Color.Red;
            this.usernameReservedLabel.Location = new System.Drawing.Point(22, 138);
            this.usernameReservedLabel.Name = "usernameReservedLabel";
            this.usernameReservedLabel.Size = new System.Drawing.Size(287, 16);
            this.usernameReservedLabel.TabIndex = 5;
            this.usernameReservedLabel.Text = "This username is reserved and cannot be used";
            // 
            // GenerateCredentialsDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(394, 267);
            this.ControlBox = false;
            this.Controls.Add(this.usernameReservedLabel);
            this.Controls.Add(this.savePwdNote);
            this.Controls.Add(this.headlineLabel);
            this.Controls.Add(this.statusIcon);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.usernameText);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GenerateCredentialsDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Generate logon credentials";
            ((System.ComponentModel.ISupportInitialize)(this.statusIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.TextBox usernameText;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label savePwdNote;
        private System.Windows.Forms.PictureBox statusIcon;
        private System.Windows.Forms.Label headlineLabel;
        private System.Windows.Forms.Label usernameReservedLabel;
    }
}