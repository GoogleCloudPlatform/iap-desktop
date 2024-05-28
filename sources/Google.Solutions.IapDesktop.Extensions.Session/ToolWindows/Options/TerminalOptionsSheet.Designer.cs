//
// Copyright 2021 Google LLC
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

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    partial class TerminalOptionsSheet
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
            this.clipboardBox = new System.Windows.Forms.GroupBox();
            this.convertTypographicQuotesCheckBox = new System.Windows.Forms.CheckBox();
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.textSelectionBox = new System.Windows.Forms.GroupBox();
            this.selectUsingShiftArrrowEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.navigationUsingControlArrrowEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.selectAllUsingCtrlAEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.scollingBox = new System.Windows.Forms.GroupBox();
            this.scrollUsingCtrlUpDownCheckBox = new System.Windows.Forms.CheckBox();
            this.scrollUsingCtrlHomeEndcheckBox = new System.Windows.Forms.CheckBox();
            this.themeBox = new System.Windows.Forms.GroupBox();
            this.terminalLook = new System.Windows.Forms.Label();
            this.selectBackgroundColorButton = new System.Windows.Forms.Button();
            this.selectForegroundColorButton = new System.Windows.Forms.Button();
            this.selectFontButton = new System.Windows.Forms.Button();
            this.clipboardBox.SuspendLayout();
            this.textSelectionBox.SuspendLayout();
            this.scollingBox.SuspendLayout();
            this.themeBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // clipboardBox
            // 
            this.clipboardBox.Controls.Add(this.convertTypographicQuotesCheckBox);
            this.clipboardBox.Controls.Add(this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox);
            this.clipboardBox.Controls.Add(this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox);
            this.clipboardBox.Location = new System.Drawing.Point(4, 3);
            this.clipboardBox.Name = "clipboardBox";
            this.clipboardBox.Size = new System.Drawing.Size(336, 89);
            this.clipboardBox.TabIndex = 0;
            this.clipboardBox.TabStop = false;
            this.clipboardBox.Text = "Clipboard:";
            // 
            // convertTypographicQuotesCheckBox
            // 
            this.convertTypographicQuotesCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.convertTypographicQuotesCheckBox.Location = new System.Drawing.Point(18, 60);
            this.convertTypographicQuotesCheckBox.Name = "convertTypographicQuotesCheckBox";
            this.convertTypographicQuotesCheckBox.Size = new System.Drawing.Size(222, 17);
            this.convertTypographicQuotesCheckBox.TabIndex = 2;
            this.convertTypographicQuotesCheckBox.Text = "Convert typographic &quotes when pasting";
            this.convertTypographicQuotesCheckBox.UseVisualStyleBackColor = true;
            this.convertTypographicQuotesCheckBox.AutoSize = false;
            // 
            // copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox
            // 
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Location = new System.Drawing.Point(18, 42);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Name = "copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox";
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Size = new System.Drawing.Size(222, 17);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.TabIndex = 1;
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Text = "Use Ctrl+&Insert/Shift+Insert to copy/paste";
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.UseVisualStyleBackColor = true;
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.AutoSize = false;
            // 
            // copyPasteUsingCtrlCAndCtrlVEnabledCheckBox
            // 
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Location = new System.Drawing.Point(18, 24);
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Name = "copyPasteUsingCtrlCAndCtrlVEnabledCheckBox";
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Size = new System.Drawing.Size(178, 17);
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.TabIndex = 0;
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Text = "Use Ctrl+C/Ctrl+&V to copy/paste";
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.UseVisualStyleBackColor = true;
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.AutoSize = false;
            // 
            // textSelectionBox
            // 
            this.textSelectionBox.Controls.Add(this.selectUsingShiftArrrowEnabledCheckBox);
            this.textSelectionBox.Controls.Add(this.navigationUsingControlArrrowEnabledCheckBox);
            this.textSelectionBox.Controls.Add(this.selectAllUsingCtrlAEnabledCheckBox);
            this.textSelectionBox.Location = new System.Drawing.Point(4, 98);
            this.textSelectionBox.Name = "textSelectionBox";
            this.textSelectionBox.Size = new System.Drawing.Size(336, 89);
            this.textSelectionBox.TabIndex = 1;
            this.textSelectionBox.TabStop = false;
            this.textSelectionBox.Text = "Text selection:";
            // 
            // selectUsingShiftArrrowEnabledCheckBox
            // 
            this.selectUsingShiftArrrowEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.selectUsingShiftArrrowEnabledCheckBox.Location = new System.Drawing.Point(18, 24);
            this.selectUsingShiftArrrowEnabledCheckBox.Name = "selectUsingShiftArrrowEnabledCheckBox";
            this.selectUsingShiftArrrowEnabledCheckBox.Size = new System.Drawing.Size(185, 17);
            this.selectUsingShiftArrrowEnabledCheckBox.TabIndex = 3;
            this.selectUsingShiftArrrowEnabledCheckBox.Text = "Use Shift+Arrow key to &select text";
            this.selectUsingShiftArrrowEnabledCheckBox.UseVisualStyleBackColor = true;
            this.selectUsingShiftArrrowEnabledCheckBox.AutoSize = false;
            // 
            // navigationUsingControlArrrowEnabledCheckBox
            // 
            this.navigationUsingControlArrrowEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.navigationUsingControlArrrowEnabledCheckBox.Location = new System.Drawing.Point(18, 60);
            this.navigationUsingControlArrrowEnabledCheckBox.Name = "navigationUsingControlArrrowEnabledCheckBox";
            this.navigationUsingControlArrrowEnabledCheckBox.Size = new System.Drawing.Size(260, 17);
            this.navigationUsingControlArrrowEnabledCheckBox.TabIndex = 5;
            this.navigationUsingControlArrrowEnabledCheckBox.Text = "Use Ctrl+Left/Right to jump to previous/next &word";
            this.navigationUsingControlArrrowEnabledCheckBox.UseVisualStyleBackColor = true;
            this.navigationUsingControlArrrowEnabledCheckBox.AutoSize = false;
            // 
            // selectAllUsingCtrlAEnabledCheckBox
            // 
            this.selectAllUsingCtrlAEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.selectAllUsingCtrlAEnabledCheckBox.Location = new System.Drawing.Point(18, 42);
            this.selectAllUsingCtrlAEnabledCheckBox.Name = "selectAllUsingCtrlAEnabledCheckBox";
            this.selectAllUsingCtrlAEnabledCheckBox.Size = new System.Drawing.Size(152, 17);
            this.selectAllUsingCtrlAEnabledCheckBox.TabIndex = 4;
            this.selectAllUsingCtrlAEnabledCheckBox.Text = "Use Ctrl+&A to select all text";
            this.selectAllUsingCtrlAEnabledCheckBox.UseVisualStyleBackColor = true;
            this.selectAllUsingCtrlAEnabledCheckBox.AutoSize = false;
            // 
            // scollingBox
            // 
            this.scollingBox.Controls.Add(this.scrollUsingCtrlUpDownCheckBox);
            this.scollingBox.Controls.Add(this.scrollUsingCtrlHomeEndcheckBox);
            this.scollingBox.Location = new System.Drawing.Point(4, 193);
            this.scollingBox.Name = "scollingBox";
            this.scollingBox.Size = new System.Drawing.Size(336, 73);
            this.scollingBox.TabIndex = 2;
            this.scollingBox.TabStop = false;
            this.scollingBox.Text = "Scrolling:";
            // 
            // scrollUsingCtrlUpDownCheckBox
            // 
            this.scrollUsingCtrlUpDownCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.scrollUsingCtrlUpDownCheckBox.Location = new System.Drawing.Point(18, 24);
            this.scrollUsingCtrlUpDownCheckBox.Name = "scrollUsingCtrlUpDownCheckBox";
            this.scrollUsingCtrlUpDownCheckBox.Size = new System.Drawing.Size(155, 17);
            this.scrollUsingCtrlUpDownCheckBox.TabIndex = 6;
            this.scrollUsingCtrlUpDownCheckBox.Text = "Use Ctrl+Up/&Down to scroll";
            this.scrollUsingCtrlUpDownCheckBox.UseVisualStyleBackColor = true;
            this.scrollUsingCtrlUpDownCheckBox.AutoSize = false;
            // 
            // scrollUsingCtrlHomeEndcheckBox
            // 
            this.scrollUsingCtrlHomeEndcheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.scrollUsingCtrlHomeEndcheckBox.Location = new System.Drawing.Point(18, 42);
            this.scrollUsingCtrlHomeEndcheckBox.Name = "scrollUsingCtrlHomeEndcheckBox";
            this.scrollUsingCtrlHomeEndcheckBox.Size = new System.Drawing.Size(227, 17);
            this.scrollUsingCtrlHomeEndcheckBox.TabIndex = 7;
            this.scrollUsingCtrlHomeEndcheckBox.Text = "Use Ctrl+&Home/End to scroll to top/bottom";
            this.scrollUsingCtrlHomeEndcheckBox.UseVisualStyleBackColor = true;
            this.scrollUsingCtrlHomeEndcheckBox.AutoSize = false;
            // 
            // themeBox
            // 
            this.themeBox.Controls.Add(this.terminalLook);
            this.themeBox.Controls.Add(this.selectBackgroundColorButton);
            this.themeBox.Controls.Add(this.selectForegroundColorButton);
            this.themeBox.Controls.Add(this.selectFontButton);
            this.themeBox.Location = new System.Drawing.Point(4, 272);
            this.themeBox.Name = "themeBox";
            this.themeBox.Size = new System.Drawing.Size(336, 87);
            this.themeBox.TabIndex = 3;
            this.themeBox.TabStop = false;
            this.themeBox.Text = "Theme:";
            // 
            // terminalLook
            // 
            this.terminalLook.BackColor = System.Drawing.Color.Black;
            this.terminalLook.ForeColor = System.Drawing.Color.White;
            this.terminalLook.Location = new System.Drawing.Point(18, 24);
            this.terminalLook.Name = "terminalLook";
            this.terminalLook.Padding = new System.Windows.Forms.Padding(3);
            this.terminalLook.Size = new System.Drawing.Size(226, 51);
            this.terminalLook.TabIndex = 1;
            this.terminalLook.Text = "[user@host ~]$";
            // 
            // selectBackgroundColorButton
            // 
            this.selectBackgroundColorButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.BackgroundColor_16;
            this.selectBackgroundColorButton.Location = new System.Drawing.Point(284, 52);
            this.selectBackgroundColorButton.Name = "selectBackgroundColorButton";
            this.selectBackgroundColorButton.Size = new System.Drawing.Size(30, 23);
            this.selectBackgroundColorButton.TabIndex = 10;
            this.selectBackgroundColorButton.UseVisualStyleBackColor = true;
            this.selectBackgroundColorButton.Click += new System.EventHandler(this.selectTerminalColorButton_Click);
            // 
            // selectForegroundColorButton
            // 
            this.selectForegroundColorButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.TextColor_16;
            this.selectForegroundColorButton.Location = new System.Drawing.Point(250, 52);
            this.selectForegroundColorButton.Name = "selectForegroundColorButton";
            this.selectForegroundColorButton.Size = new System.Drawing.Size(30, 23);
            this.selectForegroundColorButton.TabIndex = 9;
            this.selectForegroundColorButton.UseVisualStyleBackColor = true;
            this.selectForegroundColorButton.Click += new System.EventHandler(this.selectTerminalColorButton_Click);
            // 
            // selectFontButton
            // 
            this.selectFontButton.Location = new System.Drawing.Point(250, 24);
            this.selectFontButton.Name = "selectFontButton";
            this.selectFontButton.Size = new System.Drawing.Size(64, 23);
            this.selectFontButton.TabIndex = 8;
            this.selectFontButton.Text = "Font...";
            this.selectFontButton.UseVisualStyleBackColor = true;
            this.selectFontButton.Click += new System.EventHandler(this.selectFontButton_Click);
            // 
            // TerminalOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.themeBox);
            this.Controls.Add(this.scollingBox);
            this.Controls.Add(this.textSelectionBox);
            this.Controls.Add(this.clipboardBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "TerminalOptionsSheet";
            this.Size = new System.Drawing.Size(343, 369);
            this.clipboardBox.ResumeLayout(false);
            this.clipboardBox.PerformLayout();
            this.textSelectionBox.ResumeLayout(false);
            this.textSelectionBox.PerformLayout();
            this.scollingBox.ResumeLayout(false);
            this.scollingBox.PerformLayout();
            this.themeBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox clipboardBox;
        private System.Windows.Forms.CheckBox copyPasteUsingCtrlCAndCtrlVEnabledCheckBox;
        private System.Windows.Forms.CheckBox copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox;
        private System.Windows.Forms.CheckBox convertTypographicQuotesCheckBox;
        private System.Windows.Forms.GroupBox textSelectionBox;
        private System.Windows.Forms.CheckBox selectUsingShiftArrrowEnabledCheckBox;
        private System.Windows.Forms.CheckBox selectAllUsingCtrlAEnabledCheckBox;
        private System.Windows.Forms.CheckBox navigationUsingControlArrrowEnabledCheckBox;
        private System.Windows.Forms.GroupBox scollingBox;
        private System.Windows.Forms.CheckBox scrollUsingCtrlUpDownCheckBox;
        private System.Windows.Forms.CheckBox scrollUsingCtrlHomeEndcheckBox;
        private System.Windows.Forms.GroupBox themeBox;
        private System.Windows.Forms.Button selectFontButton;
        private System.Windows.Forms.Label terminalLook;
        private System.Windows.Forms.Button selectForegroundColorButton;
        private System.Windows.Forms.Button selectBackgroundColorButton;
    }
}
