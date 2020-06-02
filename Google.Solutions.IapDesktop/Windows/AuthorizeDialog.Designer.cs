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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuthorizeDialog));
            this.signInButton = new System.Windows.Forms.Button();
            this.spinner = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).BeginInit();
            this.SuspendLayout();
            // 
            // signInButton
            // 
            this.signInButton.Image = global::Google.Solutions.IapDesktop.Windows.Resources.btn_google_signin_dark_normal_web;
            this.signInButton.Location = new System.Drawing.Point(346, 186);
            this.signInButton.Name = "signInButton";
            this.signInButton.Size = new System.Drawing.Size(191, 46);
            this.signInButton.TabIndex = 0;
            this.signInButton.UseVisualStyleBackColor = true;
            this.signInButton.Visible = false;
            // 
            // spinner
            // 
            this.spinner.BackColor = System.Drawing.Color.White;
            this.spinner.Image = global::Google.Solutions.IapDesktop.Windows.Resources.Spinner;
            this.spinner.Location = new System.Drawing.Point(420, 113);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.TabIndex = 2;
            this.spinner.TabStop = false;
            // 
            // AuthorizeDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(634, 290);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.signInButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AuthorizeDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sign in";
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button signInButton;
        private System.Windows.Forms.PictureBox spinner;
    }
}