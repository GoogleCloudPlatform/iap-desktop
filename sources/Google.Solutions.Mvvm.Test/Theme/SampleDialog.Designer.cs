namespace Google.Solutions.Mvvm.Test.Theme
{
    partial class SampleDialog
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
            this.anchoredBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.fixedBox = new System.Windows.Forms.GroupBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.leftAlignedLabel = new System.Windows.Forms.Label();
            this.rightAlignedLabel = new System.Windows.Forms.Label();
            this.anchoredBox.SuspendLayout();
            this.fixedBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // anchoredBox
            // 
            this.anchoredBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.anchoredBox.Controls.Add(this.fixedBox);
            this.anchoredBox.Location = new System.Drawing.Point(12, 12);
            this.anchoredBox.Name = "anchoredBox";
            this.anchoredBox.Size = new System.Drawing.Size(776, 365);
            this.anchoredBox.TabIndex = 0;
            this.anchoredBox.TabStop = false;
            this.anchoredBox.Text = "Anchored box";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Label";
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(713, 383);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point(632, 383);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(54, 20);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(127, 20);
            this.textBox1.TabIndex = 1;
            // 
            // fixedBox
            // 
            this.fixedBox.Controls.Add(this.checkBox1);
            this.fixedBox.Controls.Add(this.label1);
            this.fixedBox.Controls.Add(this.textBox1);
            this.fixedBox.Location = new System.Drawing.Point(12, 24);
            this.fixedBox.Name = "fixedBox";
            this.fixedBox.Size = new System.Drawing.Size(200, 100);
            this.fixedBox.TabIndex = 2;
            this.fixedBox.TabStop = false;
            this.fixedBox.Text = "FixedBox";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(54, 47);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(80, 17);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "checkBox1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // leftAlignedLabel
            // 
            this.leftAlignedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.leftAlignedLabel.AutoSize = true;
            this.leftAlignedLabel.Location = new System.Drawing.Point(710, 425);
            this.leftAlignedLabel.Name = "leftAlignedLabel";
            this.leftAlignedLabel.Size = new System.Drawing.Size(82, 13);
            this.leftAlignedLabel.TabIndex = 3;
            this.leftAlignedLabel.Text = "Left-aligned text";
            this.leftAlignedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rightAlignedLabel
            // 
            this.rightAlignedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.rightAlignedLabel.AutoSize = true;
            this.rightAlignedLabel.Location = new System.Drawing.Point(12, 425);
            this.rightAlignedLabel.Name = "rightAlignedLabel";
            this.rightAlignedLabel.Size = new System.Drawing.Size(89, 13);
            this.rightAlignedLabel.TabIndex = 4;
            this.rightAlignedLabel.Text = "Right-aligned text";
            // 
            // TestDpiAwarenessRuleset
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.rightAlignedLabel);
            this.Controls.Add(this.leftAlignedLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.anchoredBox);
            this.Name = "TestDpiAwarenessRuleset";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "TestDpiAwarenessRuleset";
            this.anchoredBox.ResumeLayout(false);
            this.fixedBox.ResumeLayout(false);
            this.fixedBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox anchoredBox;
        private System.Windows.Forms.GroupBox fixedBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label leftAlignedLabel;
        private System.Windows.Forms.Label rightAlignedLabel;
    }
}