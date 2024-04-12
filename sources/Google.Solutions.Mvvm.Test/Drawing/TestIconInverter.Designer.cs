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
            this.largeDarkIcon = new System.Windows.Forms.PictureBox();
            this.mediumDarkIcon = new System.Windows.Forms.PictureBox();
            this.smallDarkIcon = new System.Windows.Forms.PictureBox();
            this.largeLightIcon = new System.Windows.Forms.PictureBox();
            this.mediumLightIcon = new System.Windows.Forms.PictureBox();
            this.smallLightIcon = new System.Windows.Forms.PictureBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.invertGraysCheckBox = new System.Windows.Forms.CheckBox();
            this.grayFactorScale = new System.Windows.Forms.TrackBar();
            this.grayFactorLabel = new System.Windows.Forms.Label();
            this.colorFactor = new System.Windows.Forms.Label();
            this.colorFactorScale = new System.Windows.Forms.TrackBar();
            this.grayFactorValue = new System.Windows.Forms.Label();
            this.colorFactorValue = new System.Windows.Forms.Label();
            this.darkPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.largeDarkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumDarkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallDarkIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.largeLightIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumLightIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallLightIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grayFactorScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.colorFactorScale)).BeginInit();
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
            // largeDarkIcon
            // 
            this.largeDarkIcon.Location = new System.Drawing.Point(16, 76);
            this.largeDarkIcon.Name = "largeDarkIcon";
            this.largeDarkIcon.Size = new System.Drawing.Size(48, 48);
            this.largeDarkIcon.TabIndex = 2;
            this.largeDarkIcon.TabStop = false;
            // 
            // mediumDarkIcon
            // 
            this.mediumDarkIcon.Location = new System.Drawing.Point(16, 38);
            this.mediumDarkIcon.Name = "mediumDarkIcon";
            this.mediumDarkIcon.Size = new System.Drawing.Size(32, 32);
            this.mediumDarkIcon.TabIndex = 1;
            this.mediumDarkIcon.TabStop = false;
            // 
            // smallDarkIcon
            // 
            this.smallDarkIcon.Location = new System.Drawing.Point(16, 16);
            this.smallDarkIcon.Name = "smallDarkIcon";
            this.smallDarkIcon.Size = new System.Drawing.Size(16, 16);
            this.smallDarkIcon.TabIndex = 0;
            this.smallDarkIcon.TabStop = false;
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
            this.browseButton.Location = new System.Drawing.Point(14, 295);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(130, 23);
            this.browseButton.TabIndex = 6;
            this.browseButton.Text = "Select image...";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // invertGraysCheckBox
            // 
            this.invertGraysCheckBox.AutoSize = true;
            this.invertGraysCheckBox.Checked = true;
            this.invertGraysCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.invertGraysCheckBox.Location = new System.Drawing.Point(12, 186);
            this.invertGraysCheckBox.Name = "invertGraysCheckBox";
            this.invertGraysCheckBox.Size = new System.Drawing.Size(83, 17);
            this.invertGraysCheckBox.TabIndex = 7;
            this.invertGraysCheckBox.Text = "Invert Grays";
            this.invertGraysCheckBox.UseVisualStyleBackColor = true;
            // 
            // grayFactorScale
            // 
            this.grayFactorScale.Location = new System.Drawing.Point(55, 209);
            this.grayFactorScale.Maximum = 100;
            this.grayFactorScale.Name = "grayFactorScale";
            this.grayFactorScale.Size = new System.Drawing.Size(91, 45);
            this.grayFactorScale.TabIndex = 9;
            this.grayFactorScale.TickStyle = System.Windows.Forms.TickStyle.None;
            this.grayFactorScale.Value = 63;
            // 
            // grayFactorLabel
            // 
            this.grayFactorLabel.AutoSize = true;
            this.grayFactorLabel.Location = new System.Drawing.Point(11, 212);
            this.grayFactorLabel.Name = "grayFactorLabel";
            this.grayFactorLabel.Size = new System.Drawing.Size(37, 13);
            this.grayFactorLabel.TabIndex = 10;
            this.grayFactorLabel.Text = "Grays:";
            // 
            // colorFactor
            // 
            this.colorFactor.AutoSize = true;
            this.colorFactor.Location = new System.Drawing.Point(11, 247);
            this.colorFactor.Name = "colorFactor";
            this.colorFactor.Size = new System.Drawing.Size(39, 13);
            this.colorFactor.TabIndex = 12;
            this.colorFactor.Text = "Colors:";
            // 
            // colorFactorScale
            // 
            this.colorFactorScale.Location = new System.Drawing.Point(55, 244);
            this.colorFactorScale.Maximum = 100;
            this.colorFactorScale.Name = "colorFactorScale";
            this.colorFactorScale.Size = new System.Drawing.Size(91, 45);
            this.colorFactorScale.TabIndex = 11;
            this.colorFactorScale.TickStyle = System.Windows.Forms.TickStyle.None;
            this.colorFactorScale.Value = 95;
            // 
            // grayFactorValue
            // 
            this.grayFactorValue.AutoSize = true;
            this.grayFactorValue.Location = new System.Drawing.Point(146, 212);
            this.grayFactorValue.Name = "grayFactorValue";
            this.grayFactorValue.Size = new System.Drawing.Size(10, 13);
            this.grayFactorValue.TabIndex = 13;
            this.grayFactorValue.Text = "-";
            // 
            // colorFactorValue
            // 
            this.colorFactorValue.AutoSize = true;
            this.colorFactorValue.Location = new System.Drawing.Point(146, 247);
            this.colorFactorValue.Name = "colorFactorValue";
            this.colorFactorValue.Size = new System.Drawing.Size(10, 13);
            this.colorFactorValue.TabIndex = 13;
            this.colorFactorValue.Text = "-";
            // 
            // TestIconInverter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(185, 336);
            this.Controls.Add(this.colorFactorValue);
            this.Controls.Add(this.grayFactorValue);
            this.Controls.Add(this.colorFactor);
            this.Controls.Add(this.colorFactorScale);
            this.Controls.Add(this.grayFactorLabel);
            this.Controls.Add(this.grayFactorScale);
            this.Controls.Add(this.invertGraysCheckBox);
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
            ((System.ComponentModel.ISupportInitialize)(this.largeDarkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumDarkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallDarkIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.largeLightIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mediumLightIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallLightIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grayFactorScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.colorFactorScale)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.CheckBox invertGraysCheckBox;
        private System.Windows.Forms.TrackBar grayFactorScale;
        private System.Windows.Forms.Label grayFactorLabel;
        private System.Windows.Forms.Label colorFactor;
        private System.Windows.Forms.TrackBar colorFactorScale;
        private System.Windows.Forms.Label grayFactorValue;
        private System.Windows.Forms.Label colorFactorValue;
    }
}