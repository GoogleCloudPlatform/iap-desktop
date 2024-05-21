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
    partial class NetworkOptionsSheet
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
            this.proxyBox = new System.Windows.Forms.GroupBox();
            this.proxyAuthCheckBox = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.proxyDescriptionLabel = new System.Windows.Forms.Label();
            this.proxyAuthPasswordLabel = new System.Windows.Forms.Label();
            this.proxyAuthUsernameLabel = new System.Windows.Forms.Label();
            this.pacAddressLabel = new System.Windows.Forms.Label();
            this.addressLabel = new System.Windows.Forms.Label();
            this.proxyPortTextBox = new System.Windows.Forms.TextBox();
            this.proxyAuthPasswordTextBox = new System.Windows.Forms.TextBox();
            this.proxyAuthUsernameTextBox = new System.Windows.Forms.TextBox();
            this.proxyPacTextBox = new System.Windows.Forms.TextBox();
            this.proxyServerTextBox = new System.Windows.Forms.TextBox();
            this.openProxyControlPanelAppletButton = new System.Windows.Forms.Button();
            this.usePacRadioButton = new System.Windows.Forms.RadioButton();
            this.useCustomRadioButton = new System.Windows.Forms.RadioButton();
            this.useSystemRadioButton = new System.Windows.Forms.RadioButton();
            this.proxyBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // proxyBox
            // 
            this.proxyBox.Controls.Add(this.proxyAuthCheckBox);
            this.proxyBox.Controls.Add(this.label2);
            this.proxyBox.Controls.Add(this.proxyDescriptionLabel);
            this.proxyBox.Controls.Add(this.proxyAuthPasswordLabel);
            this.proxyBox.Controls.Add(this.proxyAuthUsernameLabel);
            this.proxyBox.Controls.Add(this.pacAddressLabel);
            this.proxyBox.Controls.Add(this.addressLabel);
            this.proxyBox.Controls.Add(this.proxyPortTextBox);
            this.proxyBox.Controls.Add(this.proxyAuthPasswordTextBox);
            this.proxyBox.Controls.Add(this.proxyAuthUsernameTextBox);
            this.proxyBox.Controls.Add(this.proxyPacTextBox);
            this.proxyBox.Controls.Add(this.proxyServerTextBox);
            this.proxyBox.Controls.Add(this.openProxyControlPanelAppletButton);
            this.proxyBox.Controls.Add(this.usePacRadioButton);
            this.proxyBox.Controls.Add(this.useCustomRadioButton);
            this.proxyBox.Controls.Add(this.useSystemRadioButton);
            this.proxyBox.Location = new System.Drawing.Point(3, 4);
            this.proxyBox.Name = "proxyBox";
            this.proxyBox.Size = new System.Drawing.Size(336, 327);
            this.proxyBox.TabIndex = 0;
            this.proxyBox.TabStop = false;
            this.proxyBox.Text = "Proxy:";
            // 
            // proxyAuthCheckBox
            // 
            this.proxyAuthCheckBox.AutoSize = true;
            this.proxyAuthCheckBox.Location = new System.Drawing.Point(20, 244);
            this.proxyAuthCheckBox.Name = "proxyAuthCheckBox";
            this.proxyAuthCheckBox.Size = new System.Drawing.Size(194, 17);
            this.proxyAuthCheckBox.TabIndex = 11;
            this.proxyAuthCheckBox.Text = "Proxy server requires authentication";
            this.proxyAuthCheckBox.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(241, 144);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(10, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = ":";
            // 
            // proxyDescriptionLabel
            // 
            this.proxyDescriptionLabel.AutoSize = true;
            this.proxyDescriptionLabel.Location = new System.Drawing.Point(18, 24);
            this.proxyDescriptionLabel.Name = "proxyDescriptionLabel";
            this.proxyDescriptionLabel.Size = new System.Drawing.Size(201, 13);
            this.proxyDescriptionLabel.TabIndex = 0;
            this.proxyDescriptionLabel.Text = "Connect to the internet via a proxy server";
            // 
            // proxyAuthPasswordLabel
            // 
            this.proxyAuthPasswordLabel.AutoSize = true;
            this.proxyAuthPasswordLabel.Location = new System.Drawing.Point(36, 296);
            this.proxyAuthPasswordLabel.Name = "proxyAuthPasswordLabel";
            this.proxyAuthPasswordLabel.Size = new System.Drawing.Size(56, 13);
            this.proxyAuthPasswordLabel.TabIndex = 14;
            this.proxyAuthPasswordLabel.Text = "Password:";
            // 
            // proxyAuthUsernameLabel
            // 
            this.proxyAuthUsernameLabel.AutoSize = true;
            this.proxyAuthUsernameLabel.Location = new System.Drawing.Point(36, 270);
            this.proxyAuthUsernameLabel.Name = "proxyAuthUsernameLabel";
            this.proxyAuthUsernameLabel.Size = new System.Drawing.Size(58, 13);
            this.proxyAuthUsernameLabel.TabIndex = 12;
            this.proxyAuthUsernameLabel.Text = "Username:";
            // 
            // pacAddressLabel
            // 
            this.pacAddressLabel.AutoSize = true;
            this.pacAddressLabel.Location = new System.Drawing.Point(37, 210);
            this.pacAddressLabel.Name = "pacAddressLabel";
            this.pacAddressLabel.Size = new System.Drawing.Size(48, 13);
            this.pacAddressLabel.TabIndex = 9;
            this.pacAddressLabel.Text = "Address:";
            // 
            // addressLabel
            // 
            this.addressLabel.AutoSize = true;
            this.addressLabel.Location = new System.Drawing.Point(36, 144);
            this.addressLabel.Name = "addressLabel";
            this.addressLabel.Size = new System.Drawing.Size(48, 13);
            this.addressLabel.TabIndex = 4;
            this.addressLabel.Text = "Address:";
            // 
            // proxyPortTextBox
            // 
            this.proxyPortTextBox.Location = new System.Drawing.Point(251, 141);
            this.proxyPortTextBox.Name = "proxyPortTextBox";
            this.proxyPortTextBox.Size = new System.Drawing.Size(49, 20);
            this.proxyPortTextBox.TabIndex = 7;
            this.proxyPortTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.proxyPortTextBox_KeyPress);
            // 
            // proxyAuthPasswordTextBox
            // 
            this.proxyAuthPasswordTextBox.Location = new System.Drawing.Point(100, 293);
            this.proxyAuthPasswordTextBox.MaxLength = 64;
            this.proxyAuthPasswordTextBox.Name = "proxyAuthPasswordTextBox";
            this.proxyAuthPasswordTextBox.PasswordChar = '●';
            this.proxyAuthPasswordTextBox.Size = new System.Drawing.Size(200, 20);
            this.proxyAuthPasswordTextBox.TabIndex = 15;
            // 
            // proxyAuthUsernameTextBox
            // 
            this.proxyAuthUsernameTextBox.Location = new System.Drawing.Point(100, 267);
            this.proxyAuthUsernameTextBox.MaxLength = 64;
            this.proxyAuthUsernameTextBox.Name = "proxyAuthUsernameTextBox";
            this.proxyAuthUsernameTextBox.Size = new System.Drawing.Size(200, 20);
            this.proxyAuthUsernameTextBox.TabIndex = 13;
            // 
            // proxyPacTextBox
            // 
            this.proxyPacTextBox.Location = new System.Drawing.Point(100, 207);
            this.proxyPacTextBox.MaxLength = 256;
            this.proxyPacTextBox.Name = "proxyPacTextBox";
            this.proxyPacTextBox.Size = new System.Drawing.Size(200, 20);
            this.proxyPacTextBox.TabIndex = 10;
            // 
            // proxyServerTextBox
            // 
            this.proxyServerTextBox.Location = new System.Drawing.Point(100, 141);
            this.proxyServerTextBox.MaxLength = 64;
            this.proxyServerTextBox.Name = "proxyServerTextBox";
            this.proxyServerTextBox.Size = new System.Drawing.Size(140, 20);
            this.proxyServerTextBox.TabIndex = 5;
            // 
            // openProxyControlPanelAppletButton
            // 
            this.openProxyControlPanelAppletButton.Location = new System.Drawing.Point(40, 71);
            this.openProxyControlPanelAppletButton.Name = "openProxyControlPanelAppletButton";
            this.openProxyControlPanelAppletButton.Size = new System.Drawing.Size(75, 23);
            this.openProxyControlPanelAppletButton.TabIndex = 2;
            this.openProxyControlPanelAppletButton.Text = "Settings";
            this.openProxyControlPanelAppletButton.UseVisualStyleBackColor = true;
            this.openProxyControlPanelAppletButton.Click += new System.EventHandler(this.openProxyControlPanelAppletButton_Click);
            // 
            // usePacRadioButton
            // 
            this.usePacRadioButton.AutoSize = true;
            this.usePacRadioButton.Location = new System.Drawing.Point(20, 176);
            this.usePacRadioButton.Name = "usePacRadioButton";
            this.usePacRadioButton.Size = new System.Drawing.Size(185, 17);
            this.usePacRadioButton.TabIndex = 8;
            this.usePacRadioButton.TabStop = true;
            this.usePacRadioButton.Text = "Use automatic configuration script";
            this.usePacRadioButton.UseVisualStyleBackColor = true;
            // 
            // useCustomRadioButton
            // 
            this.useCustomRadioButton.AutoSize = true;
            this.useCustomRadioButton.Location = new System.Drawing.Point(20, 110);
            this.useCustomRadioButton.Name = "useCustomRadioButton";
            this.useCustomRadioButton.Size = new System.Drawing.Size(151, 17);
            this.useCustomRadioButton.TabIndex = 3;
            this.useCustomRadioButton.TabStop = true;
            this.useCustomRadioButton.Text = "Connect via a proxy server";
            this.useCustomRadioButton.UseVisualStyleBackColor = true;
            // 
            // useSystemRadioButton
            // 
            this.useSystemRadioButton.AutoSize = true;
            this.useSystemRadioButton.Location = new System.Drawing.Point(20, 46);
            this.useSystemRadioButton.Name = "useSystemRadioButton";
            this.useSystemRadioButton.Size = new System.Drawing.Size(194, 17);
            this.useSystemRadioButton.TabIndex = 1;
            this.useSystemRadioButton.TabStop = true;
            this.useSystemRadioButton.Text = "Use system settings (recommended)";
            this.useSystemRadioButton.UseVisualStyleBackColor = true;
            // 
            // NetworkOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.proxyBox);
            this.Name = "NetworkOptionsSheet";
            this.Size = new System.Drawing.Size(343, 425);
            this.proxyBox.ResumeLayout(false);
            this.proxyBox.PerformLayout();
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
        private System.Windows.Forms.Label proxyDescriptionLabel;
        private System.Windows.Forms.CheckBox proxyAuthCheckBox;
        private System.Windows.Forms.Label proxyAuthPasswordLabel;
        private System.Windows.Forms.Label proxyAuthUsernameLabel;
        private System.Windows.Forms.TextBox proxyAuthPasswordTextBox;
        private System.Windows.Forms.TextBox proxyAuthUsernameTextBox;
        private System.Windows.Forms.Label pacAddressLabel;
        private System.Windows.Forms.TextBox proxyPacTextBox;
        private System.Windows.Forms.RadioButton usePacRadioButton;
    }
}
