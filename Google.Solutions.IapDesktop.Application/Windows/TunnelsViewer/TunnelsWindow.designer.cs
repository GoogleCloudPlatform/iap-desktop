namespace Google.Solutions.IapDesktop.Application.Windows.TunnelsViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TunnelsWindow));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.terminateToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.tunnelsList = new System.Windows.Forms.ListView();
            this.instanceHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.projectIdHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.zoneHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.localPortHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pidHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.terminateToolStripButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(455, 25);
            this.toolStrip.TabIndex = 5;
            this.toolStrip.Text = "toolStrip";
            // 
            // terminateToolStripButton
            // 
            this.terminateToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.terminateToolStripButton.Enabled = false;
            this.terminateToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("terminateToolStripButton.Image")));
            this.terminateToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.terminateToolStripButton.Name = "terminateToolStripButton";
            this.terminateToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.terminateToolStripButton.Text = "toolStripButton1";
            this.terminateToolStripButton.Click += new System.EventHandler(this.terminateToolStripButton_Click);
            // 
            // tunnelsList
            // 
            this.tunnelsList.BackColor = System.Drawing.SystemColors.Control;
            this.tunnelsList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tunnelsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.instanceHeader,
            this.projectIdHeader,
            this.zoneHeader,
            this.localPortHeader,
            this.pidHeader});
            this.tunnelsList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tunnelsList.FullRowSelect = true;
            this.tunnelsList.GridLines = true;
            this.tunnelsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.tunnelsList.HideSelection = false;
            this.tunnelsList.Location = new System.Drawing.Point(0, 25);
            this.tunnelsList.MultiSelect = false;
            this.tunnelsList.Name = "tunnelsList";
            this.tunnelsList.Size = new System.Drawing.Size(455, 339);
            this.tunnelsList.TabIndex = 6;
            this.tunnelsList.UseCompatibleStateImageBehavior = false;
            this.tunnelsList.View = System.Windows.Forms.View.Details;
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
            // TunnelsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(455, 364);
            this.ControlBox = false;
            this.Controls.Add(this.tunnelsList);
            this.Controls.Add(this.toolStrip);
            this.Name = "TunnelsWindow";
            this.ShowIcon = false;
            this.Text = "Active IAP tunnels";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton terminateToolStripButton;
        private System.Windows.Forms.ListView tunnelsList;
        private System.Windows.Forms.ColumnHeader instanceHeader;
        private System.Windows.Forms.ColumnHeader projectIdHeader;
        private System.Windows.Forms.ColumnHeader zoneHeader;
        private System.Windows.Forms.ColumnHeader localPortHeader;
        private System.Windows.Forms.ColumnHeader pidHeader;
    }
}