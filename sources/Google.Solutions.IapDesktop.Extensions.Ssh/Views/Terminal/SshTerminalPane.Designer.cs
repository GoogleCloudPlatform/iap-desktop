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

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal
{
    partial class SshTerminalPane
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SshTerminalPane));
            this.terminal = new Google.Solutions.IapDesktop.Extensions.Ssh.Controls.VirtualTerminal();
            this.reconnectPanel = new System.Windows.Forms.Panel();
            this.reconnectLabel = new System.Windows.Forms.Label();
            this.reconnectButton = new System.Windows.Forms.LinkLabel();
            this.timeoutIcon = new System.Windows.Forms.PictureBox();
            this.spinner = new System.Windows.Forms.PictureBox();
            this.reconnectPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).BeginInit();
            this.SuspendLayout();
            // 
            // terminal
            // 
            this.terminal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.terminal.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.terminal.Location = new System.Drawing.Point(0, 0);
            this.terminal.Name = "terminal";
            this.terminal.Size = new System.Drawing.Size(800, 450);
            this.terminal.TabIndex = 0;
            this.terminal.ViewTop = 0;
            this.terminal.WindowTitle = null;
            // 
            // reconnectPanel
            // 
            this.reconnectPanel.BackColor = System.Drawing.Color.White;
            this.reconnectPanel.Controls.Add(this.reconnectLabel);
            this.reconnectPanel.Controls.Add(this.reconnectButton);
            this.reconnectPanel.Controls.Add(this.timeoutIcon);
            this.reconnectPanel.Location = new System.Drawing.Point(141, 261);
            this.reconnectPanel.Name = "reconnectPanel";
            this.reconnectPanel.Size = new System.Drawing.Size(207, 100);
            this.reconnectPanel.TabIndex = 8;
            this.reconnectPanel.Visible = false;
            // 
            // reconnectLabel
            // 
            this.reconnectLabel.AutoSize = true;
            this.reconnectLabel.Location = new System.Drawing.Point(11, 56);
            this.reconnectLabel.Name = "reconnectLabel";
            this.reconnectLabel.Size = new System.Drawing.Size(189, 26);
            this.reconnectLabel.TabIndex = 8;
            this.reconnectLabel.Text = "Remote Desktop connection timed out\r\nor has been disconnected";
            this.reconnectLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // reconnectButton
            // 
            this.reconnectButton.AutoSize = true;
            this.reconnectButton.BackColor = System.Drawing.Color.White;
            this.reconnectButton.Location = new System.Drawing.Point(71, 85);
            this.reconnectButton.Name = "reconnectButton";
            this.reconnectButton.Size = new System.Drawing.Size(60, 13);
            this.reconnectButton.TabIndex = 7;
            this.reconnectButton.TabStop = true;
            this.reconnectButton.Text = "Reconnect";
            // 
            // timeoutIcon
            // 
            this.timeoutIcon.BackColor = System.Drawing.Color.White;
            this.timeoutIcon.Image = ((System.Drawing.Image)(resources.GetObject("timeoutIcon.Image")));
            this.timeoutIcon.Location = new System.Drawing.Point(77, 3);
            this.timeoutIcon.Name = "timeoutIcon";
            this.timeoutIcon.Size = new System.Drawing.Size(51, 48);
            this.timeoutIcon.TabIndex = 5;
            this.timeoutIcon.TabStop = false;
            // 
            // spinner
            // 
            this.spinner.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.spinner.BackColor = System.Drawing.Color.White;
            this.spinner.Location = new System.Drawing.Point(218, 81);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.TabIndex = 9;
            this.spinner.TabStop = false;
            // 
            // SshTerminalPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.reconnectPanel);
            this.Controls.Add(this.terminal);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SshTerminalPane";
            this.Text = "TerminalPaneBase";
            this.reconnectPanel.ResumeLayout(false);
            this.reconnectPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.VirtualTerminal terminal;
        private System.Windows.Forms.Panel reconnectPanel;
        private System.Windows.Forms.Label reconnectLabel;
        private System.Windows.Forms.LinkLabel reconnectButton;
        private System.Windows.Forms.PictureBox timeoutIcon;
        private System.Windows.Forms.PictureBox spinner;
    }
}