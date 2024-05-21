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

using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Tsc;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp
{
    partial class RdpView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RdpView));
            this.overlayPanel = new System.Windows.Forms.Panel();
            this.waitPanel = new System.Windows.Forms.Panel();
            this.spinner = new Google.Solutions.Mvvm.Controls.CircularProgressBar();
            this.reconnectPanel = new System.Windows.Forms.Panel();
            this.reconnectLabel = new System.Windows.Forms.Label();
            this.reconnectButton = new System.Windows.Forms.LinkLabel();
            this.timeoutIcon = new System.Windows.Forms.PictureBox();
            this.rdpClient = new RdpClient();
            this.overlayPanel.SuspendLayout();
            this.waitPanel.SuspendLayout();
            this.reconnectPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // overlayPanel
            // 
            this.overlayPanel.Controls.Add(this.waitPanel);
            this.overlayPanel.Controls.Add(this.reconnectPanel);
            this.overlayPanel.Location = new System.Drawing.Point(299, 71);
            this.overlayPanel.Name = "overlayPanel";
            this.overlayPanel.Size = new System.Drawing.Size(295, 280);
            this.overlayPanel.TabIndex = 9;
            // 
            // waitPanel
            // 
            this.waitPanel.Controls.Add(this.spinner);
            this.waitPanel.Location = new System.Drawing.Point(41, 42);
            this.waitPanel.Name = "waitPanel";
            this.waitPanel.Size = new System.Drawing.Size(200, 100);
            this.waitPanel.TabIndex = 8;
            // 
            // spinner
            // 
            this.spinner.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.spinner.Indeterminate = true;
            this.spinner.LineWidth = 5;
            this.spinner.Location = new System.Drawing.Point(80, 30);
            this.spinner.Maximum = 100;
            this.spinner.MinimumSize = new System.Drawing.Size(15, 15);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(40, 40);
            this.spinner.Speed = 3;
            this.spinner.TabIndex = 3;
            this.spinner.TabStop = false;
            this.spinner.Value = 52;
            // 
            // reconnectPanel
            // 
            this.reconnectPanel.Controls.Add(this.reconnectLabel);
            this.reconnectPanel.Controls.Add(this.reconnectButton);
            this.reconnectPanel.Controls.Add(this.timeoutIcon);
            this.reconnectPanel.Location = new System.Drawing.Point(41, 163);
            this.reconnectPanel.Name = "reconnectPanel";
            this.reconnectPanel.Size = new System.Drawing.Size(200, 100);
            this.reconnectPanel.TabIndex = 7;
            // 
            // reconnectLabel
            // 
            this.reconnectLabel.AutoSize = true;
            this.reconnectLabel.Location = new System.Drawing.Point(3, 56);
            this.reconnectLabel.Name = "reconnectLabel";
            this.reconnectLabel.Size = new System.Drawing.Size(193, 26);
            this.reconnectLabel.TabIndex = 8;
            this.reconnectLabel.Text = "The Remote Desktop connection timed\r\nout or has been disconnected";
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
            this.reconnectButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.reconnectButton_LinkClicked);
            // 
            // timeoutIcon
            // 
            this.timeoutIcon.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.Disconnected_32;
            this.timeoutIcon.Location = new System.Drawing.Point(86, 21);
            this.timeoutIcon.Name = "timeoutIcon";
            this.timeoutIcon.Size = new System.Drawing.Size(32, 32);
            this.timeoutIcon.TabIndex = 5;
            this.timeoutIcon.TabStop = false;
            // 
            // rdpClient
            // 
            this.rdpClient.Enabled = true;
            this.rdpClient.Location = new System.Drawing.Point(0, 0);
            this.rdpClient.Name = "rdpClient";
            this.rdpClient.Size = new System.Drawing.Size(763, 431);
            this.rdpClient.TabIndex = 0;
            this.rdpClient.ConnectionFailed += rdpClient_ConnectionFailed;
            this.rdpClient.ConnectionClosed += rdpClient_ConnectionClosed;
            this.rdpClient.ServerAuthenticationWarningDisplayed += rdpClient_ServerAuthenticationWarningDisplayed;
            this.rdpClient.StateChanged += rdpClient_StateChanged;
            // 
            // RemoteDesktopView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(763, 431);
            this.Controls.Add(this.overlayPanel);
            this.Controls.Add(this.rdpClient);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RemoteDesktopView";
            this.Text = "RemoteDesktopPane";
            this.overlayPanel.ResumeLayout(false);
            this.waitPanel.ResumeLayout(false);
            this.reconnectPanel.ResumeLayout(false);
            this.reconnectPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private RdpClient rdpClient;
        private CircularProgressBar spinner;
        private System.Windows.Forms.Panel reconnectPanel;
        private System.Windows.Forms.Label reconnectLabel;
        private System.Windows.Forms.LinkLabel reconnectButton;
        private System.Windows.Forms.PictureBox timeoutIcon;
        private System.Windows.Forms.Panel waitPanel;
        private System.Windows.Forms.Panel overlayPanel;
    }
}