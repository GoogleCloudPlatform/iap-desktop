
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
            this.headerLabel = new Google.Solutions.Mvvm.Controls.HeaderLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.sidebar = new Google.Solutions.Mvvm.Controls.MarkdownViewer();
            this.SuspendLayout();
            // 
            // document
            // 
            this.document.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.document.Location = new System.Drawing.Point(150, 68);
            this.document.Markdown = "";
            this.document.Name = "document";
            this.document.Size = new System.Drawing.Size(500, 370);
            this.document.TabIndex = 0;
            this.document.TabStop = false;
            this.document.TextPadding = ((uint)(10u));
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(11, 15);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(134, 30);
            this.headerLabel.TabIndex = 1;
            this.headerLabel.Text = "What\'s new?";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(13, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(116, 47);
            this.label1.TabIndex = 2;
            this.label1.Text = "Changes since you last upgraded";
            // 
            // sidebar
            // 
            this.sidebar.Location = new System.Drawing.Point(668, 68);
            this.sidebar.Markdown = "If you like IAP Desktop, [give it a star on GitHub](https://github.com/GoogleClou" +
    "dPlatform/iap-desktop)!";
            this.sidebar.Name = "sidebar";
            this.sidebar.Size = new System.Drawing.Size(177, 186);
            this.sidebar.TabIndex = 3;
            this.sidebar.TabStop = false;
            this.sidebar.TextPadding = ((uint)(10u));
            // 
            // ReleaseNotesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(917, 450);
            this.Controls.Add(this.sidebar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.document);
            this.Name = "ReleaseNotesView";
            this.Text = "What\'s new?";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Mvvm.Controls.MarkdownViewer document;
        private Mvvm.Controls.HeaderLabel headerLabel;
        private System.Windows.Forms.Label label1;
        private Mvvm.Controls.MarkdownViewer sidebar;
    }
}