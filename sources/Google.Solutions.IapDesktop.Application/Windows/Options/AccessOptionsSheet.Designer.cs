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

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    partial class AccessOptionsSheet
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
            this.pscBox = new System.Windows.Forms.GroupBox();
            this.pscLink = new System.Windows.Forms.LinkLabel();
            this.pscProxyNote = new System.Windows.Forms.Label();
            this.pscEndpointLabel = new System.Windows.Forms.Label();
            this.pscEndpointTextBox = new System.Windows.Forms.TextBox();
            this.enablePscCheckBox = new System.Windows.Forms.CheckBox();
            this.dcaBox = new System.Windows.Forms.GroupBox();
            this.dcaNote = new System.Windows.Forms.Label();
            this.secureConnectLink = new System.Windows.Forms.LinkLabel();
            this.enableDcaCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.connectionBox = new System.Windows.Forms.GroupBox();
            this.connectionPoolSizeLabel = new System.Windows.Forms.Label();
            this.connectionLimitUpDown = new System.Windows.Forms.NumericUpDown();
            this.pscBox.SuspendLayout();
            this.dcaBox.SuspendLayout();
            this.connectionBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.connectionLimitUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // pscBox
            // 
            this.pscBox.Controls.Add(this.pscLink);
            this.pscBox.Controls.Add(this.pscProxyNote);
            this.pscBox.Controls.Add(this.pscEndpointLabel);
            this.pscBox.Controls.Add(this.pscEndpointTextBox);
            this.pscBox.Controls.Add(this.enablePscCheckBox);
            this.pscBox.Location = new System.Drawing.Point(3, 3);
            this.pscBox.Name = "pscBox";
            this.pscBox.Size = new System.Drawing.Size(336, 151);
            this.pscBox.TabIndex = 0;
            this.pscBox.TabStop = false;
            this.pscBox.Text = "Private Service Connect:";
            // 
            // pscLink
            // 
            this.pscLink.AutoSize = true;
            this.pscLink.Location = new System.Drawing.Point(33, 120);
            this.pscLink.Name = "pscLink";
            this.pscLink.Size = new System.Drawing.Size(85, 13);
            this.pscLink.TabIndex = 9;
            this.pscLink.TabStop = true;
            this.pscLink.Text = "More information";
            // 
            // pscProxyNote
            // 
            this.pscProxyNote.AutoSize = true;
            this.pscProxyNote.Location = new System.Drawing.Point(32, 82);
            this.pscProxyNote.Name = "pscProxyNote";
            this.pscProxyNote.Size = new System.Drawing.Size(261, 26);
            this.pscProxyNote.TabIndex = 3;
            this.pscProxyNote.Text = "Connections to the Private Service Connect endpoint \r\nbypass proxy servers";
            // 
            // pscEndpointLabel
            // 
            this.pscEndpointLabel.AutoSize = true;
            this.pscEndpointLabel.Location = new System.Drawing.Point(32, 54);
            this.pscEndpointLabel.Name = "pscEndpointLabel";
            this.pscEndpointLabel.Size = new System.Drawing.Size(116, 13);
            this.pscEndpointLabel.TabIndex = 2;
            this.pscEndpointLabel.Text = "Endpoint (IP or FQDN):";
            // 
            // pscEndpointTextBox
            // 
            this.pscEndpointTextBox.Location = new System.Drawing.Point(154, 50);
            this.pscEndpointTextBox.Name = "pscEndpointTextBox";
            this.pscEndpointTextBox.Size = new System.Drawing.Size(153, 20);
            this.pscEndpointTextBox.TabIndex = 2;
            // 
            // enablePscCheckBox
            // 
            this.enablePscCheckBox.AutoSize = true;
            this.enablePscCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.enablePscCheckBox.Location = new System.Drawing.Point(16, 24);
            this.enablePscCheckBox.Name = "enablePscCheckBox";
            this.enablePscCheckBox.Size = new System.Drawing.Size(291, 17);
            this.enablePscCheckBox.TabIndex = 1;
            this.enablePscCheckBox.Text = "Use Private Service Connect to connect to Google APIs";
            this.enablePscCheckBox.UseVisualStyleBackColor = true;
            // 
            // dcaBox
            // 
            this.dcaBox.Controls.Add(this.dcaNote);
            this.dcaBox.Controls.Add(this.secureConnectLink);
            this.dcaBox.Controls.Add(this.enableDcaCheckBox);
            this.dcaBox.Location = new System.Drawing.Point(4, 160);
            this.dcaBox.Name = "dcaBox";
            this.dcaBox.Size = new System.Drawing.Size(336, 118);
            this.dcaBox.TabIndex = 1;
            this.dcaBox.TabStop = false;
            this.dcaBox.Text = "BeyondCorp:";
            // 
            // dcaNote
            // 
            this.dcaNote.AutoSize = true;
            this.dcaNote.Location = new System.Drawing.Point(32, 48);
            this.dcaNote.Name = "dcaNote";
            this.dcaNote.Size = new System.Drawing.Size(231, 26);
            this.dcaNote.TabIndex = 7;
            this.dcaNote.Text = "Certificate-based access requires the computer \r\nto be enrolled in Endpoint Verif" +
    "ication";
            // 
            // secureConnectLink
            // 
            this.secureConnectLink.AutoSize = true;
            this.secureConnectLink.Location = new System.Drawing.Point(32, 84);
            this.secureConnectLink.Name = "secureConnectLink";
            this.secureConnectLink.Size = new System.Drawing.Size(85, 13);
            this.secureConnectLink.TabIndex = 8;
            this.secureConnectLink.TabStop = true;
            this.secureConnectLink.Text = "More information";
            // 
            // enableDcaCheckBox
            // 
            this.enableDcaCheckBox.AutoSize = true;
            this.enableDcaCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.enableDcaCheckBox.Location = new System.Drawing.Point(16, 24);
            this.enableDcaCheckBox.Name = "enableDcaCheckBox";
            this.enableDcaCheckBox.Size = new System.Drawing.Size(238, 17);
            this.enableDcaCheckBox.TabIndex = 3;
            this.enableDcaCheckBox.Text = "Enable BeyondCorp certificate-based access";
            this.enableDcaCheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 360);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(171, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Changes take effect after relaunch";
            // 
            // connectionBox
            // 
            this.connectionBox.Controls.Add(this.connectionPoolSizeLabel);
            this.connectionBox.Controls.Add(this.connectionLimitUpDown);
            this.connectionBox.Location = new System.Drawing.Point(4, 284);
            this.connectionBox.Name = "connectionBox";
            this.connectionBox.Size = new System.Drawing.Size(336, 60);
            this.connectionBox.TabIndex = 8;
            this.connectionBox.TabStop = false;
            this.connectionBox.Text = "Connection pooling:";
            // 
            // connectionPoolSizeLabel
            // 
            this.connectionPoolSizeLabel.AutoSize = true;
            this.connectionPoolSizeLabel.Location = new System.Drawing.Point(12, 24);
            this.connectionPoolSizeLabel.Name = "connectionPoolSizeLabel";
            this.connectionPoolSizeLabel.Size = new System.Drawing.Size(203, 13);
            this.connectionPoolSizeLabel.TabIndex = 0;
            this.connectionPoolSizeLabel.Text = "Max number of connections per endpoint:";
            // 
            // connectionLimitUpDown
            // 
            this.connectionLimitUpDown.Location = new System.Drawing.Point(231, 22);
            this.connectionLimitUpDown.Maximum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.connectionLimitUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.connectionLimitUpDown.Name = "connectionLimitUpDown";
            this.connectionLimitUpDown.Size = new System.Drawing.Size(75, 20);
            this.connectionLimitUpDown.TabIndex = 1;
            this.connectionLimitUpDown.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // AccessOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.connectionBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dcaBox);
            this.Controls.Add(this.pscBox);
            this.Name = "AccessOptionsSheet";
            this.Size = new System.Drawing.Size(340, 401);
            this.pscBox.ResumeLayout(false);
            this.pscBox.PerformLayout();
            this.dcaBox.ResumeLayout(false);
            this.dcaBox.PerformLayout();
            this.connectionBox.ResumeLayout(false);
            this.connectionBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.connectionLimitUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox pscBox;
        private System.Windows.Forms.CheckBox enablePscCheckBox;
        private System.Windows.Forms.TextBox pscEndpointTextBox;
        private System.Windows.Forms.Label pscEndpointLabel;
        private System.Windows.Forms.GroupBox dcaBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label dcaNote;
        private System.Windows.Forms.LinkLabel secureConnectLink;
        private System.Windows.Forms.CheckBox enableDcaCheckBox;
        private System.Windows.Forms.Label pscProxyNote;
        private System.Windows.Forms.GroupBox connectionBox;
        private System.Windows.Forms.Label connectionPoolSizeLabel;
        private System.Windows.Forms.NumericUpDown connectionLimitUpDown;
        private System.Windows.Forms.LinkLabel pscLink;
    }
}
