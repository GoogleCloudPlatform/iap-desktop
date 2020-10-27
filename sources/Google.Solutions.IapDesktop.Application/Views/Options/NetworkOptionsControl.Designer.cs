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
    partial class NetworkOptionsControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkOptionsControl));
            this.proxyBox = new System.Windows.Forms.GroupBox();
            this.useSystemRadioButton = new System.Windows.Forms.RadioButton();
            this.useCustomRadioButton = new System.Windows.Forms.RadioButton();
            this.openProxyControlPanelAppletButton = new System.Windows.Forms.Button();
            this.proxyServerTextBox = new System.Windows.Forms.TextBox();
            this.proxyPortTextBox = new System.Windows.Forms.TextBox();
            this.addressLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.linkIcon = new System.Windows.Forms.PictureBox();
            this.proxyDescriptionLabel = new System.Windows.Forms.Label();
            this.proxyBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.linkIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // proxyBox
            // 
            this.proxyBox.Controls.Add(this.linkIcon);
            this.proxyBox.Controls.Add(this.label2);
            this.proxyBox.Controls.Add(this.proxyDescriptionLabel);
            this.proxyBox.Controls.Add(this.addressLabel);
            this.proxyBox.Controls.Add(this.proxyPortTextBox);
            this.proxyBox.Controls.Add(this.proxyServerTextBox);
            this.proxyBox.Controls.Add(this.openProxyControlPanelAppletButton);
            this.proxyBox.Controls.Add(this.useCustomRadioButton);
            this.proxyBox.Controls.Add(this.useSystemRadioButton);
            this.proxyBox.Location = new System.Drawing.Point(3, 4);
            this.proxyBox.Name = "proxyBox";
            this.proxyBox.Size = new System.Drawing.Size(336, 183);
            this.proxyBox.TabIndex = 2;
            this.proxyBox.TabStop = false;
            this.proxyBox.Text = "Proxy:";
            // 
            // useSystemRadioButton
            // 
            this.useSystemRadioButton.AutoSize = true;
            this.useSystemRadioButton.Location = new System.Drawing.Point(58, 46);
            this.useSystemRadioButton.Name = "useSystemRadioButton";
            this.useSystemRadioButton.Size = new System.Drawing.Size(194, 17);
            this.useSystemRadioButton.TabIndex = 0;
            this.useSystemRadioButton.TabStop = true;
            this.useSystemRadioButton.Text = "Use system settings (recommended)";
            this.useSystemRadioButton.UseVisualStyleBackColor = true;
            // 
            // useCustomRadioButton
            // 
            this.useCustomRadioButton.AutoSize = true;
            this.useCustomRadioButton.Location = new System.Drawing.Point(58, 110);
            this.useCustomRadioButton.Name = "useCustomRadioButton";
            this.useCustomRadioButton.Size = new System.Drawing.Size(151, 17);
            this.useCustomRadioButton.TabIndex = 0;
            this.useCustomRadioButton.TabStop = true;
            this.useCustomRadioButton.Text = "Connect via a proxy server";
            this.useCustomRadioButton.UseVisualStyleBackColor = true;
            // 
            // openProxyControlPanelAppletButton
            // 
            this.openProxyControlPanelAppletButton.Location = new System.Drawing.Point(78, 71);
            this.openProxyControlPanelAppletButton.Name = "openProxyControlPanelAppletButton";
            this.openProxyControlPanelAppletButton.Size = new System.Drawing.Size(75, 23);
            this.openProxyControlPanelAppletButton.TabIndex = 1;
            this.openProxyControlPanelAppletButton.Text = "Settings";
            this.openProxyControlPanelAppletButton.UseVisualStyleBackColor = true;
            this.openProxyControlPanelAppletButton.Click += new System.EventHandler(this.openProxyControlPanelAppletButton_Click);
            // 
            // proxyServerTextBox
            // 
            this.proxyServerTextBox.Location = new System.Drawing.Point(124, 141);
            this.proxyServerTextBox.Name = "proxyServerTextBox";
            this.proxyServerTextBox.Size = new System.Drawing.Size(127, 20);
            this.proxyServerTextBox.TabIndex = 2;
            // 
            // proxyPortTextBox
            // 
            this.proxyPortTextBox.Location = new System.Drawing.Point(261, 141);
            this.proxyPortTextBox.Name = "proxyPortTextBox";
            this.proxyPortTextBox.Size = new System.Drawing.Size(46, 20);
            this.proxyPortTextBox.TabIndex = 3;
            this.proxyPortTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.proxyPortTextBox_KeyPress);
            // 
            // addressLabel
            // 
            this.addressLabel.AutoSize = true;
            this.addressLabel.Location = new System.Drawing.Point(74, 144);
            this.addressLabel.Name = "addressLabel";
            this.addressLabel.Size = new System.Drawing.Size(48, 13);
            this.addressLabel.TabIndex = 3;
            this.addressLabel.Text = "Address:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(251, 144);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(10, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = ":";
            // 
            // linkIcon
            // 
            this.linkIcon.Image = ((System.Drawing.Image)(resources.GetObject("linkIcon.Image")));
            this.linkIcon.Location = new System.Drawing.Point(10, 21);
            this.linkIcon.Name = "linkIcon";
            this.linkIcon.Size = new System.Drawing.Size(36, 36);
            this.linkIcon.TabIndex = 4;
            this.linkIcon.TabStop = false;
            // 
            // proxyDescriptionLabel
            // 
            this.proxyDescriptionLabel.AutoSize = true;
            this.proxyDescriptionLabel.Location = new System.Drawing.Point(58, 24);
            this.proxyDescriptionLabel.Name = "proxyDescriptionLabel";
            this.proxyDescriptionLabel.Size = new System.Drawing.Size(201, 13);
            this.proxyDescriptionLabel.TabIndex = 3;
            this.proxyDescriptionLabel.Text = "Connect to the internet via a proxy server";
            // 
            // NetworkOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.proxyBox);
            this.Name = "NetworkOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.proxyBox.ResumeLayout(false);
            this.proxyBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.linkIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox proxyBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label addressLabel;
        private System.Windows.Forms.TextBox proxyPortTextBox;
        private System.Windows.Forms.TextBox proxyServerTextBox;
        private System.Windows.Forms.Button openProxyControlPanelAppletButton;
        private System.Windows.Forms.RadioButton useCustomRadioButton;
        private System.Windows.Forms.RadioButton useSystemRadioButton;
        private System.Windows.Forms.PictureBox linkIcon;
        private System.Windows.Forms.Label proxyDescriptionLabel;
    }
}
