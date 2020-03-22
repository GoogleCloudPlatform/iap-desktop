namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    partial class ProjectExplorerWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectExplorerWindow));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.addButton = new System.Windows.Forms.ToolStripButton();
            this.vsToolStripExtender = new WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender(this.components);
            this.vs2015LightTheme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
            this.treeView = new System.Windows.Forms.TreeView();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshAllProjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unloadProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip.SuspendLayout();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshButton,
            this.addButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // refreshButton
            // 
            this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshButton.Image = ((System.Drawing.Image)(resources.GetObject("refreshButton.Image")));
            this.refreshButton.ImageTransparentColor = System.Drawing.Color.White;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(23, 22);
            this.refreshButton.Text = "Refresh";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // addButton
            // 
            this.addButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.addButton.Image = ((System.Drawing.Image)(resources.GetObject("addButton.Image")));
            this.addButton.ImageTransparentColor = System.Drawing.Color.White;
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(23, 22);
            this.addButton.Text = "Add project";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // vsToolStripExtender
            // 
            this.vsToolStripExtender.DefaultRenderer = null;
            // 
            // treeView
            // 
            this.treeView.ContextMenuStrip = this.contextMenu;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageList;
            this.treeView.Location = new System.Drawing.Point(0, 25);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.Size = new System.Drawing.Size(800, 425);
            this.treeView.TabIndex = 1;
            this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
            this.treeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseClick);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.refreshAllProjectsToolStripMenuItem,
            this.unloadProjectToolStripMenuItem,
            this.propertiesToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(181, 114);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.refreshToolStripMenuItem.Text = "&Refresh project";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // refreshAllProjectsToolStripMenuItem
            // 
            this.refreshAllProjectsToolStripMenuItem.Name = "refreshAllProjectsToolStripMenuItem";
            this.refreshAllProjectsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.refreshAllProjectsToolStripMenuItem.Text = "Refresh &all projects";
            this.refreshAllProjectsToolStripMenuItem.Click += new System.EventHandler(this.refreshAllProjectsToolStripMenuItem_Click);
            // 
            // unloadProjectToolStripMenuItem
            // 
            this.unloadProjectToolStripMenuItem.Name = "unloadProjectToolStripMenuItem";
            this.unloadProjectToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.unloadProjectToolStripMenuItem.Text = "&Unload project";
            this.unloadProjectToolStripMenuItem.Click += new System.EventHandler(this.unloadProjectToolStripMenuItem_Click);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "Cloud.ico");
            this.imageList.Images.SetKeyName(1, "Project.ico");
            this.imageList.Images.SetKeyName(2, "Region.ico");
            this.imageList.Images.SetKeyName(3, "Zone.ico");
            this.imageList.Images.SetKeyName(4, "Vm.ico");
            this.imageList.Images.SetKeyName(5, "VmBlue.ico");
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.propertiesToolStripMenuItem.Text = "P&roperties";
            this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.propertiesToolStripMenuItem_Click);
            // 
            // ProjectExplorerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.toolStrip);
            this.Name = "ProjectExplorerWindow";
            this.Text = "Project Explorer";
            this.Shown += new System.EventHandler(this.ProjectExplorerWindow_Shown);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender vsToolStripExtender;
        private WeifenLuo.WinFormsUI.Docking.VS2015LightTheme vs2015LightTheme;
        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.ToolStripButton addButton;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshAllProjectsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unloadProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
    }
}