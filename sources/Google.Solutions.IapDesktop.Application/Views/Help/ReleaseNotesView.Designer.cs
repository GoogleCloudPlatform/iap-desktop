
namespace Google.Solutions.IapDesktop.Application.Views.Help
{
    partial class ReleaseNotesView
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
            this.document = new Google.Solutions.Mvvm.Controls.MarkdownViewer();
            this.SuspendLayout();
            // 
            // document
            // 
            this.document.Dock = System.Windows.Forms.DockStyle.Fill;
            this.document.Location = new System.Drawing.Point(0, 0);
            this.document.Markdown = "";
            this.document.Name = "document";
            this.document.Size = new System.Drawing.Size(800, 450);
            this.document.TabIndex = 0;
            // 
            // ReleaseNotesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.document);
            this.Name = "ReleaseNotesView";
            this.Text = "ReleaseNotesView";
            this.ResumeLayout(false);

        }

        #endregion

        private Mvvm.Controls.MarkdownViewer document;
    }
}