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

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    partial class ScreenOptionsSheet
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
            this.fullScreenBox = new System.Windows.Forms.GroupBox();
            this.screensLabel = new System.Windows.Forms.Label();
            this.screenPicker = new Google.Solutions.IapDesktop.Application.Windows.Options.ScreenDevicePicker();
            this.fullScreenBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // fullScreenBox
            // 
            this.fullScreenBox.Controls.Add(this.screensLabel);
            this.fullScreenBox.Controls.Add(this.screenPicker);
            this.fullScreenBox.Location = new System.Drawing.Point(4, 3);
            this.fullScreenBox.Name = "fullScreenBox";
            this.fullScreenBox.Size = new System.Drawing.Size(336, 350);
            this.fullScreenBox.TabIndex = 1;
            this.fullScreenBox.TabStop = false;
            this.fullScreenBox.Text = "Full screen:";
            // 
            // screensLabel
            // 
            this.screensLabel.AutoSize = true;
            this.screensLabel.Location = new System.Drawing.Point(7, 20);
            this.screensLabel.Name = "screensLabel";
            this.screensLabel.Size = new System.Drawing.Size(207, 13);
            this.screensLabel.TabIndex = 4;
            this.screensLabel.Text = "Select displays to use for full-screen mode:";
            // 
            // screenPicker
            // 
            this.screenPicker.Location = new System.Drawing.Point(58, 50);
            this.screenPicker.Name = "screenPicker";
            this.screenPicker.Size = new System.Drawing.Size(261, 281);
            this.screenPicker.TabIndex = 3;
            // 
            // ScreenOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.fullScreenBox);
            this.Name = "ScreenOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.fullScreenBox.ResumeLayout(false);
            this.fullScreenBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox fullScreenBox;
        private System.Windows.Forms.Label screensLabel;
        private ScreenDevicePicker screenPicker;
    }
}
