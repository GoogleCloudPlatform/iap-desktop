//
// Copyright 2024 Google LLC
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

namespace Google.Solutions.Terminal.Controls
{
    partial class VirtualTerminal
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.caretBlinkTimer = new System.Windows.Forms.Timer(this.components);
            this.scrollBar = new System.Windows.Forms.VScrollBar();
            this.SuspendLayout();
            // 
            // scrollBar
            // 
            this.scrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scrollBar.Location = new System.Drawing.Point(112, 0);
            this.scrollBar.Name = "scrollBar";
            this.scrollBar.Size = new System.Drawing.Size(17, 130);
            this.scrollBar.TabIndex = 0;
            this.scrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.OnScrollbarScrolled);
            this.scrollBar.ValueChanged += new System.EventHandler(OnScrollbarValueChanged);
            // 
            // VirtualTerminal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.scrollBar);
            this.Name = "VirtualTerminal";
            this.Size = new System.Drawing.Size(129, 130);
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Timer caretBlinkTimer;
        private System.Windows.Forms.VScrollBar scrollBar;
    }
}
