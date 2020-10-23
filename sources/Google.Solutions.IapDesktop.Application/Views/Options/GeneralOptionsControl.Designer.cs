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

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    partial class GeneralOptionsControl
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
            this.updateCheckBox = new System.Windows.Forms.GroupBox();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.lastCheckHeaderLabel = new System.Windows.Forms.Label();
            this.enableUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.browserIntegrationBox = new System.Windows.Forms.GroupBox();
            this.enableBrowserIntegrationCheclBox = new System.Windows.Forms.CheckBox();
            this.updateCheckBox.SuspendLayout();
            this.browserIntegrationBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // updateCheckBox
            // 
            this.updateCheckBox.Controls.Add(this.lastCheckLabel);
            this.updateCheckBox.Controls.Add(this.lastCheckHeaderLabel);
            this.updateCheckBox.Controls.Add(this.enableUpdateCheckBox);
            this.updateCheckBox.Location = new System.Drawing.Point(4, 70);
            this.updateCheckBox.Name = "updateCheckBox";
            this.updateCheckBox.Size = new System.Drawing.Size(336, 83);
            this.updateCheckBox.TabIndex = 1;
            this.updateCheckBox.TabStop = false;
            this.updateCheckBox.Text = "Updates";
            // 
            // lastCheckLabel
            // 
            this.lastCheckLabel.AutoSize = true;
            this.lastCheckLabel.Location = new System.Drawing.Point(95, 48);
            this.lastCheckLabel.Name = "lastCheckLabel";
            this.lastCheckLabel.Size = new System.Drawing.Size(10, 13);
            this.lastCheckLabel.TabIndex = 2;
            this.lastCheckLabel.Text = "-";
            // 
            // lastCheckHeaderLabel
            // 
            this.lastCheckHeaderLabel.AutoSize = true;
            this.lastCheckHeaderLabel.Location = new System.Drawing.Point(26, 48);
            this.lastCheckHeaderLabel.Name = "lastCheckHeaderLabel";
            this.lastCheckHeaderLabel.Size = new System.Drawing.Size(63, 13);
            this.lastCheckHeaderLabel.TabIndex = 2;
            this.lastCheckHeaderLabel.Text = "Last check:";
            // 
            // enableUpdateCheckBox
            // 
            this.enableUpdateCheckBox.AutoSize = true;
            this.enableUpdateCheckBox.Location = new System.Drawing.Point(10, 24);
            this.enableUpdateCheckBox.Name = "enableUpdateCheckBox";
            this.enableUpdateCheckBox.Size = new System.Drawing.Size(287, 17);
            this.enableUpdateCheckBox.TabIndex = 1;
            this.enableUpdateCheckBox.Text = "Periodically check for updates when closing application";
            this.enableUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // browserIntegrationBox
            // 
            this.browserIntegrationBox.Controls.Add(this.enableBrowserIntegrationCheclBox);
            this.browserIntegrationBox.Location = new System.Drawing.Point(4, 3);
            this.browserIntegrationBox.Name = "browserIntegrationBox";
            this.browserIntegrationBox.Size = new System.Drawing.Size(336, 61);
            this.browserIntegrationBox.TabIndex = 1;
            this.browserIntegrationBox.TabStop = false;
            this.browserIntegrationBox.Text = "Browser integration";
            // 
            // enableBrowserIntegrationCheclBox
            // 
            this.enableBrowserIntegrationCheclBox.AutoSize = true;
            this.enableBrowserIntegrationCheclBox.Location = new System.Drawing.Point(10, 24);
            this.enableBrowserIntegrationCheclBox.Name = "enableBrowserIntegrationCheclBox";
            this.enableBrowserIntegrationCheclBox.Size = new System.Drawing.Size(234, 17);
            this.enableBrowserIntegrationCheclBox.TabIndex = 1;
            this.enableBrowserIntegrationCheclBox.Text = "Associate IAP Desktop with iap-rdp:/// links";
            this.enableBrowserIntegrationCheclBox.UseVisualStyleBackColor = true;
            // 
            // GeneralOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.browserIntegrationBox);
            this.Controls.Add(this.updateCheckBox);
            this.Name = "GeneralOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.updateCheckBox.ResumeLayout(false);
            this.updateCheckBox.PerformLayout();
            this.browserIntegrationBox.ResumeLayout(false);
            this.browserIntegrationBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox updateCheckBox;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Label lastCheckHeaderLabel;
        private System.Windows.Forms.CheckBox enableUpdateCheckBox;
        private System.Windows.Forms.GroupBox browserIntegrationBox;
        private System.Windows.Forms.CheckBox enableBrowserIntegrationCheclBox;
    }
}
