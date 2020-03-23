namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
    partial class ProjectPickerDialog
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
                this.resourceManager.Dispose();
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
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.panel = new System.Windows.Forms.Panel();
            this.topPane = new System.Windows.Forms.Label();
            this.projectLabel = new System.Windows.Forms.Label();
            this.projectComboBox = new System.Windows.Forms.ComboBox();
            this.projectIcon = new System.Windows.Forms.PictureBox();
            this.panel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.projectIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(237, 136);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(82, 28);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(150, 136);
            this.okButton.Margin = new System.Windows.Forms.Padding(2);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(82, 28);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // panel
            // 
            this.panel.BackColor = System.Drawing.Color.White;
            this.panel.Controls.Add(this.topPane);
            this.panel.Controls.Add(this.projectIcon);
            this.panel.Controls.Add(this.projectLabel);
            this.panel.Controls.Add(this.projectComboBox);
            this.panel.Location = new System.Drawing.Point(-1, -1);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(334, 119);
            this.panel.TabIndex = 1;
            // 
            // topPane
            // 
            this.topPane.AutoSize = true;
            this.topPane.Location = new System.Drawing.Point(64, 14);
            this.topPane.Name = "topPane";
            this.topPane.Size = new System.Drawing.Size(232, 39);
            this.topPane.TabIndex = 10;
            this.topPane.Text = "Type the name of the project to connect to.\r\nAll Windows-based VM instances will " +
    "be added \r\nto the server tree.";
            // 
            // projectLabel
            // 
            this.projectLabel.AutoSize = true;
            this.projectLabel.Location = new System.Drawing.Point(15, 67);
            this.projectLabel.Name = "projectLabel";
            this.projectLabel.Size = new System.Drawing.Size(43, 13);
            this.projectLabel.TabIndex = 8;
            this.projectLabel.Text = "Project:";
            // 
            // projectComboBox
            // 
            this.projectComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.projectComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.projectComboBox.FormattingEnabled = true;
            this.projectComboBox.Location = new System.Drawing.Point(64, 64);
            this.projectComboBox.Name = "projectComboBox";
            this.projectComboBox.Size = new System.Drawing.Size(254, 21);
            this.projectComboBox.TabIndex = 1;
            this.projectComboBox.Text = "Type project name...";
            this.projectComboBox.TextUpdate += new System.EventHandler(this.projectComboBox_TextUpdate);
            // 
            // projectIcon
            // 
            this.projectIcon.Image = global::Google.Solutions.IapDesktop.Application.Properties.Resources.Project_small;
            this.projectIcon.Location = new System.Drawing.Point(18, 14);
            this.projectIcon.Name = "projectIcon";
            this.projectIcon.Size = new System.Drawing.Size(40, 50);
            this.projectIcon.TabIndex = 9;
            this.projectIcon.TabStop = false;
            // 
            // ProjectPickerDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(330, 177);
            this.ControlBox = false;
            this.Controls.Add(this.panel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "ProjectPickerDialog";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add project";
            this.panel.ResumeLayout(false);
            this.panel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.projectIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.Label projectLabel;
        private System.Windows.Forms.ComboBox projectComboBox;
        private System.Windows.Forms.Label topPane;
        private System.Windows.Forms.PictureBox projectIcon;
    }
}