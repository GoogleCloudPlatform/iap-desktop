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

using Google.Solutions.Mvvm.Controls;

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
            this.bracketedPastingCheckBox = new System.Windows.Forms.CheckBox();
            this.convertTypographicQuotesCheckBox = new System.Windows.Forms.CheckBox();
            this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.scollingBox = new System.Windows.Forms.GroupBox();
            this.scrollUsingCtrlPageUpDownCheckBox = new System.Windows.Forms.CheckBox();
            this.scrollUsingCtrlHomeEndCheckBox = new System.Windows.Forms.CheckBox();
            this.themeBox = new System.Windows.Forms.GroupBox();
            this.caretStyle = new System.Windows.Forms.Label();
            this.caretStyleCombobox = new Google.Solutions.Mvvm.Controls.BindableComboBox();
            this.terminalLook = new System.Windows.Forms.Label();
            this.selectBackgroundColorButton = new System.Windows.Forms.Button();
            this.selectForegroundColorButton = new System.Windows.Forms.Button();
            this.selectFontButton = new System.Windows.Forms.Button();
            this.clipboardBox.SuspendLayout();
            this.scollingBox.SuspendLayout();
            this.themeBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // clipboardBox
            // 
            this.clipboardBox.Controls.Add(this.bracketedPastingCheckBox);
            this.clipboardBox.Controls.Add(this.convertTypographicQuotesCheckBox);
            this.clipboardBox.Controls.Add(this.copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox);
            this.clipboardBox.Controls.Add(this.copyPasteUsingCtrlCAndCtrlVEnabledCheckBox);
            this.clipboardBox.Location = new System.Drawing.Point(4, 3);
            this.clipboardBox.Name = "clipboardBox";
            this.clipboardBox.Size = new System.Drawing.Size(336, 108);
            this.clipboardBox.TabIndex = 0;
            this.clipboardBox.TabStop = false;
            this.clipboardBox.Text = "Clipboard:";
            // 
            // bracketedPastingCheckBox
            // 
            this.bracketedPastingCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.bracketedPastingCheckBox.Location = new System.Drawing.Point(18, 78);
            this.bracketedPastingCheckBox.Name = "bracketedPastingCheckBox";
            this.bracketedPastingCheckBox.Size = new System.Drawing.Size(290, 17);
            this.bracketedPastingCheckBox.TabIndex = 3;
            this.bracketedPastingCheckBox.Text = "Use bracketed-paste when pasting multiple lines of text";
            this.bracketedPastingCheckBox.UseVisualStyleBackColor = true;
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
            // 
            // scollingBox
            // 
            this.scollingBox.Controls.Add(this.scrollUsingCtrlPageUpDownCheckBox);
            this.scollingBox.Controls.Add(this.scrollUsingCtrlHomeEndCheckBox);
            this.scollingBox.Location = new System.Drawing.Point(4, 118);
            this.scollingBox.Name = "scollingBox";
            this.scollingBox.Size = new System.Drawing.Size(336, 73);
            this.scollingBox.TabIndex = 2;
            this.scollingBox.TabStop = false;
            this.scollingBox.Text = "Scrolling:";
            // 
            // scrollUsingCtrlPageUpDownCheckBox
            // 
            this.scrollUsingCtrlPageUpDownCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.scrollUsingCtrlPageUpDownCheckBox.Location = new System.Drawing.Point(18, 42);
            this.scrollUsingCtrlPageUpDownCheckBox.Name = "scrollUsingCtrlPageUpDownCheckBox";
            this.scrollUsingCtrlPageUpDownCheckBox.Size = new System.Drawing.Size(250, 17);
            this.scrollUsingCtrlPageUpDownCheckBox.TabIndex = 8;
            this.scrollUsingCtrlPageUpDownCheckBox.Text = "Use Ctrl+&PageUp/Ctrl+PageDown to scroll up/down";
            this.scrollUsingCtrlPageUpDownCheckBox.UseVisualStyleBackColor = true;
            // 
            // scrollUsingCtrlHomeEndCheckBox
            // 
            this.scrollUsingCtrlHomeEndCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.scrollUsingCtrlHomeEndCheckBox.Location = new System.Drawing.Point(18, 24);
            this.scrollUsingCtrlHomeEndCheckBox.Name = "scrollUsingCtrlHomeEndCheckBox";
            this.scrollUsingCtrlHomeEndCheckBox.Size = new System.Drawing.Size(250, 17);
            this.scrollUsingCtrlHomeEndCheckBox.TabIndex = 7;
            this.scrollUsingCtrlHomeEndCheckBox.Text = "Use Ctrl+&Home/Ctrl+End to scroll to top/bottom";
            this.scrollUsingCtrlHomeEndCheckBox.UseVisualStyleBackColor = true;
            // 
            // themeBox
            // 
            this.themeBox.Controls.Add(this.caretStyle);
            this.themeBox.Controls.Add(this.caretStyleCombobox);
            this.themeBox.Controls.Add(this.terminalLook);
            this.themeBox.Controls.Add(this.selectBackgroundColorButton);
            this.themeBox.Controls.Add(this.selectForegroundColorButton);
            this.themeBox.Controls.Add(this.selectFontButton);
            this.themeBox.Location = new System.Drawing.Point(4, 197);
            this.themeBox.Name = "themeBox";
            this.themeBox.Size = new System.Drawing.Size(336, 119);
            this.themeBox.TabIndex = 3;
            this.themeBox.TabStop = false;
            this.themeBox.Text = "Theme:";
            // 
            // caretStyle
            // 
            this.caretStyle.AutoSize = true;
            this.caretStyle.Location = new System.Drawing.Point(18, 24);
            this.caretStyle.Name = "caretStyle";
            this.caretStyle.Size = new System.Drawing.Size(59, 13);
            this.caretStyle.TabIndex = 12;
            this.caretStyle.Text = "Caret style:";
            // 
            // caretStyleCombobox
            // 
            this.caretStyleCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.caretStyleCombobox.FormattingEnabled = true;
            this.caretStyleCombobox.Location = new System.Drawing.Point(94, 20);
            this.caretStyleCombobox.Name = "caretStyleCombobox";
            this.caretStyleCombobox.Size = new System.Drawing.Size(150, 21);
            this.caretStyleCombobox.TabIndex = 8;
            // 
            // terminalLook
            // 
            this.terminalLook.BackColor = System.Drawing.Color.Black;
            this.terminalLook.ForeColor = System.Drawing.Color.White;
            this.terminalLook.Location = new System.Drawing.Point(18, 53);
            this.terminalLook.Name = "terminalLook";
            this.terminalLook.Padding = new System.Windows.Forms.Padding(3);
            this.terminalLook.Size = new System.Drawing.Size(226, 51);
            this.terminalLook.TabIndex = 1;
            this.terminalLook.Text = "[user@host ~]$";
            // 
            // selectBackgroundColorButton
            // 
            this.selectBackgroundColorButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.BackgroundColor_16;
            this.selectBackgroundColorButton.Location = new System.Drawing.Point(284, 81);
            this.selectBackgroundColorButton.Name = "selectBackgroundColorButton";
            this.selectBackgroundColorButton.Size = new System.Drawing.Size(30, 23);
            this.selectBackgroundColorButton.TabIndex = 11;
            this.selectBackgroundColorButton.UseVisualStyleBackColor = true;
            this.selectBackgroundColorButton.Click += new System.EventHandler(this.selectTerminalColorButton_Click);
            // 
            // selectForegroundColorButton
            // 
            this.selectForegroundColorButton.Image = global::Google.Solutions.IapDesktop.Extensions.Session.Properties.Resources.TextColor_16;
            this.selectForegroundColorButton.Location = new System.Drawing.Point(250, 81);
            this.selectForegroundColorButton.Name = "selectForegroundColorButton";
            this.selectForegroundColorButton.Size = new System.Drawing.Size(30, 23);
            this.selectForegroundColorButton.TabIndex = 10;
            this.selectForegroundColorButton.UseVisualStyleBackColor = true;
            this.selectForegroundColorButton.Click += new System.EventHandler(this.selectTerminalColorButton_Click);
            // 
            // selectFontButton
            // 
            this.selectFontButton.Location = new System.Drawing.Point(250, 53);
            this.selectFontButton.Name = "selectFontButton";
            this.selectFontButton.Size = new System.Drawing.Size(64, 23);
            this.selectFontButton.TabIndex = 9;
            this.selectFontButton.Text = "Font...";
            this.selectFontButton.UseVisualStyleBackColor = true;
            this.selectFontButton.Click += new System.EventHandler(this.selectFontButton_Click);
            // 
            // TerminalOptionsSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.themeBox);
            this.Controls.Add(this.scollingBox);
            this.Controls.Add(this.clipboardBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "TerminalOptionsSheet";
            this.Size = new System.Drawing.Size(343, 369);
            this.clipboardBox.ResumeLayout(false);
            this.scollingBox.ResumeLayout(false);
            this.themeBox.ResumeLayout(false);
            this.themeBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox clipboardBox;
        private System.Windows.Forms.CheckBox copyPasteUsingCtrlCAndCtrlVEnabledCheckBox;
        private System.Windows.Forms.CheckBox copyPasteUsingShiftInsertAndCtrlInsertEnabledCheckBox;
        private System.Windows.Forms.CheckBox convertTypographicQuotesCheckBox;
        private System.Windows.Forms.GroupBox scollingBox;
        private System.Windows.Forms.CheckBox scrollUsingCtrlHomeEndCheckBox;
        private System.Windows.Forms.GroupBox themeBox;
        private System.Windows.Forms.Button selectFontButton;
        private System.Windows.Forms.Label terminalLook;
        private System.Windows.Forms.Button selectForegroundColorButton;
        private System.Windows.Forms.Button selectBackgroundColorButton;
        private BindableComboBox caretStyleCombobox;
        private System.Windows.Forms.Label caretStyle;
        private System.Windows.Forms.CheckBox bracketedPastingCheckBox;
        private System.Windows.Forms.CheckBox scrollUsingCtrlPageUpDownCheckBox;
    }
}
