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

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    partial class SshOptionsControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SshOptionsControl));
            this.authBox = new System.Windows.Forms.GroupBox();
            this.keyTypeLabel = new System.Windows.Forms.Label();
            this.publicKeyType = new System.Windows.Forms.ComboBox();
            this.daysLabel = new System.Windows.Forms.Label();
            this.validityNoteLabel = new System.Windows.Forms.Label();
            this.publicKeyValidityLabel = new System.Windows.Forms.Label();
            this.publicKeyValidityUpDown = new System.Windows.Forms.NumericUpDown();
            this.keyIcon = new System.Windows.Forms.PictureBox();
            this.connectionBox = new System.Windows.Forms.GroupBox();
            this.connectionIcon = new System.Windows.Forms.PictureBox();
            this.propagateLocaleCheckBox = new System.Windows.Forms.CheckBox();
            this.authBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.publicKeyValidityUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.keyIcon)).BeginInit();
            this.connectionBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.connectionIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // authBox
            // 
            this.authBox.Controls.Add(this.keyTypeLabel);
            this.authBox.Controls.Add(this.publicKeyType);
            this.authBox.Controls.Add(this.daysLabel);
            this.authBox.Controls.Add(this.validityNoteLabel);
            this.authBox.Controls.Add(this.publicKeyValidityLabel);
            this.authBox.Controls.Add(this.publicKeyValidityUpDown);
            this.authBox.Controls.Add(this.keyIcon);
            this.authBox.Location = new System.Drawing.Point(5, 4);
            this.authBox.Margin = new System.Windows.Forms.Padding(4);
            this.authBox.Name = "authBox";
            this.authBox.Padding = new System.Windows.Forms.Padding(4);
            this.authBox.Size = new System.Drawing.Size(448, 137);
            this.authBox.TabIndex = 0;
            this.authBox.TabStop = false;
            this.authBox.Text = "Public key authentication:";
            // 
            // keyTypeLabel
            // 
            this.keyTypeLabel.AutoSize = true;
            this.keyTypeLabel.Location = new System.Drawing.Point(73, 30);
            this.keyTypeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.keyTypeLabel.Name = "keyTypeLabel";
            this.keyTypeLabel.Size = new System.Drawing.Size(62, 16);
            this.keyTypeLabel.TabIndex = 6;
            this.keyTypeLabel.Text = "Key type:";
            // 
            // publicKeyType
            // 
            this.publicKeyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.publicKeyType.FormattingEnabled = true;
            this.publicKeyType.Location = new System.Drawing.Point(149, 26);
            this.publicKeyType.Margin = new System.Windows.Forms.Padding(4);
            this.publicKeyType.Name = "publicKeyType";
            this.publicKeyType.Size = new System.Drawing.Size(204, 24);
            this.publicKeyType.TabIndex = 5;
            // 
            // daysLabel
            // 
            this.daysLabel.AutoSize = true;
            this.daysLabel.Location = new System.Drawing.Point(357, 70);
            this.daysLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.daysLabel.Name = "daysLabel";
            this.daysLabel.Size = new System.Drawing.Size(37, 16);
            this.daysLabel.TabIndex = 4;
            this.daysLabel.Text = "days";
            // 
            // validityNoteLabel
            // 
            this.validityNoteLabel.AutoSize = true;
            this.validityNoteLabel.Location = new System.Drawing.Point(73, 97);
            this.validityNoteLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.validityNoteLabel.Name = "validityNoteLabel";
            this.validityNoteLabel.Size = new System.Drawing.Size(348, 16);
            this.validityNoteLabel.TabIndex = 4;
            this.validityNoteLabel.Text = "IAP Desktop automatically re-publishes keys if necessary";
            // 
            // publicKeyValidityLabel
            // 
            this.publicKeyValidityLabel.AutoSize = true;
            this.publicKeyValidityLabel.Location = new System.Drawing.Point(73, 70);
            this.publicKeyValidityLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.publicKeyValidityLabel.Name = "publicKeyValidityLabel";
            this.publicKeyValidityLabel.Size = new System.Drawing.Size(191, 16);
            this.publicKeyValidityLabel.TabIndex = 4;
            this.publicKeyValidityLabel.Text = "Let authorized keys expire after";
            // 
            // publicKeyValidityUpDown
            // 
            this.publicKeyValidityUpDown.Location = new System.Drawing.Point(279, 68);
            this.publicKeyValidityUpDown.Margin = new System.Windows.Forms.Padding(4);
            this.publicKeyValidityUpDown.Maximum = new decimal(new int[] {
            3650,
            0,
            0,
            0});
            this.publicKeyValidityUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.publicKeyValidityUpDown.Name = "publicKeyValidityUpDown";
            this.publicKeyValidityUpDown.Size = new System.Drawing.Size(76, 22);
            this.publicKeyValidityUpDown.TabIndex = 2;
            this.publicKeyValidityUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // keyIcon
            // 
            this.keyIcon.Image = ((System.Drawing.Image)(resources.GetObject("keyIcon.Image")));
            this.keyIcon.Location = new System.Drawing.Point(13, 26);
            this.keyIcon.Margin = new System.Windows.Forms.Padding(4);
            this.keyIcon.Name = "keyIcon";
            this.keyIcon.Size = new System.Drawing.Size(48, 44);
            this.keyIcon.TabIndex = 3;
            this.keyIcon.TabStop = false;
            // 
            // connectionBox
            // 
            this.connectionBox.Controls.Add(this.connectionIcon);
            this.connectionBox.Controls.Add(this.propagateLocaleCheckBox);
            this.connectionBox.Location = new System.Drawing.Point(5, 146);
            this.connectionBox.Margin = new System.Windows.Forms.Padding(4);
            this.connectionBox.Name = "connectionBox";
            this.connectionBox.Padding = new System.Windows.Forms.Padding(4);
            this.connectionBox.Size = new System.Drawing.Size(448, 90);
            this.connectionBox.TabIndex = 1;
            this.connectionBox.TabStop = false;
            this.connectionBox.Text = "Connection:";
            // 
            // connectionIcon
            // 
            this.connectionIcon.Image = ((System.Drawing.Image)(resources.GetObject("connectionIcon.Image")));
            this.connectionIcon.Location = new System.Drawing.Point(13, 26);
            this.connectionIcon.Margin = new System.Windows.Forms.Padding(4);
            this.connectionIcon.Name = "connectionIcon";
            this.connectionIcon.Size = new System.Drawing.Size(48, 44);
            this.connectionIcon.TabIndex = 4;
            this.connectionIcon.TabStop = false;
            // 
            // propagateLocaleCheckBox
            // 
            this.propagateLocaleCheckBox.AutoSize = true;
            this.propagateLocaleCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.propagateLocaleCheckBox.Location = new System.Drawing.Point(73, 30);
            this.propagateLocaleCheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.propagateLocaleCheckBox.Name = "propagateLocaleCheckBox";
            this.propagateLocaleCheckBox.Size = new System.Drawing.Size(331, 20);
            this.propagateLocaleCheckBox.TabIndex = 2;
            this.propagateLocaleCheckBox.Text = "Use Windows display &language as locale (LC_ALL)";
            this.propagateLocaleCheckBox.UseVisualStyleBackColor = true;
            // 
            // SshOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.connectionBox);
            this.Controls.Add(this.authBox);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "SshOptionsControl";
            this.Size = new System.Drawing.Size(457, 454);
            this.authBox.ResumeLayout(false);
            this.authBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.publicKeyValidityUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.keyIcon)).EndInit();
            this.connectionBox.ResumeLayout(false);
            this.connectionBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.connectionIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox authBox;
        private System.Windows.Forms.PictureBox keyIcon;
        private System.Windows.Forms.NumericUpDown publicKeyValidityUpDown;
        private System.Windows.Forms.Label daysLabel;
        private System.Windows.Forms.Label validityNoteLabel;
        private System.Windows.Forms.Label publicKeyValidityLabel;
        private System.Windows.Forms.Label keyTypeLabel;
        private System.Windows.Forms.ComboBox publicKeyType;
        private System.Windows.Forms.GroupBox connectionBox;
        private System.Windows.Forms.PictureBox connectionIcon;
        private System.Windows.Forms.CheckBox propagateLocaleCheckBox;
    }
}
