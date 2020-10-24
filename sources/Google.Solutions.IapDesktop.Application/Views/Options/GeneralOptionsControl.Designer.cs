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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralOptionsControl));
            this.updateCheckBox = new System.Windows.Forms.GroupBox();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.lastCheckHeaderLabel = new System.Windows.Forms.Label();
            this.enableUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.browserIntegrationBox = new System.Windows.Forms.GroupBox();
            this.enableBrowserIntegrationCheclBox = new System.Windows.Forms.CheckBox();
            this.linkIcon = new System.Windows.Forms.PictureBox();
            this.updateIcon = new System.Windows.Forms.PictureBox();
            this.browserIntegrationLink = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.updateCheckBox.SuspendLayout();
            this.browserIntegrationBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.linkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.updateIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // updateCheckBox
            // 
            this.updateCheckBox.Controls.Add(this.updateIcon);
            this.updateCheckBox.Controls.Add(this.lastCheckLabel);
            this.updateCheckBox.Controls.Add(this.lastCheckHeaderLabel);
            this.updateCheckBox.Controls.Add(this.enableUpdateCheckBox);
            this.updateCheckBox.Location = new System.Drawing.Point(4, 110);
            this.updateCheckBox.Name = "updateCheckBox";
            this.updateCheckBox.Size = new System.Drawing.Size(336, 83);
            this.updateCheckBox.TabIndex = 1;
            this.updateCheckBox.TabStop = false;
            this.updateCheckBox.Text = "Updates:";
            // 
            // lastCheckLabel
            // 
            this.lastCheckLabel.AutoSize = true;
            this.lastCheckLabel.Location = new System.Drawing.Point(143, 48);
            this.lastCheckLabel.Name = "lastCheckLabel";
            this.lastCheckLabel.Size = new System.Drawing.Size(10, 13);
            this.lastCheckLabel.TabIndex = 2;
            this.lastCheckLabel.Text = "-";
            // 
            // lastCheckHeaderLabel
            // 
            this.lastCheckHeaderLabel.AutoSize = true;
            this.lastCheckHeaderLabel.Location = new System.Drawing.Point(74, 48);
            this.lastCheckHeaderLabel.Name = "lastCheckHeaderLabel";
            this.lastCheckHeaderLabel.Size = new System.Drawing.Size(63, 13);
            this.lastCheckHeaderLabel.TabIndex = 2;
            this.lastCheckHeaderLabel.Text = "Last check:";
            // 
            // enableUpdateCheckBox
            // 
            this.enableUpdateCheckBox.Location = new System.Drawing.Point(58, 23);
            this.enableUpdateCheckBox.Name = "enableUpdateCheckBox";
            this.enableUpdateCheckBox.Size = new System.Drawing.Size(287, 17);
            this.enableUpdateCheckBox.TabIndex = 1;
            this.enableUpdateCheckBox.Text = "Periodically check for updates on exit";
            this.enableUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // browserIntegrationBox
            // 
            this.browserIntegrationBox.Controls.Add(this.label1);
            this.browserIntegrationBox.Controls.Add(this.browserIntegrationLink);
            this.browserIntegrationBox.Controls.Add(this.linkIcon);
            this.browserIntegrationBox.Controls.Add(this.enableBrowserIntegrationCheclBox);
            this.browserIntegrationBox.Location = new System.Drawing.Point(4, 3);
            this.browserIntegrationBox.Name = "browserIntegrationBox";
            this.browserIntegrationBox.Size = new System.Drawing.Size(336, 99);
            this.browserIntegrationBox.TabIndex = 1;
            this.browserIntegrationBox.TabStop = false;
            this.browserIntegrationBox.Text = "Browser integration:";
            // 
            // enableBrowserIntegrationCheclBox
            // 
            this.enableBrowserIntegrationCheclBox.AutoSize = true;
            this.enableBrowserIntegrationCheclBox.Location = new System.Drawing.Point(58, 24);
            this.enableBrowserIntegrationCheclBox.Name = "enableBrowserIntegrationCheclBox";
            this.enableBrowserIntegrationCheclBox.Size = new System.Drawing.Size(258, 17);
            this.enableBrowserIntegrationCheclBox.TabIndex = 1;
            this.enableBrowserIntegrationCheclBox.Text = "Allow launching IAP Desktop from a web browser";
            this.enableBrowserIntegrationCheclBox.UseVisualStyleBackColor = true;
            // 
            // linkIcon
            // 
            this.linkIcon.Image = ((System.Drawing.Image)(resources.GetObject("linkIcon.Image")));
            this.linkIcon.Location = new System.Drawing.Point(10, 21);
            this.linkIcon.Name = "linkIcon";
            this.linkIcon.Size = new System.Drawing.Size(36, 36);
            this.linkIcon.TabIndex = 2;
            this.linkIcon.TabStop = false;
            // 
            // updateIcon
            // 
            this.updateIcon.Image = ((System.Drawing.Image)(resources.GetObject("updateIcon.Image")));
            this.updateIcon.Location = new System.Drawing.Point(10, 21);
            this.updateIcon.Name = "updateIcon";
            this.updateIcon.Size = new System.Drawing.Size(36, 36);
            this.updateIcon.TabIndex = 3;
            this.updateIcon.TabStop = false;
            // 
            // browserIntegrationLink
            // 
            this.browserIntegrationLink.AutoSize = true;
            this.browserIntegrationLink.Location = new System.Drawing.Point(74, 67);
            this.browserIntegrationLink.Name = "browserIntegrationLink";
            this.browserIntegrationLink.Size = new System.Drawing.Size(85, 13);
            this.browserIntegrationLink.TabIndex = 3;
            this.browserIntegrationLink.TabStop = true;
            this.browserIntegrationLink.Text = "More information";
            this.browserIntegrationLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.browserIntegrationLink_LinkClicked);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(75, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(239, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "when selecting iap-rdp:/// links";
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
            ((System.ComponentModel.ISupportInitialize)(this.linkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.updateIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox updateCheckBox;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Label lastCheckHeaderLabel;
        private System.Windows.Forms.CheckBox enableUpdateCheckBox;
        private System.Windows.Forms.GroupBox browserIntegrationBox;
        private System.Windows.Forms.CheckBox enableBrowserIntegrationCheclBox;
        private System.Windows.Forms.PictureBox linkIcon;
        private System.Windows.Forms.PictureBox updateIcon;
        private System.Windows.Forms.LinkLabel browserIntegrationLink;
        private System.Windows.Forms.Label label1;
    }
}
