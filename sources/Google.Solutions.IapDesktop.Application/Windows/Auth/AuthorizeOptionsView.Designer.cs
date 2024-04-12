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

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    partial class AuthorizeOptionsView
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
            this.headerLabel = new Google.Solutions.Mvvm.Controls.HeaderLabel();
            this.groupPnel = new System.Windows.Forms.Panel();
            this.wifProviderIdTextBox = new System.Windows.Forms.TextBox();
            this.wifProviderIdLabel = new System.Windows.Forms.Label();
            this.wifPoolIdTextBox = new System.Windows.Forms.TextBox();
            this.wifPoolIdLabel = new System.Windows.Forms.Label();
            this.wifLocationIdTextBox = new System.Windows.Forms.TextBox();
            this.wifLocationIdLabel = new System.Windows.Forms.Label();
            this.workforceIdentityRadioButton = new System.Windows.Forms.RadioButton();
            this.gaiaRadioButton = new System.Windows.Forms.RadioButton();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.groupPnel.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(16, 16);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(163, 30);
            this.headerLabel.TabIndex = 0;
            this.headerLabel.Text = "Sign-in method";
            // 
            // groupPnel
            // 
            this.groupPnel.Controls.Add(this.wifProviderIdTextBox);
            this.groupPnel.Controls.Add(this.wifProviderIdLabel);
            this.groupPnel.Controls.Add(this.wifPoolIdTextBox);
            this.groupPnel.Controls.Add(this.wifPoolIdLabel);
            this.groupPnel.Controls.Add(this.wifLocationIdTextBox);
            this.groupPnel.Controls.Add(this.wifLocationIdLabel);
            this.groupPnel.Controls.Add(this.workforceIdentityRadioButton);
            this.groupPnel.Controls.Add(this.gaiaRadioButton);
            this.groupPnel.Location = new System.Drawing.Point(0, 52);
            this.groupPnel.Name = "groupPnel";
            this.groupPnel.Size = new System.Drawing.Size(268, 168);
            this.groupPnel.TabIndex = 1;
            this.groupPnel.Text = "Identity provider:";
            // 
            // wifProviderIdTextBox
            // 
            this.wifProviderIdTextBox.Location = new System.Drawing.Point(108, 132);
            this.wifProviderIdTextBox.Name = "wifProviderIdTextBox";
            this.wifProviderIdTextBox.Size = new System.Drawing.Size(140, 20);
            this.wifProviderIdTextBox.TabIndex = 4;
            // 
            // wifProviderIdLabel
            // 
            this.wifProviderIdLabel.AutoSize = true;
            this.wifProviderIdLabel.Location = new System.Drawing.Point(41, 135);
            this.wifProviderIdLabel.Name = "wifProviderIdLabel";
            this.wifProviderIdLabel.Size = new System.Drawing.Size(63, 13);
            this.wifProviderIdLabel.TabIndex = 6;
            this.wifProviderIdLabel.Text = "Provider ID:";
            // 
            // wifPoolIdTextBox
            // 
            this.wifPoolIdTextBox.Location = new System.Drawing.Point(108, 106);
            this.wifPoolIdTextBox.Name = "wifPoolIdTextBox";
            this.wifPoolIdTextBox.Size = new System.Drawing.Size(140, 20);
            this.wifPoolIdTextBox.TabIndex = 3;
            // 
            // wifPoolIdLabel
            // 
            this.wifPoolIdLabel.AutoSize = true;
            this.wifPoolIdLabel.Location = new System.Drawing.Point(41, 109);
            this.wifPoolIdLabel.Name = "wifPoolIdLabel";
            this.wifPoolIdLabel.Size = new System.Drawing.Size(45, 13);
            this.wifPoolIdLabel.TabIndex = 4;
            this.wifPoolIdLabel.Text = "Pool ID:";
            // 
            // wifLocationIdTextBox
            // 
            this.wifLocationIdTextBox.Location = new System.Drawing.Point(108, 80);
            this.wifLocationIdTextBox.Name = "wifLocationIdTextBox";
            this.wifLocationIdTextBox.Size = new System.Drawing.Size(140, 20);
            this.wifLocationIdTextBox.TabIndex = 2;
            this.wifLocationIdTextBox.Text = "global";
            // 
            // wifLocationIdLabel
            // 
            this.wifLocationIdLabel.AutoSize = true;
            this.wifLocationIdLabel.Location = new System.Drawing.Point(41, 83);
            this.wifLocationIdLabel.Name = "wifLocationIdLabel";
            this.wifLocationIdLabel.Size = new System.Drawing.Size(65, 13);
            this.wifLocationIdLabel.TabIndex = 2;
            this.wifLocationIdLabel.Text = "Location ID:";
            // 
            // workforceIdentityRadioButton
            // 
            this.workforceIdentityRadioButton.AutoSize = true;
            this.workforceIdentityRadioButton.Location = new System.Drawing.Point(24, 48);
            this.workforceIdentityRadioButton.Name = "workforceIdentityRadioButton";
            this.workforceIdentityRadioButton.Size = new System.Drawing.Size(215, 17);
            this.workforceIdentityRadioButton.TabIndex = 1;
            this.workforceIdentityRadioButton.TabStop = true;
            this.workforceIdentityRadioButton.Text = "Sign in with workforce identity federation";
            this.workforceIdentityRadioButton.UseVisualStyleBackColor = true;
            // 
            // gaiaRadioButton
            // 
            this.gaiaRadioButton.AutoSize = true;
            this.gaiaRadioButton.Location = new System.Drawing.Point(24, 20);
            this.gaiaRadioButton.Name = "gaiaRadioButton";
            this.gaiaRadioButton.Size = new System.Drawing.Size(208, 17);
            this.gaiaRadioButton.TabIndex = 0;
            this.gaiaRadioButton.TabStop = true;
            this.gaiaRadioButton.Text = "Sign in with a Google account (default)";
            this.gaiaRadioButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(91, 240);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 5;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(172, 240);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // AuthorizeOptionsView
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(264, 281);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.groupPnel);
            this.Controls.Add(this.headerLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AuthorizeOptionsView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sign-in method";
            this.groupPnel.ResumeLayout(false);
            this.groupPnel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Mvvm.Controls.HeaderLabel headerLabel;
        private System.Windows.Forms.Panel groupPnel;
        private System.Windows.Forms.RadioButton gaiaRadioButton;
        private System.Windows.Forms.TextBox wifProviderIdTextBox;
        private System.Windows.Forms.Label wifProviderIdLabel;
        private System.Windows.Forms.TextBox wifPoolIdTextBox;
        private System.Windows.Forms.Label wifPoolIdLabel;
        private System.Windows.Forms.TextBox wifLocationIdTextBox;
        private System.Windows.Forms.Label wifLocationIdLabel;
        private System.Windows.Forms.RadioButton workforceIdentityRadioButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}