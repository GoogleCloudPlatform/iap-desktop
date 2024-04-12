//
// Copyright 2023 Google LLC
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

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    partial class DebugCommonControlsView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DebugCommonControlsView));
            this.regularButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label = new System.Windows.Forms.Label();
            this.linkLabel = new System.Windows.Forms.LinkLabel();
            this.checkBox = new System.Windows.Forms.CheckBox();
            this.radioButton = new System.Windows.Forms.RadioButton();
            this.textBox = new System.Windows.Forms.TextBox();
            this.multilineTextBox = new System.Windows.Forms.TextBox();
            this.textBoxEnabled = new System.Windows.Forms.CheckBox();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.comboBox = new System.Windows.Forms.ComboBox();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.numericUpDown = new System.Windows.Forms.NumericUpDown();
            this.progressBar = new Google.Solutions.Mvvm.Controls.LinearProgressBar();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.listView = new System.Windows.Forms.ListView();
            this.nameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.valueColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.readOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.circularProgressBar1 = new Google.Solutions.Mvvm.Controls.CircularProgressBar();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.oneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.twoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.dropDownButton = new Google.Solutions.Mvvm.Controls.DropDownButton();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // regularButton
            // 
            this.regularButton.Location = new System.Drawing.Point(11, 62);
            this.regularButton.Name = "regularButton";
            this.regularButton.Size = new System.Drawing.Size(75, 23);
            this.regularButton.TabIndex = 0;
            this.regularButton.Text = "Regular";
            this.regularButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(92, 62);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(173, 62);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(11, 92);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(33, 13);
            this.label.TabIndex = 3;
            this.label.Text = "Label";
            // 
            // linkLabel
            // 
            this.linkLabel.AutoSize = true;
            this.linkLabel.Location = new System.Drawing.Point(11, 113);
            this.linkLabel.Name = "linkLabel";
            this.linkLabel.Size = new System.Drawing.Size(27, 13);
            this.linkLabel.TabIndex = 4;
            this.linkLabel.TabStop = true;
            this.linkLabel.Text = "Link";
            // 
            // checkBox
            // 
            this.checkBox.AutoSize = true;
            this.checkBox.Location = new System.Drawing.Point(14, 139);
            this.checkBox.Name = "checkBox";
            this.checkBox.Size = new System.Drawing.Size(74, 17);
            this.checkBox.TabIndex = 5;
            this.checkBox.Text = "Checkbox";
            this.checkBox.UseVisualStyleBackColor = true;
            // 
            // radioButton
            // 
            this.radioButton.AutoSize = true;
            this.radioButton.Location = new System.Drawing.Point(14, 162);
            this.radioButton.Name = "radioButton";
            this.radioButton.Size = new System.Drawing.Size(86, 17);
            this.radioButton.TabIndex = 6;
            this.radioButton.TabStop = true;
            this.radioButton.Text = "Radio button";
            this.radioButton.UseVisualStyleBackColor = true;
            // 
            // textBox
            // 
            this.textBox.Location = new System.Drawing.Point(14, 196);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(153, 20);
            this.textBox.TabIndex = 7;
            this.textBox.Text = "Text";
            // 
            // multilineTextBox
            // 
            this.multilineTextBox.Location = new System.Drawing.Point(14, 222);
            this.multilineTextBox.Multiline = true;
            this.multilineTextBox.Name = "multilineTextBox";
            this.multilineTextBox.ReadOnly = true;
            this.multilineTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.multilineTextBox.Size = new System.Drawing.Size(153, 55);
            this.multilineTextBox.TabIndex = 7;
            this.multilineTextBox.Text = "Text 1\r\nText 2\r\nText 3\r\nText 4\r\nText 5\r\nText 6\r\nText 7";
            // 
            // textBoxEnabled
            // 
            this.textBoxEnabled.AutoSize = true;
            this.textBoxEnabled.Location = new System.Drawing.Point(11, 28);
            this.textBoxEnabled.Name = "textBoxEnabled";
            this.textBoxEnabled.Size = new System.Drawing.Size(65, 17);
            this.textBoxEnabled.TabIndex = 8;
            this.textBoxEnabled.Text = "Enabled";
            this.textBoxEnabled.UseVisualStyleBackColor = true;
            // 
            // richTextBox
            // 
            this.richTextBox.Location = new System.Drawing.Point(14, 284);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(153, 44);
            this.richTextBox.TabIndex = 9;
            this.richTextBox.Text = "";
            // 
            // comboBox
            // 
            this.comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox.FormattingEnabled = true;
            this.comboBox.Items.AddRange(new object[] {
            "Option 1",
            "Option 2",
            "Option 3",
            "Option 4",
            "Option 5"});
            this.comboBox.Location = new System.Drawing.Point(14, 335);
            this.comboBox.Name = "comboBox";
            this.comboBox.Size = new System.Drawing.Size(153, 21);
            this.comboBox.TabIndex = 10;
            // 
            // groupBox
            // 
            this.groupBox.Location = new System.Drawing.Point(14, 482);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(234, 45);
            this.groupBox.TabIndex = 11;
            this.groupBox.TabStop = false;
            this.groupBox.Text = "Group:";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(14, 533);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(234, 116);
            this.tabControl1.TabIndex = 12;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(226, 90);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(226, 90);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // numericUpDown
            // 
            this.numericUpDown.Location = new System.Drawing.Point(14, 364);
            this.numericUpDown.Name = "numericUpDown";
            this.numericUpDown.Size = new System.Drawing.Size(153, 20);
            this.numericUpDown.TabIndex = 13;
            // 
            // progressBar
            // 
            this.progressBar.Indeterminate = false;
            this.progressBar.Location = new System.Drawing.Point(18, 656);
            this.progressBar.Maximum = 100;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(226, 23);
            this.progressBar.Speed = 1;
            this.progressBar.TabIndex = 14;
            this.progressBar.Value = 17;
            // 
            // vScrollBar
            // 
            this.vScrollBar.Location = new System.Drawing.Point(222, 652);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(17, 68);
            this.vScrollBar.TabIndex = 15;
            // 
            // listView
            // 
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.nameColumn,
            this.valueColumn});
            this.listView.HideSelection = false;
            this.listView.Location = new System.Drawing.Point(14, 390);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(153, 86);
            this.listView.TabIndex = 16;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            // 
            // nameColumn
            // 
            this.nameColumn.Text = "Name";
            // 
            // valueColumn
            // 
            this.valueColumn.Text = "Value";
            // 
            // readOnlyCheckBox
            // 
            this.readOnlyCheckBox.AutoSize = true;
            this.readOnlyCheckBox.Location = new System.Drawing.Point(82, 28);
            this.readOnlyCheckBox.Name = "readOnlyCheckBox";
            this.readOnlyCheckBox.Size = new System.Drawing.Size(71, 17);
            this.readOnlyCheckBox.TabIndex = 17;
            this.readOnlyCheckBox.Text = "Readonly";
            this.readOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // circularProgressBar1
            // 
            this.circularProgressBar1.Indeterminate = false;
            this.circularProgressBar1.LineWidth = 5;
            this.circularProgressBar1.Location = new System.Drawing.Point(18, 686);
            this.circularProgressBar1.Maximum = 100;
            this.circularProgressBar1.MinimumSize = new System.Drawing.Size(10, 10);
            this.circularProgressBar1.Name = "circularProgressBar1";
            this.circularProgressBar1.Size = new System.Drawing.Size(51, 51);
            this.circularProgressBar1.Speed = 1;
            this.circularProgressBar1.TabIndex = 18;
            this.circularProgressBar1.Text = "circularProgressBar";
            this.circularProgressBar1.Value = 88;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton,
            this.toolStripDropDownButton,
            this.toolStripComboBox});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(454, 25);
            this.toolStrip1.TabIndex = 19;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton
            // 
            this.toolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton.Image")));
            this.toolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton.Name = "toolStripButton";
            this.toolStripButton.Size = new System.Drawing.Size(47, 22);
            this.toolStripButton.Text = "Button";
            // 
            // toolStripDropDownButton
            // 
            this.toolStripDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.oneToolStripMenuItem,
            this.twoToolStripMenuItem});
            this.toolStripDropDownButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton.Image")));
            this.toolStripDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton.Name = "toolStripDropDownButton";
            this.toolStripDropDownButton.Size = new System.Drawing.Size(80, 22);
            this.toolStripDropDownButton.Text = "Drop Down";
            // 
            // oneToolStripMenuItem
            // 
            this.oneToolStripMenuItem.Name = "oneToolStripMenuItem";
            this.oneToolStripMenuItem.Size = new System.Drawing.Size(96, 22);
            this.oneToolStripMenuItem.Text = "One";
            // 
            // twoToolStripMenuItem
            // 
            this.twoToolStripMenuItem.Name = "twoToolStripMenuItem";
            this.twoToolStripMenuItem.Size = new System.Drawing.Size(96, 22);
            this.twoToolStripMenuItem.Text = "Two";
            // 
            // toolStripComboBox
            // 
            this.toolStripComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripComboBox.Items.AddRange(new object[] {
            "One",
            "Two",
            "Three"});
            this.toolStripComboBox.Name = "toolStripComboBox";
            this.toolStripComboBox.Size = new System.Drawing.Size(121, 25);
            // 
            // dropDownButton
            // 
            this.dropDownButton.Location = new System.Drawing.Point(18, 730);
            this.dropDownButton.Menu = this.contextMenuStrip1;
            this.dropDownButton.Name = "dropDownButton";
            this.dropDownButton.Size = new System.Drawing.Size(149, 23);
            this.dropDownButton.TabIndex = 20;
            this.dropDownButton.Text = "dropDownButton1";
            this.dropDownButton.UseVisualStyleBackColor = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(108, 26);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(107, 22);
            this.toolStripMenuItem1.Text = "Item 1";
            // 
            // DebugCommonControlsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(454, 777);
            this.Controls.Add(this.dropDownButton);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.circularProgressBar1);
            this.Controls.Add(this.readOnlyCheckBox);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.vScrollBar);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.numericUpDown);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.groupBox);
            this.Controls.Add(this.comboBox);
            this.Controls.Add(this.richTextBox);
            this.Controls.Add(this.textBoxEnabled);
            this.Controls.Add(this.multilineTextBox);
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.radioButton);
            this.Controls.Add(this.checkBox);
            this.Controls.Add(this.linkLabel);
            this.Controls.Add(this.label);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.regularButton);
            this.Name = "DebugCommonControlsView";
            this.Text = "Common Controls";
            this.tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button regularButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.LinkLabel linkLabel;
        private System.Windows.Forms.CheckBox checkBox;
        private System.Windows.Forms.RadioButton radioButton;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.TextBox multilineTextBox;
        private System.Windows.Forms.CheckBox textBoxEnabled;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.ComboBox comboBox;
        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.NumericUpDown numericUpDown;
        private LinearProgressBar progressBar;
        private System.Windows.Forms.VScrollBar vScrollBar;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader nameColumn;
        private System.Windows.Forms.ColumnHeader valueColumn;
        private System.Windows.Forms.CheckBox readOnlyCheckBox;
        private CircularProgressBar circularProgressBar1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem oneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem twoToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox;
        private DropDownButton dropDownButton;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
    }
}