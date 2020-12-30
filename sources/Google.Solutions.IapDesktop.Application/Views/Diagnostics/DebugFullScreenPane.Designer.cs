namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    partial class DebugFullScreenPane
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
            this.fullScreenToggleButton = new System.Windows.Forms.Button();
            this.allScreensCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // fullScreenToggleButton
            // 
            this.fullScreenToggleButton.Location = new System.Drawing.Point(12, 12);
            this.fullScreenToggleButton.Name = "fullScreenToggleButton";
            this.fullScreenToggleButton.Size = new System.Drawing.Size(116, 23);
            this.fullScreenToggleButton.TabIndex = 0;
            this.fullScreenToggleButton.Text = "Toggle full screen";
            this.fullScreenToggleButton.UseVisualStyleBackColor = true;
            this.fullScreenToggleButton.Click += new System.EventHandler(this.fullScreenToggleButton_Click);
            // 
            // allScreensCheckBox
            // 
            this.allScreensCheckBox.AutoSize = true;
            this.allScreensCheckBox.Location = new System.Drawing.Point(134, 16);
            this.allScreensCheckBox.Name = "allScreensCheckBox";
            this.allScreensCheckBox.Size = new System.Drawing.Size(98, 17);
            this.allScreensCheckBox.TabIndex = 1;
            this.allScreensCheckBox.Text = "Use all screens";
            this.allScreensCheckBox.UseVisualStyleBackColor = true;
            // 
            // DebugFullScreenPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.allScreensCheckBox);
            this.Controls.Add(this.fullScreenToggleButton);
            this.Name = "DebugFullScreenPane";
            this.Text = "DebugFullScreenPane";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button fullScreenToggleButton;
        private System.Windows.Forms.CheckBox allScreensCheckBox;
    }
}