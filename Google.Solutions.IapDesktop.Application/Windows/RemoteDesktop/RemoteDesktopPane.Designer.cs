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

namespace Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop
{
    partial class RemoteDesktopPane
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RemoteDesktopPane));
            this.rdpClient = new AxMSTSCLib.AxMsRdpClient9NotSafeForScripting();
            this.spinner = new System.Windows.Forms.PictureBox();
            this.reconnectToResizeTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.rdpClient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).BeginInit();
            this.SuspendLayout();
            // 
            // rdpClient
            // 
            this.rdpClient.Enabled = true;
            this.rdpClient.Location = new System.Drawing.Point(0, 0);
            this.rdpClient.Name = "rdpClient";
            this.rdpClient.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("rdpClient.OcxState")));
            this.rdpClient.Size = new System.Drawing.Size(763, 431);
            this.rdpClient.TabIndex = 0;
            this.rdpClient.OnConnecting += new System.EventHandler(this.rdpClient_OnConnecting);
            this.rdpClient.OnConnected += new System.EventHandler(this.rdpClient_OnConnected);
            this.rdpClient.OnDisconnected += new AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEventHandler(this.rdpClient_OnDisconnected);
            this.rdpClient.OnEnterFullScreenMode += new System.EventHandler(this.rdpClient_OnEnterFullScreenMode);
            this.rdpClient.OnLeaveFullScreenMode += new System.EventHandler(this.rdpClient_OnLeaveFullScreenMode);
            this.rdpClient.OnFatalError += new AxMSTSCLib.IMsTscAxEvents_OnFatalErrorEventHandler(this.rdpClient_OnFatalError);
            this.rdpClient.OnWarning += new AxMSTSCLib.IMsTscAxEvents_OnWarningEventHandler(this.rdpClient_OnWarning);
            this.rdpClient.OnRemoteDesktopSizeChange += new AxMSTSCLib.IMsTscAxEvents_OnRemoteDesktopSizeChangeEventHandler(this.rdpClient_OnRemoteDesktopSizeChange);
            this.rdpClient.OnAuthenticationWarningDisplayed += new System.EventHandler(this.rdpClient_OnAuthenticationWarningDisplayed);
            this.rdpClient.OnLogonError += new AxMSTSCLib.IMsTscAxEvents_OnLogonErrorEventHandler(this.rdpClient_OnLogonError);
            this.rdpClient.OnFocusReleased += new AxMSTSCLib.IMsTscAxEvents_OnFocusReleasedEventHandler(this.rdpClient_OnFocusReleased);
            this.rdpClient.OnAutoReconnected += new System.EventHandler(this.rdpClient_OnAutoReconnected);
            this.rdpClient.OnAutoReconnecting2 += new AxMSTSCLib.IMsTscAxEvents_OnAutoReconnecting2EventHandler(this.rdpClient_OnAutoReconnecting2);
            // 
            // spinner
            // 
            this.spinner.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.spinner.BackColor = System.Drawing.Color.White;
            this.spinner.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Spinner;
            this.spinner.Location = new System.Drawing.Point(107, 101);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.TabIndex = 3;
            this.spinner.TabStop = false;
            // 
            // reconnectToResizeTimer
            // 
            this.reconnectToResizeTimer.Interval = 1000;
            this.reconnectToResizeTimer.Tick += new System.EventHandler(this.reconnectToResizeTimer_Tick);
            // 
            // RemoteDesktopPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(763, 431);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.rdpClient);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RemoteDesktopPane";
            this.Text = "RemoteDesktopPane";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RemoteDesktopPane_FormClosing);
            this.SizeChanged += new System.EventHandler(this.RemoteDesktopPane_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.rdpClient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private AxMSTSCLib.AxMsRdpClient9NotSafeForScripting rdpClient;
        private System.Windows.Forms.PictureBox spinner;
        private System.Windows.Forms.Timer reconnectToResizeTimer;
    }
}