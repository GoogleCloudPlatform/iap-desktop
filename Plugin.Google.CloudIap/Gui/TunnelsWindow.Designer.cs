namespace Plugin.Google.CloudIap.Gui
{
    partial class TunnelsWindow
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
            this.closeButton = new System.Windows.Forms.Button();
            this.tunnelsList = new System.Windows.Forms.ListView();
            this.instanceHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.projectIdHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.zoneHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.localPortHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pidHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.terminateTunnelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(362, 325);
            this.closeButton.Margin = new System.Windows.Forms.Padding(2);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(82, 28);
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "OK";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // tunnelsList
            // 
            this.tunnelsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tunnelsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.instanceHeader,
            this.projectIdHeader,
            this.zoneHeader,
            this.localPortHeader,
            this.pidHeader});
            this.tunnelsList.FullRowSelect = true;
            this.tunnelsList.GridLines = true;
            this.tunnelsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.tunnelsList.Location = new System.Drawing.Point(10, 11);
            this.tunnelsList.MultiSelect = false;
            this.tunnelsList.Name = "tunnelsList";
            this.tunnelsList.Size = new System.Drawing.Size(433, 309);
            this.tunnelsList.TabIndex = 4;
            this.tunnelsList.UseCompatibleStateImageBehavior = false;
            this.tunnelsList.View = System.Windows.Forms.View.Details;
            this.tunnelsList.SelectedIndexChanged += new System.EventHandler(this.tunnelsList_SelectedIndexChanged);
            // 
            // instanceHeader
            // 
            this.instanceHeader.Text = "Instance";
            this.instanceHeader.Width = 100;
            // 
            // projectIdHeader
            // 
            this.projectIdHeader.Text = "Project ID";
            this.projectIdHeader.Width = 100;
            // 
            // zoneHeader
            // 
            this.zoneHeader.Text = "Zone";
            this.zoneHeader.Width = 80;
            // 
            // localPortHeader
            // 
            this.localPortHeader.Text = "Local Port";
            this.localPortHeader.Width = 70;
            // 
            // pidHeader
            // 
            this.pidHeader.Text = "Process ID";
            this.pidHeader.Width = 70;
            // 
            // terminateTunnelButton
            // 
            this.terminateTunnelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.terminateTunnelButton.Enabled = false;
            this.terminateTunnelButton.Location = new System.Drawing.Point(11, 325);
            this.terminateTunnelButton.Margin = new System.Windows.Forms.Padding(2);
            this.terminateTunnelButton.Name = "terminateTunnelButton";
            this.terminateTunnelButton.Size = new System.Drawing.Size(82, 28);
            this.terminateTunnelButton.TabIndex = 3;
            this.terminateTunnelButton.Text = "Terminate";
            this.terminateTunnelButton.UseVisualStyleBackColor = true;
            this.terminateTunnelButton.Click += new System.EventHandler(this.terminateTunnelButton_Click);
            // 
            // TunnelsWindow
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(455, 364);
            this.ControlBox = false;
            this.Controls.Add(this.tunnelsList);
            this.Controls.Add(this.terminateTunnelButton);
            this.Controls.Add(this.closeButton);
            this.Name = "TunnelsWindow";
            this.ShowIcon = false;
            this.Text = "Active Cloud IAP tunnels";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ListView tunnelsList;
        private System.Windows.Forms.ColumnHeader pidHeader;
        private System.Windows.Forms.ColumnHeader localPortHeader;
        private System.Windows.Forms.ColumnHeader instanceHeader;
        private System.Windows.Forms.ColumnHeader projectIdHeader;
        private System.Windows.Forms.ColumnHeader zoneHeader;
        private System.Windows.Forms.Button terminateTunnelButton;
    }
}