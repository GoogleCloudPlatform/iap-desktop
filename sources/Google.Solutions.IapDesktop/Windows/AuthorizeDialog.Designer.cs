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

namespace Google.Solutions.IapDesktop.Windows
{
    partial class AuthorizeDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuthorizeDialog));
            this.cancelSignInLink = new System.Windows.Forms.LinkLabel();
            this.cancelSignInLabel = new System.Windows.Forms.Label();
            this.signInMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.signInWithChromeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signInWithChromeGuestMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signInWithDefaultBrowserMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.introLabel = new System.Windows.Forms.Label();
            this.gradient = new System.Windows.Forms.PictureBox();
            this.spinner = new Google.Solutions.Mvvm.Controls.CircularProgressBar();
            this.signInButton = new Google.Solutions.Mvvm.Controls.DropDownButton();
            this.headerLabel = new Google.Solutions.Mvvm.Controls.HeaderLabel();
            this.signInMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gradient)).BeginInit();
            this.SuspendLayout();
            // 
            // cancelSignInLink
            // 
            this.cancelSignInLink.BackColor = System.Drawing.Color.Transparent;
            this.cancelSignInLink.Location = new System.Drawing.Point(100, 378);
            this.cancelSignInLink.Name = "cancelSignInLink";
            this.cancelSignInLink.Size = new System.Drawing.Size(134, 23);
            this.cancelSignInLink.TabIndex = 3;
            this.cancelSignInLink.TabStop = true;
            this.cancelSignInLink.Text = "Cancel sign-in";
            this.cancelSignInLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cancelSignInLabel
            // 
            this.cancelSignInLabel.BackColor = System.Drawing.Color.Transparent;
            this.cancelSignInLabel.Location = new System.Drawing.Point(6, 343);
            this.cancelSignInLabel.Name = "cancelSignInLabel";
            this.cancelSignInLabel.Size = new System.Drawing.Size(322, 33);
            this.cancelSignInLabel.TabIndex = 4;
            this.cancelSignInLabel.Text = "Waiting for you to sign in\r\nusing a web browser ...";
            this.cancelSignInLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // signInMenuStrip
            // 
            this.signInMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.signInWithChromeMenuItem,
            this.signInWithChromeGuestMenuItem,
            this.signInWithDefaultBrowserMenuItem});
            this.signInMenuStrip.Name = "signInMenuStrip";
            this.signInMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.signInMenuStrip.Size = new System.Drawing.Size(258, 70);
            // 
            // signInWithChromeMenuItem
            // 
            this.signInWithChromeMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("signInWithChromeMenuItem.Image")));
            this.signInWithChromeMenuItem.Name = "signInWithChromeMenuItem";
            this.signInWithChromeMenuItem.Size = new System.Drawing.Size(257, 22);
            this.signInWithChromeMenuItem.Text = "Sign in with &Chrome";
            // 
            // signInWithChromeGuestMenuItem
            // 
            this.signInWithChromeGuestMenuItem.Name = "signInWithChromeGuestMenuItem";
            this.signInWithChromeGuestMenuItem.Size = new System.Drawing.Size(257, 22);
            this.signInWithChromeGuestMenuItem.Text = "Sign in with Chrome (&Guest mode)";
            // 
            // signInWithDefaultBrowserMenuItem
            // 
            this.signInWithDefaultBrowserMenuItem.Name = "signInWithDefaultBrowserMenuItem";
            this.signInWithDefaultBrowserMenuItem.Size = new System.Drawing.Size(257, 22);
            this.signInWithDefaultBrowserMenuItem.Text = "Sign in with &default browser";
            // 
            // introLabel
            // 
            this.introLabel.AutoSize = true;
            this.introLabel.BackColor = System.Drawing.Color.Transparent;
            this.introLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.introLabel.Location = new System.Drawing.Point(45, 116);
            this.introLabel.Name = "introLabel";
            this.introLabel.Size = new System.Drawing.Size(248, 51);
            this.introLabel.TabIndex = 5;
            this.introLabel.Text = "Sign in and start using IAP Desktop \r\nto securely connect to your Linux and \r\nWin" +
    "dows VMs";
            // 
            // gradient
            // 
            this.gradient.Image = global::Google.Solutions.IapDesktop.Properties.Resources.AccentGradient_450;
            this.gradient.Location = new System.Drawing.Point(0, 442);
            this.gradient.Name = "gradient";
            this.gradient.Size = new System.Drawing.Size(450, 10);
            this.gradient.TabIndex = 6;
            this.gradient.TabStop = false;
            // 
            // spinner
            // 
            this.spinner.Indeterminate = true;
            this.spinner.LineWidth = 5;
            this.spinner.Location = new System.Drawing.Point(145, 296);
            this.spinner.Maximum = 100;
            this.spinner.MinimumSize = new System.Drawing.Size(15, 15);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.Speed = 3;
            this.spinner.TabIndex = 2;
            this.spinner.TabStop = false;
            this.spinner.Value = 0;
            // 
            // signInButton
            // 
            this.signInButton.BackColor = System.Drawing.Color.White;
            this.signInButton.Image = global::Google.Solutions.IapDesktop.Properties.Resources.GoogleSignIn_24;
            this.signInButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.signInButton.Location = new System.Drawing.Point(99, 304);
            this.signInButton.Menu = this.signInMenuStrip;
            this.signInButton.Name = "signInButton";
            this.signInButton.Size = new System.Drawing.Size(137, 36);
            this.signInButton.TabIndex = 0;
            this.signInButton.Text = "Sign in";
            this.signInButton.UseVisualStyleBackColor = false;
            this.signInButton.Visible = false;
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(70, 34);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(195, 45);
            this.headerLabel.TabIndex = 7;
            this.headerLabel.Text = "IAP Desktop";
            // 
            // AuthorizeDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 451);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.gradient);
            this.Controls.Add(this.introLabel);
            this.Controls.Add(this.cancelSignInLabel);
            this.Controls.Add(this.cancelSignInLink);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.signInButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AuthorizeDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sign in";
            this.signInMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gradient)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DropDownButton signInButton;
        private CircularProgressBar spinner;
        private System.Windows.Forms.LinkLabel cancelSignInLink;
        private System.Windows.Forms.Label cancelSignInLabel;
        private System.Windows.Forms.ContextMenuStrip signInMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem signInWithChromeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signInWithDefaultBrowserMenuItem;
        private System.Windows.Forms.Label introLabel;
        private System.Windows.Forms.ToolStripMenuItem signInWithChromeGuestMenuItem;
        private System.Windows.Forms.PictureBox gradient;
        private Mvvm.Controls.HeaderLabel headerLabel;
    }
}