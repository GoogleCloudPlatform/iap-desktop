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

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    partial class DebugJobServiceView
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
            this.slowOpButton = new System.Windows.Forms.Button();
            this.slowNonCanelOpButton = new System.Windows.Forms.Button();
            this.throwExceptionButton = new System.Windows.Forms.Button();
            this.label = new System.Windows.Forms.Label();
            this.reauthButton = new System.Windows.Forms.Button();
            this.runInBackgroundCheckBox = new System.Windows.Forms.CheckBox();
            this.spinner = new Google.Solutions.Mvvm.Controls.CircularProgressBar();
            this.SuspendLayout();
            // 
            // slowOpButton
            // 
            this.slowOpButton.Location = new System.Drawing.Point(22, 24);
            this.slowOpButton.Name = "slowOpButton";
            this.slowOpButton.Size = new System.Drawing.Size(172, 23);
            this.slowOpButton.TabIndex = 0;
            this.slowOpButton.Text = "Fire slow canellable event";
            this.slowOpButton.UseVisualStyleBackColor = true;
            // 
            // slowNonCanelOpButton
            // 
            this.slowNonCanelOpButton.Location = new System.Drawing.Point(22, 53);
            this.slowNonCanelOpButton.Name = "slowNonCanelOpButton";
            this.slowNonCanelOpButton.Size = new System.Drawing.Size(172, 23);
            this.slowNonCanelOpButton.TabIndex = 0;
            this.slowNonCanelOpButton.Text = "Fire slow non-canellable event";
            this.slowNonCanelOpButton.UseVisualStyleBackColor = true;
            // 
            // throwExceptionButton
            // 
            this.throwExceptionButton.Location = new System.Drawing.Point(22, 82);
            this.throwExceptionButton.Name = "throwExceptionButton";
            this.throwExceptionButton.Size = new System.Drawing.Size(172, 23);
            this.throwExceptionButton.TabIndex = 0;
            this.throwExceptionButton.Text = "Throw exception";
            this.throwExceptionButton.UseVisualStyleBackColor = true;
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(19, 9);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(19, 13);
            this.label.TabIndex = 4;
            this.label.Text = " ...";
            // 
            // reauthButton
            // 
            this.reauthButton.Location = new System.Drawing.Point(22, 111);
            this.reauthButton.Name = "reauthButton";
            this.reauthButton.Size = new System.Drawing.Size(172, 23);
            this.reauthButton.TabIndex = 0;
            this.reauthButton.Text = "Trigger reauth";
            this.reauthButton.UseVisualStyleBackColor = true;
            // 
            // runInBackgroundCheckBox
            // 
            this.runInBackgroundCheckBox.AutoSize = true;
            this.runInBackgroundCheckBox.Location = new System.Drawing.Point(22, 141);
            this.runInBackgroundCheckBox.Name = "runInBackgroundCheckBox";
            this.runInBackgroundCheckBox.Size = new System.Drawing.Size(117, 17);
            this.runInBackgroundCheckBox.TabIndex = 6;
            this.runInBackgroundCheckBox.Text = "Run in background";
            this.runInBackgroundCheckBox.UseVisualStyleBackColor = true;
            // 
            // circularProgressBar1
            // 
            this.spinner.Indeterminate = true;
            this.spinner.LineWidth = 5;
            this.spinner.Location = new System.Drawing.Point(200, 24);
            this.spinner.Maximum = 100;
            this.spinner.MinimumSize = new System.Drawing.Size(15, 15);
            this.spinner.Name = "circularProgressBar1";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.Speed = 1;
            this.spinner.TabIndex = 7;
            this.spinner.Text = "circularProgressBar";
            this.spinner.Value = 0;
            // 
            // DebugJobServiceView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(541, 451);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.runInBackgroundCheckBox);
            this.Controls.Add(this.label);
            this.Controls.Add(this.reauthButton);
            this.Controls.Add(this.throwExceptionButton);
            this.Controls.Add(this.slowNonCanelOpButton);
            this.Controls.Add(this.slowOpButton);
            this.Name = "DebugJobServiceView";
            this.Text = "Debug JobService";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button slowOpButton;
        private System.Windows.Forms.Button slowNonCanelOpButton;
        private System.Windows.Forms.Button throwExceptionButton;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Button reauthButton;
        private System.Windows.Forms.CheckBox runInBackgroundCheckBox;
        private Mvvm.Controls.CircularProgressBar spinner;
    }
}