namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    partial class SelectIssuerView
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
            this.headerLabel = new Google.Solutions.Mvvm.Controls.HeaderLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.gaiaRadioButton = new System.Windows.Forms.RadioButton();
            this.workforceIdentityRadioButton = new System.Windows.Forms.RadioButton();
            this.wifLocationLabel = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.wifPoolLabel = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.wifProviderLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerLabel
            // 
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(16, 16);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(163, 30);
            this.headerLabel.TabIndex = 0;
            this.headerLabel.Text = "Sign-in method";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox3);
            this.groupBox1.Controls.Add(this.wifProviderLabel);
            this.groupBox1.Controls.Add(this.textBox2);
            this.groupBox1.Controls.Add(this.wifPoolLabel);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.wifLocationLabel);
            this.groupBox1.Controls.Add(this.workforceIdentityRadioButton);
            this.groupBox1.Controls.Add(this.gaiaRadioButton);
            this.groupBox1.Location = new System.Drawing.Point(20, 60);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(265, 177);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Identity provider:";
            // 
            // gaiaRadioButton
            // 
            this.gaiaRadioButton.AutoSize = true;
            this.gaiaRadioButton.Location = new System.Drawing.Point(16, 20);
            this.gaiaRadioButton.Name = "gaiaRadioButton";
            this.gaiaRadioButton.Size = new System.Drawing.Size(174, 17);
            this.gaiaRadioButton.TabIndex = 0;
            this.gaiaRadioButton.TabStop = true;
            this.gaiaRadioButton.Text = "Sign in with my Google account";
            this.gaiaRadioButton.UseVisualStyleBackColor = true;
            // 
            // workforceIdentityRadioButton
            // 
            this.workforceIdentityRadioButton.AutoSize = true;
            this.workforceIdentityRadioButton.Location = new System.Drawing.Point(16, 53);
            this.workforceIdentityRadioButton.Name = "workforceIdentityRadioButton";
            this.workforceIdentityRadioButton.Size = new System.Drawing.Size(165, 17);
            this.workforceIdentityRadioButton.TabIndex = 1;
            this.workforceIdentityRadioButton.TabStop = true;
            this.workforceIdentityRadioButton.Text = "Sign in with workforce identity";
            this.workforceIdentityRadioButton.UseVisualStyleBackColor = true;
            // 
            // wifLocationLabel
            // 
            this.wifLocationLabel.AutoSize = true;
            this.wifLocationLabel.Location = new System.Drawing.Point(33, 83);
            this.wifLocationLabel.Name = "wifLocationLabel";
            this.wifLocationLabel.Size = new System.Drawing.Size(51, 13);
            this.wifLocationLabel.TabIndex = 2;
            this.wifLocationLabel.Text = "Location:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(100, 80);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(140, 20);
            this.textBox1.TabIndex = 3;
            this.textBox1.Text = "global";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(100, 106);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(140, 20);
            this.textBox2.TabIndex = 5;
            // 
            // wifPoolLabel
            // 
            this.wifPoolLabel.AutoSize = true;
            this.wifPoolLabel.Location = new System.Drawing.Point(33, 109);
            this.wifPoolLabel.Name = "wifPoolLabel";
            this.wifPoolLabel.Size = new System.Drawing.Size(45, 13);
            this.wifPoolLabel.TabIndex = 4;
            this.wifPoolLabel.Text = "Pool ID:";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(100, 132);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(140, 20);
            this.textBox3.TabIndex = 7;
            // 
            // wifProviderLabel
            // 
            this.wifProviderLabel.AutoSize = true;
            this.wifProviderLabel.Location = new System.Drawing.Point(33, 135);
            this.wifProviderLabel.Name = "wifProviderLabel";
            this.wifProviderLabel.Size = new System.Drawing.Size(63, 13);
            this.wifProviderLabel.TabIndex = 6;
            this.wifProviderLabel.Text = "Provider ID:";
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(129, 254);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 13;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(210, 254);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 14;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // SelectIssuerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(304, 293);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.headerLabel);
            this.Name = "SelectIssuerView";
            this.Text = "SelectIssuerView";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Mvvm.Controls.HeaderLabel headerLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton gaiaRadioButton;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label wifProviderLabel;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label wifPoolLabel;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label wifLocationLabel;
        private System.Windows.Forms.RadioButton workforceIdentityRadioButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}