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

namespace Google.Solutions.Mvvm.Test.Controls
{
    partial class TestMarkdownViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestMarkdownViewer));
            this.sourceText = new System.Windows.Forms.TextBox();
            this.rtf = new System.Windows.Forms.TextBox();
            this.layoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.markdown = new Google.Solutions.Mvvm.Controls.MarkdownViewer();
            this.parsedMarkdown = new System.Windows.Forms.TextBox();
            this.layoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // sourceText
            // 
            this.sourceText.AcceptsReturn = true;
            this.sourceText.AcceptsTab = true;
            this.sourceText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceText.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sourceText.Location = new System.Drawing.Point(3, 3);
            this.sourceText.Multiline = true;
            this.sourceText.Name = "sourceText";
            this.sourceText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.sourceText.Size = new System.Drawing.Size(394, 835);
            this.sourceText.TabIndex = 0;
            this.sourceText.Text = resources.GetString("sourceText.Text");
            // 
            // rtf
            // 
            this.rtf.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtf.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtf.Location = new System.Drawing.Point(803, 3);
            this.rtf.Multiline = true;
            this.rtf.Name = "rtf";
            this.rtf.ReadOnly = true;
            this.rtf.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.rtf.Size = new System.Drawing.Size(394, 835);
            this.rtf.TabIndex = 2;
            // 
            // layoutPanel
            // 
            this.layoutPanel.ColumnCount = 4;
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.Controls.Add(this.sourceText, 0, 0);
            this.layoutPanel.Controls.Add(this.markdown, 3, 0);
            this.layoutPanel.Controls.Add(this.rtf, 2, 0);
            this.layoutPanel.Controls.Add(this.parsedMarkdown, 1, 0);
            this.layoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutPanel.Location = new System.Drawing.Point(0, 0);
            this.layoutPanel.Name = "layoutPanel";
            this.layoutPanel.RowCount = 1;
            this.layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutPanel.Size = new System.Drawing.Size(1601, 841);
            this.layoutPanel.TabIndex = 3;
            // 
            // markdown
            // 
            this.markdown.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.markdown.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.markdown.Location = new System.Drawing.Point(1206, 5);
            this.markdown.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.markdown.Markdown = "";
            this.markdown.Name = "markdown";
            this.markdown.Size = new System.Drawing.Size(389, 831);
            this.markdown.TabIndex = 1;
            // 
            // parsedMarkdown
            // 
            this.parsedMarkdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.parsedMarkdown.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.parsedMarkdown.Location = new System.Drawing.Point(403, 3);
            this.parsedMarkdown.Multiline = true;
            this.parsedMarkdown.Name = "parsedMarkdown";
            this.parsedMarkdown.ReadOnly = true;
            this.parsedMarkdown.Size = new System.Drawing.Size(394, 835);
            this.parsedMarkdown.TabIndex = 3;
            // 
            // TestMarkdownViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1601, 841);
            this.Controls.Add(this.layoutPanel);
            this.Name = "TestMarkdownViewer";
            this.Text = "TestMarkdownViewer";
            this.layoutPanel.ResumeLayout(false);
            this.layoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox sourceText;
        private Mvvm.Controls.MarkdownViewer markdown;
        private System.Windows.Forms.TextBox rtf;
        private System.Windows.Forms.TableLayoutPanel layoutPanel;
        private System.Windows.Forms.TextBox parsedMarkdown;
    }
}