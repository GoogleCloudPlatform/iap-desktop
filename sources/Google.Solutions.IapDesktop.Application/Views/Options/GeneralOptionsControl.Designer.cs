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
            this.updateBox = new System.Windows.Forms.GroupBox();
            this.updateIcon = new System.Windows.Forms.PictureBox();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.lastCheckHeaderLabel = new System.Windows.Forms.Label();
            this.enableUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.browserIntegrationBox = new System.Windows.Forms.GroupBox();
            this.browserIntegrationLink = new System.Windows.Forms.LinkLabel();
            this.linkIcon = new System.Windows.Forms.PictureBox();
            this.enableBrowserIntegrationCheckBox = new System.Windows.Forms.CheckBox();
            this.secureConnectBox = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.secureConnectLink = new System.Windows.Forms.LinkLabel();
            this.secureConnectIcon = new System.Windows.Forms.PictureBox();
            this.enableDcaCheckBox = new System.Windows.Forms.CheckBox();
            this.updateBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updateIcon)).BeginInit();
            this.browserIntegrationBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.linkIcon)).BeginInit();
            this.secureConnectBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.secureConnectIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // updateBox
            // 
            this.updateBox.Controls.Add(this.updateIcon);
            this.updateBox.Controls.Add(this.lastCheckLabel);
            this.updateBox.Controls.Add(this.lastCheckHeaderLabel);
            this.updateBox.Controls.Add(this.enableUpdateCheckBox);
            this.updateBox.Location = new System.Drawing.Point(4, 261);
            this.updateBox.Name = "updateBox";
            this.updateBox.Size = new System.Drawing.Size(336, 83);
            this.updateBox.TabIndex = 1;
            this.updateBox.TabStop = false;
            this.updateBox.Text = "Updates:";
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
            this.enableUpdateCheckBox.Size = new System.Drawing.Size(252, 22);
            this.enableUpdateCheckBox.TabIndex = 1;
            this.enableUpdateCheckBox.Text = "Periodically check for updates on exit";
            this.enableUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // browserIntegrationBox
            // 
            this.browserIntegrationBox.Controls.Add(this.browserIntegrationLink);
            this.browserIntegrationBox.Controls.Add(this.linkIcon);
            this.browserIntegrationBox.Controls.Add(this.enableBrowserIntegrationCheckBox);
            this.browserIntegrationBox.Location = new System.Drawing.Point(4, 3);
            this.browserIntegrationBox.Name = "browserIntegrationBox";
            this.browserIntegrationBox.Size = new System.Drawing.Size(336, 92);
            this.browserIntegrationBox.TabIndex = 1;
            this.browserIntegrationBox.TabStop = false;
            this.browserIntegrationBox.Text = "Browser integration:";
            // 
            // browserIntegrationLink
            // 
            this.browserIntegrationLink.AutoSize = true;
            this.browserIntegrationLink.Location = new System.Drawing.Point(74, 62);
            this.browserIntegrationLink.Name = "browserIntegrationLink";
            this.browserIntegrationLink.Size = new System.Drawing.Size(85, 13);
            this.browserIntegrationLink.TabIndex = 3;
            this.browserIntegrationLink.TabStop = true;
            this.browserIntegrationLink.Text = "More information";
            this.browserIntegrationLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.browserIntegrationLink_LinkClicked);
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
            // enableBrowserIntegrationCheckBox
            // 
            this.enableBrowserIntegrationCheckBox.AutoSize = true;
            this.enableBrowserIntegrationCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.enableBrowserIntegrationCheckBox.Location = new System.Drawing.Point(58, 24);
            this.enableBrowserIntegrationCheckBox.Name = "enableBrowserIntegrationCheckBox";
            this.enableBrowserIntegrationCheckBox.Size = new System.Drawing.Size(258, 30);
            this.enableBrowserIntegrationCheckBox.TabIndex = 1;
            this.enableBrowserIntegrationCheckBox.Text = "Allow launching IAP Desktop from a web browser\r\nwhen selecting iap-rdp:/// links";
            this.enableBrowserIntegrationCheckBox.UseVisualStyleBackColor = true;
            // 
            // secureConnectBox
            // 
            this.secureConnectBox.Controls.Add(this.label2);
            this.secureConnectBox.Controls.Add(this.label1);
            this.secureConnectBox.Controls.Add(this.secureConnectLink);
            this.secureConnectBox.Controls.Add(this.secureConnectIcon);
            this.secureConnectBox.Controls.Add(this.enableDcaCheckBox);
            this.secureConnectBox.Location = new System.Drawing.Point(4, 101);
            this.secureConnectBox.Name = "secureConnectBox";
            this.secureConnectBox.Size = new System.Drawing.Size(336, 154);
            this.secureConnectBox.TabIndex = 1;
            this.secureConnectBox.TabStop = false;
            this.secureConnectBox.Text = "Endpoint Verification:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(73, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(229, 26);
            this.label2.TabIndex = 4;
            this.label2.Text = "Certificate-based access requires the\r\ncomputer to be enrolled in Endpoint Verifi" +
    "cation";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(74, 124);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(236, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Changes take effect after restarting IAP Desktop";
            // 
            // secureConnectLink
            // 
            this.secureConnectLink.AutoSize = true;
            this.secureConnectLink.Location = new System.Drawing.Point(73, 99);
            this.secureConnectLink.Name = "secureConnectLink";
            this.secureConnectLink.Size = new System.Drawing.Size(85, 13);
            this.secureConnectLink.TabIndex = 3;
            this.secureConnectLink.TabStop = true;
            this.secureConnectLink.Text = "More information";
            this.secureConnectLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.secureConnectLink_LinkClicked);
            // 
            // secureConnectIcon
            // 
            this.secureConnectIcon.Image = ((System.Drawing.Image)(resources.GetObject("secureConnectIcon.Image")));
            this.secureConnectIcon.Location = new System.Drawing.Point(10, 21);
            this.secureConnectIcon.Name = "secureConnectIcon";
            this.secureConnectIcon.Size = new System.Drawing.Size(36, 36);
            this.secureConnectIcon.TabIndex = 2;
            this.secureConnectIcon.TabStop = false;
            // 
            // enableDcaCheckBox
            // 
            this.enableDcaCheckBox.AutoSize = true;
            this.enableDcaCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.enableDcaCheckBox.Location = new System.Drawing.Point(58, 24);
            this.enableDcaCheckBox.Name = "enableDcaCheckBox";
            this.enableDcaCheckBox.Size = new System.Drawing.Size(242, 30);
            this.enableDcaCheckBox.TabIndex = 1;
            this.enableDcaCheckBox.Text = "Secure connections to Google Cloud by using\r\ncertificate-based access if possible" +
    "";
            this.enableDcaCheckBox.UseVisualStyleBackColor = true;
            // 
            // GeneralOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.secureConnectBox);
            this.Controls.Add(this.browserIntegrationBox);
            this.Controls.Add(this.updateBox);
            this.Name = "GeneralOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.updateBox.ResumeLayout(false);
            this.updateBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updateIcon)).EndInit();
            this.browserIntegrationBox.ResumeLayout(false);
            this.browserIntegrationBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.linkIcon)).EndInit();
            this.secureConnectBox.ResumeLayout(false);
            this.secureConnectBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.secureConnectIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox updateBox;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Label lastCheckHeaderLabel;
        private System.Windows.Forms.CheckBox enableUpdateCheckBox;
        private System.Windows.Forms.GroupBox browserIntegrationBox;
        private System.Windows.Forms.CheckBox enableBrowserIntegrationCheckBox;
        private System.Windows.Forms.PictureBox linkIcon;
        private System.Windows.Forms.PictureBox updateIcon;
        private System.Windows.Forms.LinkLabel browserIntegrationLink;
        private System.Windows.Forms.GroupBox secureConnectBox;
        private System.Windows.Forms.LinkLabel secureConnectLink;
        private System.Windows.Forms.PictureBox secureConnectIcon;
        private System.Windows.Forms.CheckBox enableDcaCheckBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}
