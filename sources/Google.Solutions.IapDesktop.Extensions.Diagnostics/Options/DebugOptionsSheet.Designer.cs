//
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

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.Options
{
    partial class DebugOptionsSheet
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
            this.failToApplyChangesCheckBox = new System.Windows.Forms.CheckBox();
            this.dirtyCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // failToApplyChangesCheckBox
            // 
            this.failToApplyChangesCheckBox.AutoSize = true;
            this.failToApplyChangesCheckBox.Location = new System.Drawing.Point(19, 47);
            this.failToApplyChangesCheckBox.Name = "failToApplyChangesCheckBox";
            this.failToApplyChangesCheckBox.Size = new System.Drawing.Size(126, 17);
            this.failToApplyChangesCheckBox.TabIndex = 0;
            this.failToApplyChangesCheckBox.Text = "Fail to apply changes";
            this.failToApplyChangesCheckBox.UseVisualStyleBackColor = true;
            // 
            // dirtyCheckBox
            // 
            this.dirtyCheckBox.AutoSize = true;
            this.dirtyCheckBox.Location = new System.Drawing.Point(19, 24);
            this.dirtyCheckBox.Name = "dirtyCheckBox";
            this.dirtyCheckBox.Size = new System.Drawing.Size(47, 17);
            this.dirtyCheckBox.TabIndex = 1;
            this.dirtyCheckBox.Text = "Dirty";
            this.dirtyCheckBox.UseVisualStyleBackColor = true;
            // 
            // DebugOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.dirtyCheckBox);
            this.Controls.Add(this.failToApplyChangesCheckBox);
            this.Name = "DebugOptionsSheet";
            this.Size = new System.Drawing.Size(269, 167);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox failToApplyChangesCheckBox;
        private System.Windows.Forms.CheckBox dirtyCheckBox;
    }
}
