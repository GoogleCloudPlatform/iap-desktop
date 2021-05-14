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

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    partial class DebugFocusWindow
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
            this.components = new System.ComponentModel.Container();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.focusLabel = new System.Windows.Forms.Label();
            this.focusNameTextBox = new System.Windows.Forms.TextBox();
            this.focusHandleTextBox = new System.Windows.Forms.TextBox();
            this.focusTypeTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 500;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // focusLabel
            // 
            this.focusLabel.AutoSize = true;
            this.focusLabel.Location = new System.Drawing.Point(12, 18);
            this.focusLabel.Name = "focusLabel";
            this.focusLabel.Size = new System.Drawing.Size(74, 13);
            this.focusLabel.TabIndex = 0;
            this.focusLabel.Text = "Focus control:";
            // 
            // focusNameTextBox
            // 
            this.focusNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.focusNameTextBox.Location = new System.Drawing.Point(92, 14);
            this.focusNameTextBox.Name = "focusNameTextBox";
            this.focusNameTextBox.ReadOnly = true;
            this.focusNameTextBox.Size = new System.Drawing.Size(211, 20);
            this.focusNameTextBox.TabIndex = 1;
            // 
            // focusHandleTextBox
            // 
            this.focusHandleTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.focusHandleTextBox.Location = new System.Drawing.Point(92, 40);
            this.focusHandleTextBox.Name = "focusHandleTextBox";
            this.focusHandleTextBox.ReadOnly = true;
            this.focusHandleTextBox.Size = new System.Drawing.Size(211, 20);
            this.focusHandleTextBox.TabIndex = 1;
            // 
            // focusTypeTextBox
            // 
            this.focusTypeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.focusTypeTextBox.Location = new System.Drawing.Point(92, 66);
            this.focusTypeTextBox.Name = "focusTypeTextBox";
            this.focusTypeTextBox.ReadOnly = true;
            this.focusTypeTextBox.Size = new System.Drawing.Size(211, 20);
            this.focusTypeTextBox.TabIndex = 1;
            // 
            // DebugFocusWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 450);
            this.Controls.Add(this.focusTypeTextBox);
            this.Controls.Add(this.focusHandleTextBox);
            this.Controls.Add(this.focusNameTextBox);
            this.Controls.Add(this.focusLabel);
            this.Name = "DebugFocusWindow";
            this.Text = "DebugWindowFocus";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Label focusLabel;
        private System.Windows.Forms.TextBox focusNameTextBox;
        private System.Windows.Forms.TextBox focusHandleTextBox;
        private System.Windows.Forms.TextBox focusTypeTextBox;
    }
}