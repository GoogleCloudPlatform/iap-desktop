namespace Google.Solutions.IapDesktop.Application.Views.Authentication
{
    partial class UserFlyoutWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserFlyoutWindow));
            this.userIcon = new System.Windows.Forms.PictureBox();
            this.emailHeaderLabel = new System.Windows.Forms.Label();
            this.emailLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.userIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // userIcon
            // 
            this.userIcon.Image = ((System.Drawing.Image)(resources.GetObject("userIcon.Image")));
            this.userIcon.Location = new System.Drawing.Point(12, 12);
            this.userIcon.Name = "userIcon";
            this.userIcon.Size = new System.Drawing.Size(48, 41);
            this.userIcon.TabIndex = 2;
            this.userIcon.TabStop = false;
            // 
            // emailHeaderLabel
            // 
            this.emailHeaderLabel.AutoSize = true;
            this.emailHeaderLabel.Location = new System.Drawing.Point(66, 21);
            this.emailHeaderLabel.Name = "emailHeaderLabel";
            this.emailHeaderLabel.Size = new System.Drawing.Size(106, 13);
            this.emailHeaderLabel.TabIndex = 3;
            this.emailHeaderLabel.Text = "You are signed in as:";
            // 
            // emailLabel
            // 
            this.emailLabel.AutoEllipsis = true;
            this.emailLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.emailLabel.Location = new System.Drawing.Point(66, 35);
            this.emailLabel.Name = "emailLabel";
            this.emailLabel.Size = new System.Drawing.Size(160, 13);
            this.emailLabel.TabIndex = 3;
            this.emailLabel.Text = "foo@example.com";
            // 
            // UserFlyoutWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(242, 68);
            this.Controls.Add(this.emailLabel);
            this.Controls.Add(this.emailHeaderLabel);
            this.Controls.Add(this.userIcon);
            this.Name = "UserFlyoutWindow";
            this.Text = "UserFlyoutWindow";
            this.Controls.SetChildIndex(this.userIcon, 0);
            this.Controls.SetChildIndex(this.emailHeaderLabel, 0);
            this.Controls.SetChildIndex(this.emailLabel, 0);
            ((System.ComponentModel.ISupportInitialize)(this.userIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox userIcon;
        private System.Windows.Forms.Label emailHeaderLabel;
        private System.Windows.Forms.Label emailLabel;
    }
}