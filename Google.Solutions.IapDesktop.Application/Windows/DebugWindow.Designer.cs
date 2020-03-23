namespace Google.Solutions.IapDesktop.Application.Windows
{
    partial class DebugWindow
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
            this.slowOpButton = new System.Windows.Forms.Button();
            this.spinner = new System.Windows.Forms.PictureBox();
            this.slowNonCanelOpButton = new System.Windows.Forms.Button();
            this.throwExceptionButton = new System.Windows.Forms.Button();
            this.label = new System.Windows.Forms.Label();
            this.reauthButton = new System.Windows.Forms.Button();
            this.rdpGroup = new System.Windows.Forms.GroupBox();
            this.serverLabel = new System.Windows.Forms.Label();
            this.serverTextBox = new System.Windows.Forms.TextBox();
            this.connectButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).BeginInit();
            this.rdpGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // slowOpButton
            // 
            this.slowOpButton.Location = new System.Drawing.Point(22, 24);
            this.slowOpButton.Name = "slowOpButton";
            this.slowOpButton.Size = new System.Drawing.Size(172, 23);
            this.slowOpButton.TabIndex = 0;
            this.slowOpButton.Text = "Fire slow canellable event";
            this.slowOpButton.UseVisualStyleBackColor = true;
            this.slowOpButton.Click += new System.EventHandler(this.slowOpButton_Click);
            // 
            // spinner
            // 
            this.spinner.BackColor = System.Drawing.Color.White;
            this.spinner.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Spinner;
            this.spinner.Location = new System.Drawing.Point(209, 24);
            this.spinner.Name = "spinner";
            this.spinner.Size = new System.Drawing.Size(44, 44);
            this.spinner.TabIndex = 3;
            this.spinner.TabStop = false;
            this.spinner.Visible = false;
            // 
            // slowNonCanelOpButton
            // 
            this.slowNonCanelOpButton.Location = new System.Drawing.Point(22, 53);
            this.slowNonCanelOpButton.Name = "slowNonCanelOpButton";
            this.slowNonCanelOpButton.Size = new System.Drawing.Size(172, 23);
            this.slowNonCanelOpButton.TabIndex = 0;
            this.slowNonCanelOpButton.Text = "Fire slow non-canellable event";
            this.slowNonCanelOpButton.UseVisualStyleBackColor = true;
            this.slowNonCanelOpButton.Click += new System.EventHandler(this.slowNonCanelOpButton_Click);
            // 
            // throwExceptionButton
            // 
            this.throwExceptionButton.Location = new System.Drawing.Point(22, 82);
            this.throwExceptionButton.Name = "throwExceptionButton";
            this.throwExceptionButton.Size = new System.Drawing.Size(172, 23);
            this.throwExceptionButton.TabIndex = 0;
            this.throwExceptionButton.Text = "Throw exception";
            this.throwExceptionButton.UseVisualStyleBackColor = true;
            this.throwExceptionButton.Click += new System.EventHandler(this.throwExceptionButton_Click);
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(19, 9);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(19, 13);
            this.label.TabIndex = 4;
            this.label.Text = " ...";
            // 
            // reauthButton
            // 
            this.reauthButton.Location = new System.Drawing.Point(22, 111);
            this.reauthButton.Name = "reauthButton";
            this.reauthButton.Size = new System.Drawing.Size(172, 23);
            this.reauthButton.TabIndex = 0;
            this.reauthButton.Text = "Trigger reauth";
            this.reauthButton.UseVisualStyleBackColor = true;
            this.reauthButton.Click += new System.EventHandler(this.reauthButton_Click);
            // 
            // rdpGroup
            // 
            this.rdpGroup.Controls.Add(this.serverLabel);
            this.rdpGroup.Controls.Add(this.serverTextBox);
            this.rdpGroup.Controls.Add(this.connectButton);
            this.rdpGroup.Location = new System.Drawing.Point(22, 164);
            this.rdpGroup.Name = "rdpGroup";
            this.rdpGroup.Size = new System.Drawing.Size(200, 100);
            this.rdpGroup.TabIndex = 5;
            this.rdpGroup.TabStop = false;
            this.rdpGroup.Text = "RDP";
            // 
            // serverLabel
            // 
            this.serverLabel.AutoSize = true;
            this.serverLabel.Location = new System.Drawing.Point(7, 37);
            this.serverLabel.Name = "serverLabel";
            this.serverLabel.Size = new System.Drawing.Size(38, 13);
            this.serverLabel.TabIndex = 2;
            this.serverLabel.Text = "Server";
            // 
            // serverTextBox
            // 
            this.serverTextBox.Location = new System.Drawing.Point(51, 37);
            this.serverTextBox.Name = "serverTextBox";
            this.serverTextBox.Size = new System.Drawing.Size(100, 20);
            this.serverTextBox.TabIndex = 1;
            this.serverTextBox.Text = "localhost:13389";
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(72, 63);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 23);
            this.connectButton.TabIndex = 0;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
            // 
            // DebugWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(541, 451);
            this.Controls.Add(this.rdpGroup);
            this.Controls.Add(this.label);
            this.Controls.Add(this.spinner);
            this.Controls.Add(this.reauthButton);
            this.Controls.Add(this.throwExceptionButton);
            this.Controls.Add(this.slowNonCanelOpButton);
            this.Controls.Add(this.slowOpButton);
            this.Name = "DebugWindow";
            this.Text = "Debug";
            ((System.ComponentModel.ISupportInitialize)(this.spinner)).EndInit();
            this.rdpGroup.ResumeLayout(false);
            this.rdpGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button slowOpButton;
        private System.Windows.Forms.PictureBox spinner;
        private System.Windows.Forms.Button slowNonCanelOpButton;
        private System.Windows.Forms.Button throwExceptionButton;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Button reauthButton;
        private System.Windows.Forms.GroupBox rdpGroup;
        private System.Windows.Forms.Label serverLabel;
        private System.Windows.Forms.TextBox serverTextBox;
        private System.Windows.Forms.Button connectButton;
    }
}