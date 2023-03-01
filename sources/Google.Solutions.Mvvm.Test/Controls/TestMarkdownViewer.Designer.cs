
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
            this.sourceText.Location = new System.Drawing.Point(1, 0);
            this.sourceText.Multiline = true;
            this.sourceText.Name = "sourceText";
            this.sourceText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.sourceText.Size = new System.Drawing.Size(395, 645);
            this.sourceText.TabIndex = 0;
            this.sourceText.Text = resources.GetString("sourceText.Text");
            // 
            // markdown
            // 
            this.markdown.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.markdown.Location = new System.Drawing.Point(808, 0);
            this.markdown.Markdown = "";
            this.markdown.Name = "markdown";
            this.markdown.Size = new System.Drawing.Size(384, 645);
            this.markdown.TabIndex = 1;
            // 
            // rtf
            // 
            this.rtf.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtf.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtf.Location = new System.Drawing.Point(402, 0);
            this.rtf.Multiline = true;
            this.rtf.Name = "rtf";
            this.rtf.ReadOnly = true;
            this.rtf.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.rtf.Size = new System.Drawing.Size(400, 645);
            this.rtf.TabIndex = 2;
            // 
            // TestMarkdownViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1204, 645);
            this.Controls.Add(this.rtf);
            this.Controls.Add(this.markdown);
            this.Controls.Add(this.sourceText);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TestMarkdownViewer";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "TestMarkdownViewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox sourceText;
        private Mvvm.Controls.MarkdownViewer markdown;
        private System.Windows.Forms.TextBox rtf;
    }
}