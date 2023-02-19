
namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    partial class DebugCommonControlsView
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
            this.regularButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label = new System.Windows.Forms.Label();
            this.linkLabel = new System.Windows.Forms.LinkLabel();
            this.checkBox = new System.Windows.Forms.CheckBox();
            this.radioButton = new System.Windows.Forms.RadioButton();
            this.textBox = new System.Windows.Forms.TextBox();
            this.multilineTextBox = new System.Windows.Forms.TextBox();
            this.textBoxEnabled = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // regularButton
            // 
            this.regularButton.Location = new System.Drawing.Point(12, 12);
            this.regularButton.Name = "regularButton";
            this.regularButton.Size = new System.Drawing.Size(75, 23);
            this.regularButton.TabIndex = 0;
            this.regularButton.Text = "Regular";
            this.regularButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(93, 12);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(174, 12);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(12, 42);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(33, 13);
            this.label.TabIndex = 3;
            this.label.Text = "Label";
            // 
            // linkLabel
            // 
            this.linkLabel.AutoSize = true;
            this.linkLabel.Location = new System.Drawing.Point(12, 63);
            this.linkLabel.Name = "linkLabel";
            this.linkLabel.Size = new System.Drawing.Size(27, 13);
            this.linkLabel.TabIndex = 4;
            this.linkLabel.TabStop = true;
            this.linkLabel.Text = "Link";
            // 
            // checkBox
            // 
            this.checkBox.AutoSize = true;
            this.checkBox.Location = new System.Drawing.Point(15, 89);
            this.checkBox.Name = "checkBox";
            this.checkBox.Size = new System.Drawing.Size(74, 17);
            this.checkBox.TabIndex = 5;
            this.checkBox.Text = "Checkbox";
            this.checkBox.UseVisualStyleBackColor = true;
            // 
            // radioButton
            // 
            this.radioButton.AutoSize = true;
            this.radioButton.Location = new System.Drawing.Point(15, 112);
            this.radioButton.Name = "radioButton";
            this.radioButton.Size = new System.Drawing.Size(86, 17);
            this.radioButton.TabIndex = 6;
            this.radioButton.TabStop = true;
            this.radioButton.Text = "Radio button";
            this.radioButton.UseVisualStyleBackColor = true;
            // 
            // textBox
            // 
            this.textBox.Location = new System.Drawing.Point(15, 146);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(153, 20);
            this.textBox.TabIndex = 7;
            this.textBox.Text = "Text";
            // 
            // multilineTextBox
            // 
            this.multilineTextBox.Location = new System.Drawing.Point(15, 172);
            this.multilineTextBox.Multiline = true;
            this.multilineTextBox.Name = "multilineTextBox";
            this.multilineTextBox.ReadOnly = true;
            this.multilineTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.multilineTextBox.Size = new System.Drawing.Size(234, 55);
            this.multilineTextBox.TabIndex = 7;
            this.multilineTextBox.Text = "Text 1\r\nText 2\r\nText 3\r\nText 4\r\nText 5\r\nText 6\r\nText 7";
            // 
            // textBoxEnabled
            // 
            this.textBoxEnabled.AutoSize = true;
            this.textBoxEnabled.Location = new System.Drawing.Point(175, 149);
            this.textBoxEnabled.Name = "textBoxEnabled";
            this.textBoxEnabled.Size = new System.Drawing.Size(65, 17);
            this.textBoxEnabled.TabIndex = 8;
            this.textBoxEnabled.Text = "Enabled";
            this.textBoxEnabled.UseVisualStyleBackColor = true;
            // 
            // DebugCommonControlsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.textBoxEnabled);
            this.Controls.Add(this.multilineTextBox);
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.radioButton);
            this.Controls.Add(this.checkBox);
            this.Controls.Add(this.linkLabel);
            this.Controls.Add(this.label);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.regularButton);
            this.Name = "DebugCommonControlsView";
            this.Text = "Common Controls";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button regularButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.LinkLabel linkLabel;
        private System.Windows.Forms.CheckBox checkBox;
        private System.Windows.Forms.RadioButton radioButton;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.TextBox multilineTextBox;
        private System.Windows.Forms.CheckBox textBoxEnabled;
    }
}