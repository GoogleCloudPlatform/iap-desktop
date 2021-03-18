namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    partial class SshOptionsControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SshOptionsControl));
            this.connectionBox = new System.Windows.Forms.GroupBox();
            this.keyboardIcon = new System.Windows.Forms.PictureBox();
            this.propagateLocaleCheckBox = new System.Windows.Forms.CheckBox();
            this.connectionBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keyboardIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // connectionBox
            // 
            this.connectionBox.Controls.Add(this.propagateLocaleCheckBox);
            this.connectionBox.Controls.Add(this.keyboardIcon);
            this.connectionBox.Location = new System.Drawing.Point(4, 3);
            this.connectionBox.Name = "connectionBox";
            this.connectionBox.Size = new System.Drawing.Size(336, 85);
            this.connectionBox.TabIndex = 0;
            this.connectionBox.TabStop = false;
            this.connectionBox.Text = "Connection:";
            // 
            // keyboardIcon
            // 
            this.keyboardIcon.Image = ((System.Drawing.Image)(resources.GetObject("keyboardIcon.Image")));
            this.keyboardIcon.Location = new System.Drawing.Point(10, 21);
            this.keyboardIcon.Name = "keyboardIcon";
            this.keyboardIcon.Size = new System.Drawing.Size(36, 36);
            this.keyboardIcon.TabIndex = 3;
            this.keyboardIcon.TabStop = false;
            // 
            // propagateLocaleCheckBox
            // 
            this.propagateLocaleCheckBox.AutoSize = true;
            this.propagateLocaleCheckBox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.propagateLocaleCheckBox.Location = new System.Drawing.Point(58, 22);
            this.propagateLocaleCheckBox.Name = "propagateLocaleCheckBox";
            this.propagateLocaleCheckBox.Size = new System.Drawing.Size(266, 17);
            this.propagateLocaleCheckBox.TabIndex = 4;
            this.propagateLocaleCheckBox.Text = "Use Windows display language as locale (LC_ALL)";
            this.propagateLocaleCheckBox.UseVisualStyleBackColor = true;
            // 
            // SshOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.connectionBox);
            this.Name = "SshOptionsControl";
            this.Size = new System.Drawing.Size(343, 369);
            this.connectionBox.ResumeLayout(false);
            this.connectionBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keyboardIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox connectionBox;
        private System.Windows.Forms.PictureBox keyboardIcon;
        private System.Windows.Forms.CheckBox propagateLocaleCheckBox;
    }
}
