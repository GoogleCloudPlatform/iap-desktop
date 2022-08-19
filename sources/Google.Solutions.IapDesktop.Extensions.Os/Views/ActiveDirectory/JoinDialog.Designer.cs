
namespace Google.Solutions.IapDesktop.Extensions.Os.Views.ActiveDirectory
{
    partial class JoinDialog
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
            this.headlineLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.domainLabel = new System.Windows.Forms.Label();
            this.domainText = new System.Windows.Forms.TextBox();
            this.computerNameText = new System.Windows.Forms.TextBox();
            this.computerNameLabel = new System.Windows.Forms.Label();
            this.computerNameWarning = new System.Windows.Forms.Label();
            this.domainWarning = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // headlineLabel
            // 
            this.headlineLabel.AutoSize = true;
            this.headlineLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headlineLabel.Location = new System.Drawing.Point(11, 15);
            this.headlineLabel.Name = "headlineLabel";
            this.headlineLabel.Size = new System.Drawing.Size(237, 30);
            this.headlineLabel.TabIndex = 9;
            this.headlineLabel.Text = "Join to Active Directory";
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(86, 232);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(113, 23);
            this.okButton.TabIndex = 10;
            this.okButton.Text = "Restart and join";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(205, 232);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 11;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // domainLabel
            // 
            this.domainLabel.AutoSize = true;
            this.domainLabel.Location = new System.Drawing.Point(13, 69);
            this.domainLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.domainLabel.Name = "domainLabel";
            this.domainLabel.Size = new System.Drawing.Size(165, 13);
            this.domainLabel.TabIndex = 13;
            this.domainLabel.Text = "Active Directory domain to join to:";
            // 
            // domainText
            // 
            this.domainText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.domainText.Location = new System.Drawing.Point(16, 89);
            this.domainText.Margin = new System.Windows.Forms.Padding(2);
            this.domainText.MaxLength = 20;
            this.domainText.Multiline = true;
            this.domainText.Name = "domainText";
            this.domainText.Size = new System.Drawing.Size(254, 24);
            this.domainText.TabIndex = 12;
            // 
            // computerNameText
            // 
            this.computerNameText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.computerNameText.Location = new System.Drawing.Point(16, 168);
            this.computerNameText.Margin = new System.Windows.Forms.Padding(2);
            this.computerNameText.MaxLength = 20;
            this.computerNameText.Multiline = true;
            this.computerNameText.Name = "computerNameText";
            this.computerNameText.Size = new System.Drawing.Size(156, 24);
            this.computerNameText.TabIndex = 12;
            // 
            // computerNameLabel
            // 
            this.computerNameLabel.AutoSize = true;
            this.computerNameLabel.Location = new System.Drawing.Point(13, 151);
            this.computerNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.computerNameLabel.Name = "computerNameLabel";
            this.computerNameLabel.Size = new System.Drawing.Size(84, 13);
            this.computerNameLabel.TabIndex = 18;
            this.computerNameLabel.Text = "Computer name:";
            // 
            // computerNameWarning
            // 
            this.computerNameWarning.AutoSize = true;
            this.computerNameWarning.ForeColor = System.Drawing.Color.Red;
            this.computerNameWarning.Location = new System.Drawing.Point(13, 198);
            this.computerNameWarning.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.computerNameWarning.Name = "computerNameWarning";
            this.computerNameWarning.Size = new System.Drawing.Size(251, 13);
            this.computerNameWarning.TabIndex = 19;
            this.computerNameWarning.Text = "The computer name must not exceed 15 characters";
            // 
            // domainWarning
            // 
            this.domainWarning.AutoSize = true;
            this.domainWarning.ForeColor = System.Drawing.Color.Red;
            this.domainWarning.Location = new System.Drawing.Point(13, 121);
            this.domainWarning.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.domainWarning.Name = "domainWarning";
            this.domainWarning.Size = new System.Drawing.Size(249, 13);
            this.domainWarning.TabIndex = 20;
            this.domainWarning.Text = "Use the DNS domain name, not the NetBIOS name";
            // 
            // JoinDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(300, 275);
            this.ControlBox = false;
            this.Controls.Add(this.domainWarning);
            this.Controls.Add(this.computerNameWarning);
            this.Controls.Add(this.computerNameLabel);
            this.Controls.Add(this.domainLabel);
            this.Controls.Add(this.computerNameText);
            this.Controls.Add(this.domainText);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.headlineLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "JoinDialog";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Join to Active Directory";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label headlineLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label domainLabel;
        private System.Windows.Forms.TextBox domainText;
        private System.Windows.Forms.TextBox computerNameText;
        private System.Windows.Forms.Label computerNameLabel;
        private System.Windows.Forms.Label computerNameWarning;
        private System.Windows.Forms.Label domainWarning;
    }
}