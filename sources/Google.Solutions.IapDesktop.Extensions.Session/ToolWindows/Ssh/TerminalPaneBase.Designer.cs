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

using Google.Solutions.Mvvm.Controls;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    partial class TerminalViewBase
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TerminalViewBase));
            this.reconnectPanel = new System.Windows.Forms.Panel();
            this.reconnectLabel = new System.Windows.Forms.Label();
            this.reconnectButton = new System.Windows.Forms.LinkLabel();
            this.timeoutIcon = new System.Windows.Forms.PictureBox();
            this.spinner = new Google.Solutions.Mvvm.Controls.CircularProgressBar();
            this.terminal = new Google.Solutions.IapDesktop.Extensions.Session.Controls.VirtualTerminal();
            this.reconnectPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // reconnectPanel
            // 
            this.reconnectPanel.Controls.Add(this.reconnectLabel);
            this.reconnectPanel.Controls.Add(this.reconnectButton);
            this.reconnectPanel.Controls.Add(this.timeoutIcon);
            this.reconnectPanel.Location = new System.Drawing.Point(141, 261);
            this.reconnectPanel.Name = "reconnectPanel";
            this.reconnectPanel.Size = new System.Drawing.Size(200, 100);
            this.reconnectPanel.TabIndex = 8;
            this.reconnectPanel.Visible = false;
            // 
            // reconnectLabel
            // 
            this.reconnectLabel.AutoSize = true;
            this.reconnectLabel.Location = new System.Drawing.Point(23, 56);
            this.reconnectLabel.Name = "reconnectLabel";
            this.reconnectLabel.Size = new System.Drawing.Size(153, 26);
            this.reconnectLabel.TabIndex = 8;
            this.reconnectLabel.Text = "The SSH connection timed out\r\nor has been disconnected";
            this.reconnectLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // reconnectButton
            // 
            this.reconnectButton.AutoSize = true;
            this.reconnectButton.Location = new System.Drawing.Point(70, 85);
            this.reconnectButton.Name = "reconnectButton";
            this.reconnectButton.Size = new System.Drawing.Size(60, 13);
            this.reconnectButton.TabIndex = 7;
            this.reconnectButton.TabStop = true;
            this.reconnectButton.Text = "Reconnect";
            this.reconnectButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.reconnectButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnReconnectLinkClicked);
            // 
            // timeoutIcon
            // 
            this.timeoutIcon.BackColor = System.Drawing.Color.Transparent;
            this.timeoutIcon.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.Disconnected_32;
            this.timeoutIcon.Location = new System.Drawing.Point(86, 21);
            this.timeoutIcon.Name = "timeoutIcon";
            this.timeoutIcon.Size = new System.Drawing.Size(32, 32);
            this.timeoutIcon.TabIndex = 5;
            this.timeoutIcon.TabStop = false;
            // 
            // spinner
            // 
            this.spinner.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.spinner.Indeterminate = true;
            this.spinner.Location = new System.Drawing.Point(218, 81);
            this.spinner.Maximum = 100;
            this.spinner.MinimumSize = new System.Drawing.Size(15, 15);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(40, 40);
            this.spinner.Speed = 3;
            this.spinner.TabIndex = 9;
            this.spinner.TabStop = false;
            this.spinner.Value = 0;
            // 
            // terminal
            // 
            this.terminal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.terminal.EnableCtrlA = false;
            this.terminal.EnableCtrlC = true;
            this.terminal.EnableCtrlHomeEnd = true;
            this.terminal.EnableCtrlInsert = true;
            this.terminal.EnableCtrlLeftRight = true;
            this.terminal.EnableCtrlUpDown = true;
            this.terminal.EnableCtrlV = true;
            this.terminal.EnableShiftInsert = true;
            this.terminal.EnableShiftLeftRight = true;
            this.terminal.EnableShiftUpDown = true;
            this.terminal.EnableTypographicQuoteConversionOnPaste = true;
            this.terminal.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.terminal.Location = new System.Drawing.Point(0, 0);
            this.terminal.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.terminal.Name = "terminal";
            this.terminal.Size = new System.Drawing.Size(800, 450);
            this.terminal.TabIndex = 0;
            this.terminal.WindowTitle = null;
            // 
            // TerminalViewBase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.reconnectPanel);
            this.Controls.Add(this.terminal);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TerminalViewBase";
            this.Text = "TerminalPaneBase";
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.OnLayout);
            this.reconnectPanel.ResumeLayout(false);
            this.reconnectPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.VirtualTerminal terminal;
        private System.Windows.Forms.Panel reconnectPanel;
        private System.Windows.Forms.Label reconnectLabel;
        private System.Windows.Forms.LinkLabel reconnectButton;
        private System.Windows.Forms.PictureBox timeoutIcon;
        private CircularProgressBar spinner;
    }
}