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
    partial class DebugProjectExplorerTrackingView
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
            this.instanceNameLabel = new System.Windows.Forms.Label();
            this.currentInstanceLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // instanceNameLabel
            // 
            this.instanceNameLabel.AutoSize = true;
            this.instanceNameLabel.Location = new System.Drawing.Point(108, 24);
            this.instanceNameLabel.Name = "instanceNameLabel";
            this.instanceNameLabel.Size = new System.Drawing.Size(13, 13);
            this.instanceNameLabel.TabIndex = 0;
            this.instanceNameLabel.Text = "..";
            // 
            // currentInstanceLabel
            // 
            this.currentInstanceLabel.AutoSize = true;
            this.currentInstanceLabel.Location = new System.Drawing.Point(13, 24);
            this.currentInstanceLabel.Name = "currentInstanceLabel";
            this.currentInstanceLabel.Size = new System.Drawing.Size(87, 13);
            this.currentInstanceLabel.TabIndex = 1;
            this.currentInstanceLabel.Text = "Current instance:";
            // 
            // DebugProjectExplorerTrackingWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.currentInstanceLabel);
            this.Controls.Add(this.instanceNameLabel);
            this.Name = "DebugProjectExplorerTrackingWindow";
            this.ShowIcon = false;
            this.Text = "Debug tracking";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label instanceNameLabel;
        private System.Windows.Forms.Label currentInstanceLabel;
    }
}