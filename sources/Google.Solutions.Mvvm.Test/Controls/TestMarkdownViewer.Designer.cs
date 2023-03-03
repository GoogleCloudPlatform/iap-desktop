
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
            this.markdown = new Google.Solutions.Mvvm.Controls.MarkdownViewer();
            this.rtf = new System.Windows.Forms.TextBox();
            this.layoutPanel = new System.Windows.Forms.TableLayoutPanel();
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
            this.sourceText.Size = new System.Drawing.Size(391, 835);
            this.sourceText.TabIndex = 0;
            this.sourceText.Text = resources.GetString("sourceText.Text");
            // 
            // markdown
            // 
            this.markdown.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.markdown.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.markdown.Location = new System.Drawing.Point(800, 5);
            this.markdown.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.markdown.Markdown = "";
            this.markdown.Name = "markdown";
            this.markdown.Size = new System.Drawing.Size(398, 831);
            this.markdown.TabIndex = 1;
            // 
            // rtf
            // 
            this.rtf.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtf.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtf.Location = new System.Drawing.Point(400, 3);
            this.rtf.Multiline = true;
            this.rtf.Name = "rtf";
            this.rtf.ReadOnly = true;
            this.rtf.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.rtf.Size = new System.Drawing.Size(391, 835);
            this.rtf.TabIndex = 2;
            // 
            // layoutPanel
            // 
            this.layoutPanel.ColumnCount = 3;
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.layoutPanel.Controls.Add(this.sourceText, 0, 0);
            this.layoutPanel.Controls.Add(this.markdown, 2, 0);
            this.layoutPanel.Controls.Add(this.rtf, 1, 0);
            this.layoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutPanel.Location = new System.Drawing.Point(0, 0);
            this.layoutPanel.Name = "layoutPanel";
            this.layoutPanel.RowCount = 1;
            this.layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutPanel.Size = new System.Drawing.Size(1204, 841);
            this.layoutPanel.TabIndex = 3;
            // 
            // TestMarkdownViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1204, 841);
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
    }
}