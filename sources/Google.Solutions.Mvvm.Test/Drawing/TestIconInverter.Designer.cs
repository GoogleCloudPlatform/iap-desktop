//
// Copyright 2022 Google LLC
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

namespace Google.Solutions.Mvvm.Test.Drawing
{
    partial class TestIconInverter
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
            this.darkPanel = new System.Windows.Forms.Panel();
            this.smallDarkIcon = new System.Windows.Forms.PictureBox();
            this.mediumDarkIcon = new System.Windows.Forms.PictureBox();
            this.largeDarkIcon = new System.Windows.Forms.PictureBox();
            this.largeLightIcon = new System.Windows.Forms.PictureBox();
            this.mediumLightIcon = new System.Windows.Forms.PictureBox();
            this.smallLightIcon = new System.Windows.Forms.PictureBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.darkPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.smallDarkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumDarkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.largeDarkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.largeLightIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumLightIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallLightIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // darkPanel
            // 
            this.darkPanel.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.darkPanel.Controls.Add(this.largeDarkIcon);
            this.darkPanel.Controls.Add(this.mediumDarkIcon);
            this.darkPanel.Controls.Add(this.smallDarkIcon);
            this.darkPanel.Location = new System.Drawing.Point(0, 0);
            this.darkPanel.Name = "darkPanel";
            this.darkPanel.Size = new System.Drawing.Size(81, 157);
            this.darkPanel.TabIndex = 0;
            // 
            // smallDarkIcon
            // 
            this.smallDarkIcon.Location = new System.Drawing.Point(16, 16);
            this.smallDarkIcon.Name = "smallDarkIcon";
            this.smallDarkIcon.Size = new System.Drawing.Size(16, 16);
            this.smallDarkIcon.TabIndex = 0;
            this.smallDarkIcon.TabStop = false;
            // 
            // mediumDarkIcon
            // 
            this.mediumDarkIcon.Location = new System.Drawing.Point(16, 38);
            this.mediumDarkIcon.Name = "mediumDarkIcon";
            this.mediumDarkIcon.Size = new System.Drawing.Size(32, 32);
            this.mediumDarkIcon.TabIndex = 1;
            this.mediumDarkIcon.TabStop = false;
            // 
            // largeDarkIcon
            // 
            this.largeDarkIcon.Location = new System.Drawing.Point(16, 76);
            this.largeDarkIcon.Name = "largeDarkIcon";
            this.largeDarkIcon.Size = new System.Drawing.Size(48, 48);
            this.largeDarkIcon.TabIndex = 2;
            this.largeDarkIcon.TabStop = false;
            // 
            // largeLightIcon
            // 
            this.largeLightIcon.Location = new System.Drawing.Point(87, 76);
            this.largeLightIcon.Name = "largeLightIcon";
            this.largeLightIcon.Size = new System.Drawing.Size(48, 48);
            this.largeLightIcon.TabIndex = 5;
            this.largeLightIcon.TabStop = false;
            // 
            // mediumLightIcon
            // 
            this.mediumLightIcon.Location = new System.Drawing.Point(87, 38);
            this.mediumLightIcon.Name = "mediumLightIcon";
            this.mediumLightIcon.Size = new System.Drawing.Size(32, 32);
            this.mediumLightIcon.TabIndex = 4;
            this.mediumLightIcon.TabStop = false;
            // 
            // smallLightIcon
            // 
            this.smallLightIcon.Location = new System.Drawing.Point(87, 16);
            this.smallLightIcon.Name = "smallLightIcon";
            this.smallLightIcon.Size = new System.Drawing.Size(16, 16);
            this.smallLightIcon.TabIndex = 3;
            this.smallLightIcon.TabStop = false;
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(16, 173);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(130, 23);
            this.browseButton.TabIndex = 6;
            this.browseButton.Text = "Select image...";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // TestIconInverter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(165, 216);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.largeLightIcon);
            this.Controls.Add(this.mediumLightIcon);
            this.Controls.Add(this.darkPanel);
            this.Controls.Add(this.smallLightIcon);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TestIconInverter";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "TestIconInverter";
            this.darkPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.smallDarkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumDarkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.largeDarkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.largeLightIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumLightIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallLightIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel darkPanel;
        private System.Windows.Forms.PictureBox largeDarkIcon;
        private System.Windows.Forms.PictureBox mediumDarkIcon;
        private System.Windows.Forms.PictureBox smallDarkIcon;
        private System.Windows.Forms.PictureBox largeLightIcon;
        private System.Windows.Forms.PictureBox mediumLightIcon;
        private System.Windows.Forms.PictureBox smallLightIcon;
        private System.Windows.Forms.Button browseButton;
    }
}