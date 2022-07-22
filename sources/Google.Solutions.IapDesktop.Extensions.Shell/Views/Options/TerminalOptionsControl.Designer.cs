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

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    partial class TerminalOptionsControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TerminalOptionsControl));
            this.clipboardBox = new System.Windows.Forms.GroupBox();
            this.keyboardIcon = new System.Windows.Forms.PictureBox();
            this.convertTypographicQuotesCheckBox = new System.Windows.Forms.CheckBox();
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.textSelectionBox = new System.Windows.Forms.GroupBox();
            this.selectUsingShiftArrrowEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.navigationUsingControlArrrowEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.selectAllUsingCtrlAEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.textSelectionIcon = new System.Windows.Forms.PictureBox();
            this.scollingBox = new System.Windows.Forms.GroupBox();
            this.scrollUsingCtrlUpDownCheckBox = new System.Windows.Forms.CheckBox();
            this.scrollUsingCtrlHomeEndcheckBox = new System.Windows.Forms.CheckBox();
            this.scrollingIcon = new System.Windows.Forms.PictureBox();
            this.themeBox = new System.Windows.Forms.GroupBox();
            this.fontIcon = new System.Windows.Forms.PictureBox();
            this.terminalLook = new System.Windows.Forms.Label();
            this.selectBackgroundColorButton = new System.Windows.Forms.Button();
            this.selectForegroundColorButton = new System.Windows.Forms.Button();
            this.selectFontButton = new System.Windows.Forms.Button();
            this.clipboardBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keyboardIcon)).BeginInit();
            this.textSelectionBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.textSelectionIcon)).BeginInit();
            this.scollingBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scrollingIcon)).BeginInit();
            this.themeBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fontIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // clipboardBox
            // 
            this.clipboardBox.Controls.Add(this.keyboardIcon);
            this.clipboardBox.Controls.Add(this.convertTypographicQuotesCheckBox);
            this.clipboardBox.Controls.Add(this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox);
            this.clipboardBox.Controls.Add(this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox);
            this.clipboardBox.Location = new System.Drawing.Point(4, 3);
            this.clipboardBox.Name = "clipboardBox";
            this.clipboardBox.Size = new System.Drawing.Size(336, 85);
            this.clipboardBox.TabIndex = 2;
            this.clipboardBox.TabStop = false;
            this.clipboardBox.Text = "Clipboard:";
            // 
            // keyboardIcon
            // 
            this.keyboardIcon.Image = ((System.Drawing.Image)(resources.GetObject("keyboardIcon.Image")));
            this.keyboardIcon.Location = new System.Drawing.Point(10, 21);
            this.keyboardIcon.Name = "keyboardIcon";
            this.keyboardIcon.Size = new System.Drawing.Size(36, 36);
            this.keyboardIcon.TabIndex = 2;
            this.keyboardIcon.TabStop = false;
            // 
            // convertTypographicQuotesCheckBox
            // 
            this.convertTypographicQuotesCheckBox.AutoSize = true;
            this.convertTypographicQuotesCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.convertTypographicQuotesCheckBox.Location = new System.Drawing.Point(58, 58);
            this.convertTypographicQuotesCheckBox.Name = "convertTypographicQuotesCheckBox";
            this.convertTypographicQuotesCheckBox.Size = new System.Drawing.Size(222, 17);
            this.convertTypographicQuotesCheckBox.TabIndex = 3;
            this.convertTypographicQuotesCheckBox.Text = "Convert typographic &quotes when pasting";
            this.convertTypographicQuotesCheckBox.UseVisualStyleBackColor = true;
            // 
            // copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox
            // 
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.AutoSize = true;
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Location = new System.Drawing.Point(58, 40);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Name = "copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox";
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Size = new System.Drawing.Size(222, 17);
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.TabIndex = 2;
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.Text = "Use Ctrl+&Insert/Shift+Insert to copy/paste";
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // copyPasteUsingCtrlCAndCtrlVEnabledCheckBox
            // 
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.AutoSize = true;
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Location = new System.Drawing.Point(58, 22);
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Name = "copyPasteUsingCtrlCAndCtrlVEnabledCheckBox";
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Size = new System.Drawing.Size(178, 17);
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.TabIndex = 1;
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.Text = "Use Ctrl+C/Ctrl+&V to copy/paste";
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // textSelectionBox
            // 
            this.textSelectionBox.Controls.Add(this.selectUsingShiftArrrowEnabledCheckBox);
            this.textSelectionBox.Controls.Add(this.navigationUsingControlArrrowEnabledCheckBox);
            this.textSelectionBox.Controls.Add(this.selectAllUsingCtrlAEnabledCheckBox);
            this.textSelectionBox.Controls.Add(this.textSelectionIcon);
            this.textSelectionBox.Location = new System.Drawing.Point(4, 94);
            this.textSelectionBox.Name = "textSelectionBox";
            this.textSelectionBox.Size = new System.Drawing.Size(336, 87);
            this.textSelectionBox.TabIndex = 3;
            this.textSelectionBox.TabStop = false;
            this.textSelectionBox.Text = "Text selection:";
            // 
            // selectUsingShiftArrrowEnabledCheckBox
            // 
            this.selectUsingShiftArrrowEnabledCheckBox.AutoSize = true;
            this.selectUsingShiftArrrowEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.selectUsingShiftArrrowEnabledCheckBox.Location = new System.Drawing.Point(58, 21);
            this.selectUsingShiftArrrowEnabledCheckBox.Name = "selectUsingShiftArrrowEnabledCheckBox";
            this.selectUsingShiftArrrowEnabledCheckBox.Size = new System.Drawing.Size(185, 17);
            this.selectUsingShiftArrrowEnabledCheckBox.TabIndex = 4;
            this.selectUsingShiftArrrowEnabledCheckBox.Text = "Use Shift+Arrow key to &select text";
            this.selectUsingShiftArrrowEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // navigationUsingControlArrrowEnabledCheckBox
            // 
            this.navigationUsingControlArrrowEnabledCheckBox.AutoSize = true;
            this.navigationUsingControlArrrowEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.navigationUsingControlArrrowEnabledCheckBox.Location = new System.Drawing.Point(58, 57);
            this.navigationUsingControlArrrowEnabledCheckBox.Name = "navigationUsingControlArrrowEnabledCheckBox";
            this.navigationUsingControlArrrowEnabledCheckBox.Size = new System.Drawing.Size(260, 17);
            this.navigationUsingControlArrrowEnabledCheckBox.TabIndex = 6;
            this.navigationUsingControlArrrowEnabledCheckBox.Text = "Use Ctrl+Left/Right to jump to previous/next &word";
            this.navigationUsingControlArrrowEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // selectAllUsingCtrlAEnabledCheckBox
            // 
            this.selectAllUsingCtrlAEnabledCheckBox.AutoSize = true;
            this.selectAllUsingCtrlAEnabledCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.selectAllUsingCtrlAEnabledCheckBox.Location = new System.Drawing.Point(58, 39);
            this.selectAllUsingCtrlAEnabledCheckBox.Name = "selectAllUsingCtrlAEnabledCheckBox";
            this.selectAllUsingCtrlAEnabledCheckBox.Size = new System.Drawing.Size(152, 17);
            this.selectAllUsingCtrlAEnabledCheckBox.TabIndex = 5;
            this.selectAllUsingCtrlAEnabledCheckBox.Text = "Use Ctrl+&A to select all text";
            this.selectAllUsingCtrlAEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // textSelectionIcon
            // 
            this.textSelectionIcon.Image = ((System.Drawing.Image)(resources.GetObject("textSelectionIcon.Image")));
            this.textSelectionIcon.Location = new System.Drawing.Point(10, 21);
            this.textSelectionIcon.Name = "textSelectionIcon";
            this.textSelectionIcon.Size = new System.Drawing.Size(36, 36);
            this.textSelectionIcon.TabIndex = 3;
            this.textSelectionIcon.TabStop = false;
            // 
            // scollingBox
            // 
            this.scollingBox.Controls.Add(this.scrollUsingCtrlUpDownCheckBox);
            this.scollingBox.Controls.Add(this.scrollUsingCtrlHomeEndcheckBox);
            this.scollingBox.Controls.Add(this.scrollingIcon);
            this.scollingBox.Location = new System.Drawing.Point(4, 189);
            this.scollingBox.Name = "scollingBox";
            this.scollingBox.Size = new System.Drawing.Size(336, 87);
            this.scollingBox.TabIndex = 7;
            this.scollingBox.TabStop = false;
            this.scollingBox.Text = "Scrolling:";
            // 
            // scrollUsingCtrlUpDownCheckBox
            // 
            this.scrollUsingCtrlUpDownCheckBox.AutoSize = true;
            this.scrollUsingCtrlUpDownCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.scrollUsingCtrlUpDownCheckBox.Location = new System.Drawing.Point(58, 21);
            this.scrollUsingCtrlUpDownCheckBox.Name = "scrollUsingCtrlUpDownCheckBox";
            this.scrollUsingCtrlUpDownCheckBox.Size = new System.Drawing.Size(155, 17);
            this.scrollUsingCtrlUpDownCheckBox.TabIndex = 7;
            this.scrollUsingCtrlUpDownCheckBox.Text = "Use Ctrl+Up/&Down to scroll";
            this.scrollUsingCtrlUpDownCheckBox.UseVisualStyleBackColor = true;
            // 
            // scrollUsingCtrlHomeEndcheckBox
            // 
            this.scrollUsingCtrlHomeEndcheckBox.AutoSize = true;
            this.scrollUsingCtrlHomeEndcheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.scrollUsingCtrlHomeEndcheckBox.Location = new System.Drawing.Point(58, 39);
            this.scrollUsingCtrlHomeEndcheckBox.Name = "scrollUsingCtrlHomeEndcheckBox";
            this.scrollUsingCtrlHomeEndcheckBox.Size = new System.Drawing.Size(227, 17);
            this.scrollUsingCtrlHomeEndcheckBox.TabIndex = 8;
            this.scrollUsingCtrlHomeEndcheckBox.Text = "Use Ctrl+&Home/End to scroll to top/bottom";
            this.scrollUsingCtrlHomeEndcheckBox.UseVisualStyleBackColor = true;
            // 
            // scrollingIcon
            // 
            this.scrollingIcon.Image = ((System.Drawing.Image)(resources.GetObject("scrollingIcon.Image")));
            this.scrollingIcon.Location = new System.Drawing.Point(10, 21);
            this.scrollingIcon.Name = "scrollingIcon";
            this.scrollingIcon.Size = new System.Drawing.Size(36, 36);
            this.scrollingIcon.TabIndex = 3;
            this.scrollingIcon.TabStop = false;
            // 
            // themeBox
            // 
            this.themeBox.Controls.Add(this.fontIcon);
            this.themeBox.Controls.Add(this.terminalLook);
            this.themeBox.Controls.Add(this.selectBackgroundColorButton);
            this.themeBox.Controls.Add(this.selectForegroundColorButton);
            this.themeBox.Controls.Add(this.selectFontButton);
            this.themeBox.Location = new System.Drawing.Point(4, 282);
            this.themeBox.Name = "themeBox";
            this.themeBox.Size = new System.Drawing.Size(336, 87);
            this.themeBox.TabIndex = 8;
            this.themeBox.TabStop = false;
            this.themeBox.Text = "Theme:";
            // 
            // fontIcon
            // 
            this.fontIcon.Image = ((System.Drawing.Image)(resources.GetObject("fontIcon.Image")));
            this.fontIcon.Location = new System.Drawing.Point(10, 21);
            this.fontIcon.Name = "fontIcon";
            this.fontIcon.Size = new System.Drawing.Size(36, 36);
            this.fontIcon.TabIndex = 4;
            this.fontIcon.TabStop = false;
            // 
            // terminalLook
            // 
            this.terminalLook.BackColor = System.Drawing.Color.Black;
            this.terminalLook.ForeColor = System.Drawing.Color.White;
            this.terminalLook.Location = new System.Drawing.Point(55, 19);
            this.terminalLook.Name = "terminalLook";
            this.terminalLook.Padding = new System.Windows.Forms.Padding(3);
            this.terminalLook.Size = new System.Drawing.Size(203, 51);
            this.terminalLook.TabIndex = 1;
            this.terminalLook.Text = "[user@host ~]$";
            // 
            // selectBackgroundColorButton
            // 
            this.selectBackgroundColorButton.Image = global::Google.Solutions.IapDesktop.Extensions.Shell.Properties.Resources.BackgroundColor_16;
            this.selectBackgroundColorButton.Location = new System.Drawing.Point(300, 47);
            this.selectBackgroundColorButton.Name = "selectBackgroundColorButton";
            this.selectBackgroundColorButton.Size = new System.Drawing.Size(30, 23);
            this.selectBackgroundColorButton.TabIndex = 9;
            this.selectBackgroundColorButton.UseVisualStyleBackColor = true;
            this.selectBackgroundColorButton.Click += new System.EventHandler(this.selectTerminalColorButton_Click);
            // 
            // selectForegroundColorButton
            // 
            this.selectForegroundColorButton.Image = global::Google.Solutions.IapDesktop.Extensions.Shell.Properties.Resources.TextColor_16;
            this.selectForegroundColorButton.Location = new System.Drawing.Point(266, 47);
            this.selectForegroundColorButton.Name = "selectForegroundColorButton";
            this.selectForegroundColorButton.Size = new System.Drawing.Size(30, 23);
            this.selectForegroundColorButton.TabIndex = 9;
            this.selectForegroundColorButton.UseVisualStyleBackColor = true;
            this.selectForegroundColorButton.Click += new System.EventHandler(this.selectTerminalColorButton_Click);
            // 
            // selectFontButton
            // 
            this.selectFontButton.Location = new System.Drawing.Point(266, 19);
            this.selectFontButton.Name = "selectFontButton";
            this.selectFontButton.Size = new System.Drawing.Size(64, 23);
            this.selectFontButton.TabIndex = 9;
            this.selectFontButton.Text = "Font...";
            this.selectFontButton.UseVisualStyleBackColor = true;
            this.selectFontButton.Click += new System.EventHandler(this.selectFontButton_Click);
            // 
            // TerminalOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.themeBox);
            this.Controls.Add(this.scollingBox);
            this.Controls.Add(this.textSelectionBox);
            this.Controls.Add(this.clipboardBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "TerminalOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.clipboardBox.ResumeLayout(false);
            this.clipboardBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keyboardIcon)).EndInit();
            this.textSelectionBox.ResumeLayout(false);
            this.textSelectionBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.textSelectionIcon)).EndInit();
            this.scollingBox.ResumeLayout(false);
            this.scollingBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scrollingIcon)).EndInit();
            this.themeBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fontIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox clipboardBox;
        private System.Windows.Forms.PictureBox keyboardIcon;
        private System.Windows.Forms.CheckBox copyPasteUsingCtrlCAndCtrlVEnabledCheckBox;
        private System.Windows.Forms.CheckBox copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox;
        private System.Windows.Forms.CheckBox convertTypographicQuotesCheckBox;
        private System.Windows.Forms.GroupBox textSelectionBox;
        private System.Windows.Forms.PictureBox textSelectionIcon;
        private System.Windows.Forms.CheckBox selectUsingShiftArrrowEnabledCheckBox;
        private System.Windows.Forms.CheckBox selectAllUsingCtrlAEnabledCheckBox;
        private System.Windows.Forms.CheckBox navigationUsingControlArrrowEnabledCheckBox;
        private System.Windows.Forms.GroupBox scollingBox;
        private System.Windows.Forms.CheckBox scrollUsingCtrlUpDownCheckBox;
        private System.Windows.Forms.CheckBox scrollUsingCtrlHomeEndcheckBox;
        private System.Windows.Forms.PictureBox scrollingIcon;
        private System.Windows.Forms.GroupBox themeBox;
        private System.Windows.Forms.Button selectFontButton;
        private System.Windows.Forms.Label terminalLook;
        private System.Windows.Forms.PictureBox fontIcon;
        private System.Windows.Forms.Button selectForegroundColorButton;
        private System.Windows.Forms.Button selectBackgroundColorButton;
    }
}
