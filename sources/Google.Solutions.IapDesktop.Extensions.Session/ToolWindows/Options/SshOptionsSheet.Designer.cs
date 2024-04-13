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

using Google.Solutions.Mvvm.Controls;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    partial class SshOptionsSheet
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
            this.authBox = new System.Windows.Forms.GroupBox();
            this.keyTypeLabel = new System.Windows.Forms.Label();
            this.publicKeyType = new BindableComboBox();
            this.daysLabel = new System.Windows.Forms.Label();
            this.validityNoteLabel = new System.Windows.Forms.Label();
            this.publicKeyValidityLabel = new System.Windows.Forms.Label();
            this.publicKeyValidityUpDown = new System.Windows.Forms.NumericUpDown();
            this.connectionBox = new System.Windows.Forms.GroupBox();
            this.propagateLocaleCheckBox = new System.Windows.Forms.CheckBox();
            this.usePersistentKeyCheckBox = new System.Windows.Forms.CheckBox();
            this.authBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.publicKeyValidityUpDown)).BeginInit();
            this.connectionBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // authBox
            // 
            this.authBox.Controls.Add(this.usePersistentKeyCheckBox);
            this.authBox.Controls.Add(this.keyTypeLabel);
            this.authBox.Controls.Add(this.publicKeyType);
            this.authBox.Controls.Add(this.daysLabel);
            this.authBox.Controls.Add(this.validityNoteLabel);
            this.authBox.Controls.Add(this.publicKeyValidityLabel);
            this.authBox.Controls.Add(this.publicKeyValidityUpDown);
            this.authBox.Location = new System.Drawing.Point(4, 3);
            this.authBox.Name = "authBox";
            this.authBox.Size = new System.Drawing.Size(336, 138);
            this.authBox.TabIndex = 0;
            this.authBox.TabStop = false;
            this.authBox.Text = "Public key authentication:";
            // 
            // keyTypeLabel
            // 
            this.keyTypeLabel.AutoSize = true;
            this.keyTypeLabel.Location = new System.Drawing.Point(18, 24);
            this.keyTypeLabel.Name = "keyTypeLabel";
            this.keyTypeLabel.Size = new System.Drawing.Size(51, 13);
            this.keyTypeLabel.TabIndex = 0;
            this.keyTypeLabel.Text = "Key type:";
            // 
            // publicKeyType
            // 
            this.publicKeyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.publicKeyType.FormattingEnabled = true;
            this.publicKeyType.Location = new System.Drawing.Point(75, 21);
            this.publicKeyType.Name = "publicKeyType";
            this.publicKeyType.Size = new System.Drawing.Size(154, 21);
            this.publicKeyType.TabIndex = 1;
            // 
            // daysLabel
            // 
            this.daysLabel.AutoSize = true;
            this.daysLabel.Location = new System.Drawing.Point(231, 80);
            this.daysLabel.Name = "daysLabel";
            this.daysLabel.Size = new System.Drawing.Size(29, 13);
            this.daysLabel.TabIndex = 4;
            this.daysLabel.Text = "days";
            // 
            // validityNoteLabel
            // 
            this.validityNoteLabel.AutoSize = true;
            this.validityNoteLabel.Location = new System.Drawing.Point(36, 108);
            this.validityNoteLabel.Name = "validityNoteLabel";
            this.validityNoteLabel.Size = new System.Drawing.Size(274, 13);
            this.validityNoteLabel.TabIndex = 5;
            this.validityNoteLabel.Text = "IAP Desktop automatically re-publishes keys if necessary";
            // 
            // publicKeyValidityLabel
            // 
            this.publicKeyValidityLabel.AutoSize = true;
            this.publicKeyValidityLabel.Location = new System.Drawing.Point(36, 80);
            this.publicKeyValidityLabel.Name = "publicKeyValidityLabel";
            this.publicKeyValidityLabel.Size = new System.Drawing.Size(115, 13);
            this.publicKeyValidityLabel.TabIndex = 2;
            this.publicKeyValidityLabel.Text = "Let the key expire after";
            // 
            // publicKeyValidityUpDown
            // 
            this.publicKeyValidityUpDown.Location = new System.Drawing.Point(172, 78);
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
            this.publicKeyValidityUpDown.TabIndex = 3;
            this.publicKeyValidityUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // connectionBox
            // 
            this.connectionBox.Controls.Add(this.propagateLocaleCheckBox);
            this.connectionBox.Location = new System.Drawing.Point(4, 148);
            this.connectionBox.Name = "connectionBox";
            this.connectionBox.Size = new System.Drawing.Size(336, 64);
            this.connectionBox.TabIndex = 1;
            this.connectionBox.TabStop = false;
            this.connectionBox.Text = "Environment:";
            // 
            // propagateLocaleCheckBox
            // 
            this.propagateLocaleCheckBox.AutoSize = true;
            this.propagateLocaleCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.propagateLocaleCheckBox.Location = new System.Drawing.Point(18, 24);
            this.propagateLocaleCheckBox.Name = "propagateLocaleCheckBox";
            this.propagateLocaleCheckBox.Size = new System.Drawing.Size(266, 17);
            this.propagateLocaleCheckBox.TabIndex = 0;
            this.propagateLocaleCheckBox.Text = "Use Windows display &language as locale (LC_ALL)";
            this.propagateLocaleCheckBox.UseVisualStyleBackColor = true;
            // 
            // usePersistentKeyCheckBox
            // 
            this.usePersistentKeyCheckBox.AutoSize = true;
            this.usePersistentKeyCheckBox.Location = new System.Drawing.Point(20, 52);
            this.usePersistentKeyCheckBox.Name = "usePersistentKeyCheckBox";
            this.usePersistentKeyCheckBox.Size = new System.Drawing.Size(298, 17);
            this.usePersistentKeyCheckBox.TabIndex = 6;
            this.usePersistentKeyCheckBox.Text = "Use persistent key and store it in Windows CNG key store";
            this.usePersistentKeyCheckBox.UseVisualStyleBackColor = true;
            // 
            // SshOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.connectionBox);
            this.Controls.Add(this.authBox);
            this.Name = "SshOptionsSheet";
            this.Size = new System.Drawing.Size(343, 369);
            this.authBox.ResumeLayout(false);
            this.authBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.publicKeyValidityUpDown)).EndInit();
            this.connectionBox.ResumeLayout(false);
            this.connectionBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox authBox;
        private System.Windows.Forms.NumericUpDown publicKeyValidityUpDown;
        private System.Windows.Forms.Label daysLabel;
        private System.Windows.Forms.Label validityNoteLabel;
        private System.Windows.Forms.Label publicKeyValidityLabel;
        private System.Windows.Forms.Label keyTypeLabel;
        private BindableComboBox publicKeyType;
        private System.Windows.Forms.GroupBox connectionBox;
        private System.Windows.Forms.CheckBox propagateLocaleCheckBox;
        private System.Windows.Forms.CheckBox usePersistentKeyCheckBox;
    }
}
