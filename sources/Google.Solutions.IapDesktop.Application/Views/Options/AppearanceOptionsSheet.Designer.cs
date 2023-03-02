﻿//
// Copyright 2023 Google LLC
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

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    partial class AppearanceOptionsSheet
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
            this.themeBox = new System.Windows.Forms.GroupBox();
            this.themeInfoLabel = new System.Windows.Forms.Label();
            this.themeLabel = new System.Windows.Forms.Label();
            this.theme = new Google.Solutions.Mvvm.Controls.BindableComboBox();
            this.themeBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // themeBox
            // 
            this.themeBox.Controls.Add(this.themeInfoLabel);
            this.themeBox.Controls.Add(this.themeLabel);
            this.themeBox.Controls.Add(this.theme);
            this.themeBox.Location = new System.Drawing.Point(4, 3);
            this.themeBox.Name = "themeBox";
            this.themeBox.Size = new System.Drawing.Size(336, 85);
            this.themeBox.TabIndex = 0;
            this.themeBox.TabStop = false;
            this.themeBox.Text = "Theme:";
            // 
            // themeInfoLabel
            // 
            this.themeInfoLabel.AutoSize = true;
            this.themeInfoLabel.Location = new System.Drawing.Point(83, 49);
            this.themeInfoLabel.Name = "themeInfoLabel";
            this.themeInfoLabel.Size = new System.Drawing.Size(16, 13);
            this.themeInfoLabel.TabIndex = 4;
            this.themeInfoLabel.Text = "...";
            // 
            // themeLabel
            // 
            this.themeLabel.AutoSize = true;
            this.themeLabel.Location = new System.Drawing.Point(18, 24);
            this.themeLabel.Name = "themeLabel";
            this.themeLabel.Size = new System.Drawing.Size(43, 13);
            this.themeLabel.TabIndex = 2;
            this.themeLabel.Text = "Theme:";
            // 
            // theme
            // 
            this.theme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.theme.FormattingEnabled = true;
            this.theme.Location = new System.Drawing.Point(86, 21);
            this.theme.Name = "theme";
            this.theme.Size = new System.Drawing.Size(154, 21);
            this.theme.TabIndex = 3;
            // 
            // AppearanceOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.themeBox);
            this.Name = "AppearanceOptionsSheet";
            this.Size = new System.Drawing.Size(343, 322);
            this.themeBox.ResumeLayout(false);
            this.themeBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox themeBox;
        private System.Windows.Forms.Label themeLabel;
        private BindableComboBox theme;
        private System.Windows.Forms.Label themeInfoLabel;
    }
}
