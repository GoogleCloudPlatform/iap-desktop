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
            this.connectionBox = new System.Windows.Forms.GroupBox();
            this.propagateLocaleCheckBox = new System.Windows.Forms.CheckBox();
            this.keyboardIcon = new System.Windows.Forms.PictureBox();
            this.publicKeyValidityUpDown = new System.Windows.Forms.NumericUpDown();
            this.publicKeyValidityLabel = new System.Windows.Forms.Label();
            this.daysLabel = new System.Windows.Forms.Label();
            this.validityNoteLabel = new System.Windows.Forms.Label();
            this.connectionBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keyboardIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.publicKeyValidityUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // connectionBox
            // 
            this.connectionBox.Controls.Add(this.daysLabel);
            this.connectionBox.Controls.Add(this.validityNoteLabel);
            this.connectionBox.Controls.Add(this.publicKeyValidityLabel);
            this.connectionBox.Controls.Add(this.publicKeyValidityUpDown);
            this.connectionBox.Controls.Add(this.propagateLocaleCheckBox);
            this.connectionBox.Controls.Add(this.keyboardIcon);
            this.connectionBox.Location = new System.Drawing.Point(4, 3);
            this.connectionBox.Name = "connectionBox";
            this.connectionBox.Size = new System.Drawing.Size(336, 109);
            this.connectionBox.TabIndex = 0;
            this.connectionBox.TabStop = false;
            this.connectionBox.Text = "Connection:";
            // 
            // propagateLocaleCheckBox
            // 
            this.propagateLocaleCheckBox.AutoSize = true;
            this.propagateLocaleCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.propagateLocaleCheckBox.Location = new System.Drawing.Point(58, 22);
            this.propagateLocaleCheckBox.Name = "propagateLocaleCheckBox";
            this.propagateLocaleCheckBox.Size = new System.Drawing.Size(266, 17);
            this.propagateLocaleCheckBox.TabIndex = 1;
            this.propagateLocaleCheckBox.Text = "Use Windows display &language as locale (LC_ALL)";
            this.propagateLocaleCheckBox.UseVisualStyleBackColor = true;
            // 
            // keyboardIcon
            // 
            this.keyboardIcon.Image = ((System.Drawing.Image)(resources.GetObject("keyboardIcon.Image")));
            this.keyboardIcon.Location = new System.Drawing.Point(10, 21);
            this.keyboardIcon.Name = "keyboardIcon";
            this.keyboardIcon.Size = new System.Drawing.Size(36, 36);
            this.keyboardIcon.TabIndex = 3;
            this.keyboardIcon.TabStop = false;
            // 
            // publicKeyValidityUpDown
            // 
            this.publicKeyValidityUpDown.Location = new System.Drawing.Point(209, 55);
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
            this.publicKeyValidityUpDown.Size = new System.Drawing.Size(57, 20);
            this.publicKeyValidityUpDown.TabIndex = 2;
            this.publicKeyValidityUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // publicKeyValidityLabel
            // 
            this.publicKeyValidityLabel.AutoSize = true;
            this.publicKeyValidityLabel.Location = new System.Drawing.Point(55, 57);
            this.publicKeyValidityLabel.Name = "publicKeyValidityLabel";
            this.publicKeyValidityLabel.Size = new System.Drawing.Size(154, 13);
            this.publicKeyValidityLabel.TabIndex = 4;
            this.publicKeyValidityLabel.Text = "Let authorized keys expire after";
            // 
            // daysLabel
            // 
            this.daysLabel.AutoSize = true;
            this.daysLabel.Location = new System.Drawing.Point(268, 57);
            this.daysLabel.Name = "daysLabel";
            this.daysLabel.Size = new System.Drawing.Size(29, 13);
            this.daysLabel.TabIndex = 4;
            this.daysLabel.Text = "days";
            // 
            // validityNoteLabel
            // 
            this.validityNoteLabel.AutoSize = true;
            this.validityNoteLabel.Location = new System.Drawing.Point(55, 79);
            this.validityNoteLabel.Name = "validityNoteLabel";
            this.validityNoteLabel.Size = new System.Drawing.Size(274, 13);
            this.validityNoteLabel.TabIndex = 4;
            this.validityNoteLabel.Text = "IAP Desktop automatically re-publishes keys if necessary";
            // 
            // SshOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.connectionBox);
            this.Name = "SshOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.connectionBox.ResumeLayout(false);
            this.connectionBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keyboardIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.publicKeyValidityUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox connectionBox;
        private System.Windows.Forms.PictureBox keyboardIcon;
        private System.Windows.Forms.CheckBox propagateLocaleCheckBox;
        private System.Windows.Forms.NumericUpDown publicKeyValidityUpDown;
        private System.Windows.Forms.Label daysLabel;
        private System.Windows.Forms.Label validityNoteLabel;
        private System.Windows.Forms.Label publicKeyValidityLabel;
    }
}
