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
    partial class ErrorDialog
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
            this.emojiLabel = new System.Windows.Forms.Label();
            this.errorText = new System.Windows.Forms.TextBox();
            this.headerLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // emojiLabel
            // 
            this.emojiLabel.AutoSize = true;
            this.emojiLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.emojiLabel.Location = new System.Drawing.Point(12, 9);
            this.emojiLabel.Name = "emojiLabel";
            this.emojiLabel.Size = new System.Drawing.Size(104, 108);
            this.emojiLabel.TabIndex = 0;
            this.emojiLabel.Text = ":(";
            // 
            // errorText
            // 
            this.errorText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.errorText.Font = new System.Drawing.Font("Courier New", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorText.Location = new System.Drawing.Point(140, 57);
            this.errorText.Multiline = true;
            this.errorText.Name = "errorText";
            this.errorText.ReadOnly = true;
            this.errorText.Size = new System.Drawing.Size(622, 355);
            this.errorText.TabIndex = 1;
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(136, 25);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(344, 21);
            this.headerLabel.TabIndex = 2;
            this.headerLabel.Text = "IAP Desktop experienced an unrecoverable error";
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.closeButton.Location = new System.Drawing.Point(674, 418);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(88, 31);
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // ErrorDialog
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.ControlBox = false;
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.errorText);
            this.Controls.Add(this.emojiLabel);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "ErrorDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Error";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label emojiLabel;
        private System.Windows.Forms.TextBox errorText;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.Button closeButton;
    }
}