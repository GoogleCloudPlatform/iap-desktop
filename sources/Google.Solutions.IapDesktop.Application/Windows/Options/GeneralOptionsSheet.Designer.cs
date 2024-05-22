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
    partial class GeneralOptionsSheet
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralOptionsSheet));
            this.updateBox = new System.Windows.Forms.GroupBox();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.lastCheckHeaderLabel = new System.Windows.Forms.Label();
            this.enableUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.browserIntegrationBox = new System.Windows.Forms.GroupBox();
            this.browserIntegrationLink = new System.Windows.Forms.LinkLabel();
            this.enableBrowserIntegrationCheckBox = new System.Windows.Forms.CheckBox();
            this.telemetryBox = new System.Windows.Forms.GroupBox();
            this.telemetryLink = new System.Windows.Forms.LinkLabel();
            this.enableTelemetryCheckBox = new System.Windows.Forms.CheckBox();
            this.updateBox.SuspendLayout();
            this.browserIntegrationBox.SuspendLayout();
            this.telemetryBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // updateBox
            // 
            this.updateBox.Controls.Add(this.lastCheckLabel);
            this.updateBox.Controls.Add(this.lastCheckHeaderLabel);
            this.updateBox.Controls.Add(this.enableUpdateCheckBox);
            this.updateBox.Location = new System.Drawing.Point(4, 100);
            this.updateBox.Name = "updateBox";
            this.updateBox.Size = new System.Drawing.Size(336, 83);
            this.updateBox.TabIndex = 2;
            this.updateBox.TabStop = false;
            this.updateBox.Text = "Updates:";
            // 
            // lastCheckLabel
            // 
            this.lastCheckLabel.AutoSize = true;
            this.lastCheckLabel.Location = new System.Drawing.Point(103, 49);
            this.lastCheckLabel.Name = "lastCheckLabel";
            this.lastCheckLabel.Size = new System.Drawing.Size(10, 13);
            this.lastCheckLabel.TabIndex = 9;
            this.lastCheckLabel.Text = "-";
            // 
            // lastCheckHeaderLabel
            // 
            this.lastCheckHeaderLabel.AutoSize = true;
            this.lastCheckHeaderLabel.Location = new System.Drawing.Point(32, 49);
            this.lastCheckHeaderLabel.Name = "lastCheckHeaderLabel";
            this.lastCheckHeaderLabel.Size = new System.Drawing.Size(63, 13);
            this.lastCheckHeaderLabel.TabIndex = 8;
            this.lastCheckHeaderLabel.Text = "Last check:";
            // 
            // enableUpdateCheckBox
            // 
            this.enableUpdateCheckBox.Location = new System.Drawing.Point(16, 24);
            this.enableUpdateCheckBox.Name = "enableUpdateCheckBox";
            this.enableUpdateCheckBox.Size = new System.Drawing.Size(252, 22);
            this.enableUpdateCheckBox.TabIndex = 7;
            this.enableUpdateCheckBox.Text = "Notify me when an update is available";
            this.enableUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // browserIntegrationBox
            // 
            this.browserIntegrationBox.Controls.Add(this.browserIntegrationLink);
            this.browserIntegrationBox.Controls.Add(this.enableBrowserIntegrationCheckBox);
            this.browserIntegrationBox.Location = new System.Drawing.Point(4, 4);
            this.browserIntegrationBox.Name = "browserIntegrationBox";
            this.browserIntegrationBox.Size = new System.Drawing.Size(336, 92);
            this.browserIntegrationBox.TabIndex = 0;
            this.browserIntegrationBox.TabStop = false;
            this.browserIntegrationBox.Text = "Browser integration:";
            // 
            // browserIntegrationLink
            // 
            this.browserIntegrationLink.AutoSize = true;
            this.browserIntegrationLink.Location = new System.Drawing.Point(31, 62);
            this.browserIntegrationLink.Name = "browserIntegrationLink";
            this.browserIntegrationLink.Size = new System.Drawing.Size(85, 13);
            this.browserIntegrationLink.TabIndex = 2;
            this.browserIntegrationLink.TabStop = true;
            this.browserIntegrationLink.Text = "More information";
            // 
            // enableBrowserIntegrationCheckBox
            // 
            this.enableBrowserIntegrationCheckBox.AutoSize = true;
            this.enableBrowserIntegrationCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.enableBrowserIntegrationCheckBox.Location = new System.Drawing.Point(16, 24);
            this.enableBrowserIntegrationCheckBox.Name = "enableBrowserIntegrationCheckBox";
            this.enableBrowserIntegrationCheckBox.Size = new System.Drawing.Size(290, 30);
            this.enableBrowserIntegrationCheckBox.TabIndex = 1;
            this.enableBrowserIntegrationCheckBox.Text = "Allow launching IAP Desktop from a web browser when \r\nselecting iap-rdp:/// links" +
    "";
            this.enableBrowserIntegrationCheckBox.UseVisualStyleBackColor = true;
            // 
            // telemetryBox
            // 
            this.telemetryBox.Controls.Add(this.telemetryLink);
            this.telemetryBox.Controls.Add(this.enableTelemetryCheckBox);
            this.telemetryBox.Location = new System.Drawing.Point(4, 188);
            this.telemetryBox.Name = "telemetryBox";
            this.telemetryBox.Size = new System.Drawing.Size(336, 152);
            this.telemetryBox.TabIndex = 3;
            this.telemetryBox.TabStop = false;
            this.telemetryBox.Text = "Data sharing:";
            // 
            // telemetryLink
            // 
            this.telemetryLink.AutoSize = true;
            this.telemetryLink.Location = new System.Drawing.Point(31, 125);
            this.telemetryLink.Name = "telemetryLink";
            this.telemetryLink.Size = new System.Drawing.Size(85, 13);
            this.telemetryLink.TabIndex = 2;
            this.telemetryLink.TabStop = true;
            this.telemetryLink.Text = "More information";
            // 
            // enableTelemetryCheckBox
            // 
            this.enableTelemetryCheckBox.AutoSize = true;
            this.enableTelemetryCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.enableTelemetryCheckBox.Location = new System.Drawing.Point(16, 24);
            this.enableTelemetryCheckBox.Name = "enableTelemetryCheckBox";
            this.enableTelemetryCheckBox.Size = new System.Drawing.Size(303, 95);
            this.enableTelemetryCheckBox.TabIndex = 1;
            this.enableTelemetryCheckBox.Text = resources.GetString("enableTelemetryCheckBox.Text");
            this.enableTelemetryCheckBox.UseVisualStyleBackColor = true;
            // 
            // GeneralOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.telemetryBox);
            this.Controls.Add(this.browserIntegrationBox);
            this.Controls.Add(this.updateBox);
            this.Name = "GeneralOptionsSheet";
            this.Size = new System.Drawing.Size(343, 369);
            this.updateBox.ResumeLayout(false);
            this.updateBox.PerformLayout();
            this.browserIntegrationBox.ResumeLayout(false);
            this.browserIntegrationBox.PerformLayout();
            this.telemetryBox.ResumeLayout(false);
            this.telemetryBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox updateBox;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Label lastCheckHeaderLabel;
        private System.Windows.Forms.CheckBox enableUpdateCheckBox;
        private System.Windows.Forms.GroupBox browserIntegrationBox;
        private System.Windows.Forms.CheckBox enableBrowserIntegrationCheckBox;
        private System.Windows.Forms.LinkLabel browserIntegrationLink;
        private System.Windows.Forms.GroupBox telemetryBox;
        private System.Windows.Forms.LinkLabel telemetryLink;
        private System.Windows.Forms.CheckBox enableTelemetryCheckBox;
    }
}
