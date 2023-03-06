
namespace Google.Solutions.Mvvm.Test.Controls
{
    partial class TestNotificationBarPanel
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
            this.notificationBarPanel = new Google.Solutions.Mvvm.Controls.NotificationBarPanel();
            this.textBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.notificationBarPanel)).BeginInit();
            this.notificationBarPanel.Panel2.SuspendLayout();
            this.notificationBarPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // notificationBarPanel
            // 
            this.notificationBarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.notificationBarPanel.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.notificationBarPanel.IsSplitterFixed = true;
            this.notificationBarPanel.Location = new System.Drawing.Point(0, 0);
            this.notificationBarPanel.Name = "notificationBarPanel";
            this.notificationBarPanel.NotificationBarBackColor = System.Drawing.SystemColors.Control;
            this.notificationBarPanel.NotificationBarForeColor = System.Drawing.SystemColors.InfoText;
            this.notificationBarPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // notificationBarPanel.Panel1
            // 
            this.notificationBarPanel.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.notificationBarPanel.Panel1Collapsed = true;
            // 
            // notificationBarPanel.Panel2
            // 
            this.notificationBarPanel.Panel2.Controls.Add(this.textBox);
            this.notificationBarPanel.Size = new System.Drawing.Size(476, 108);
            this.notificationBarPanel.SplitterDistance = 25;
            this.notificationBarPanel.SplitterWidth = 1;
            this.notificationBarPanel.TabIndex = 0;
            // 
            // textBox
            // 
            this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox.Location = new System.Drawing.Point(23, 23);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(427, 20);
            this.textBox.TabIndex = 0;
            // 
            // TestNotificationBarPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(476, 108);
            this.Controls.Add(this.notificationBarPanel);
            this.Name = "TestNotificationBarPanel";
            this.Text = "TestNotificationBarPanel";
            this.notificationBarPanel.Panel2.ResumeLayout(false);
            this.notificationBarPanel.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.notificationBarPanel)).EndInit();
            this.notificationBarPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Mvvm.Controls.NotificationBarPanel notificationBarPanel;
        private System.Windows.Forms.TextBox textBox;
    }
}