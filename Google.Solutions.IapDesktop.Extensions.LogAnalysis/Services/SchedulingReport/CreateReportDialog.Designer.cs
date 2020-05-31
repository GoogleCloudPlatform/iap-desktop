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

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    partial class CreateReportDialog
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
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.panel = new System.Windows.Forms.Panel();
            this.timeFrameList = new System.Windows.Forms.ComboBox();
            this.projectsList = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.topPane = new System.Windows.Forms.Label();
            this.projectIcon = new System.Windows.Forms.PictureBox();
            this.panel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.projectIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(237, 178);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(82, 28);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(150, 178);
            this.okButton.Margin = new System.Windows.Forms.Padding(2);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(82, 28);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // panel
            // 
            this.panel.BackColor = System.Drawing.Color.White;
            this.panel.Controls.Add(this.timeFrameList);
            this.panel.Controls.Add(this.projectsList);
            this.panel.Controls.Add(this.label1);
            this.panel.Controls.Add(this.topPane);
            this.panel.Controls.Add(this.projectIcon);
            this.panel.Location = new System.Drawing.Point(-1, -1);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(334, 165);
            this.panel.TabIndex = 1;
            // 
            // timeFrameList
            // 
            this.timeFrameList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.timeFrameList.FormattingEnabled = true;
            this.timeFrameList.Location = new System.Drawing.Point(64, 124);
            this.timeFrameList.Name = "timeFrameList";
            this.timeFrameList.Size = new System.Drawing.Size(230, 21);
            this.timeFrameList.TabIndex = 13;
            // 
            // projectsList
            // 
            this.projectsList.FormattingEnabled = true;
            this.projectsList.Location = new System.Drawing.Point(64, 30);
            this.projectsList.Name = "projectsList";
            this.projectsList.Size = new System.Drawing.Size(230, 64);
            this.projectsList.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(61, 108);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Time frame:";
            // 
            // topPane
            // 
            this.topPane.AutoSize = true;
            this.topPane.Location = new System.Drawing.Point(61, 14);
            this.topPane.Name = "topPane";
            this.topPane.Size = new System.Drawing.Size(99, 13);
            this.topPane.TabIndex = 10;
            this.topPane.Text = "Projects to analyze:";
            // 
            // projectIcon
            // 
            this.projectIcon.Image = global::Google.Solutions.IapDesktop.Extensions.LogAnalysis.Properties.Resources.Refresh_32;
            this.projectIcon.Location = new System.Drawing.Point(18, 14);
            this.projectIcon.Name = "projectIcon";
            this.projectIcon.Size = new System.Drawing.Size(32, 32);
            this.projectIcon.TabIndex = 9;
            this.projectIcon.TabStop = false;
            // 
            // CreateReportDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(330, 222);
            this.ControlBox = false;
            this.Controls.Add(this.panel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "CreateReportDialog";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create instance and node usage report";
            this.panel.ResumeLayout(false);
            this.panel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.projectIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.Label topPane;
        private System.Windows.Forms.PictureBox projectIcon;
        private System.Windows.Forms.ComboBox timeFrameList;
        private System.Windows.Forms.CheckedListBox projectsList;
        private System.Windows.Forms.Label label1;
    }
}