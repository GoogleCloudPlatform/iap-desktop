
namespace Google.Solutions.Mvvm.Controls
{
    partial class FileBrowser
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ColumnHeader nameColumn;
            System.Windows.Forms.ColumnHeader lastModifiedColumn;
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.directoryTree = new Google.Solutions.Mvvm.Controls.FileBrowser.DirectoryTreeView();
            this.fileList = new Google.Solutions.Mvvm.Controls.FileBrowser.FileListView();
            this.typeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.sizeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.fileIconsList = new System.Windows.Forms.ImageList(this.components);
            nameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            lastModifiedColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // nameColumn
            // 
            nameColumn.Text = "Name";
            nameColumn.Width = 150;
            // 
            // lastModifiedColumn
            // 
            lastModifiedColumn.Text = "Date Modified";
            lastModifiedColumn.Width = 100;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.MinimumSize = new System.Drawing.Size(400, 200);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.directoryTree);
            this.splitContainer.Panel1MinSize = 200;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.fileList);
            this.splitContainer.Panel2MinSize = 200;
            this.splitContainer.Size = new System.Drawing.Size(592, 256);
            this.splitContainer.SplitterDistance = 200;
            this.splitContainer.SplitterWidth = 2;
            this.splitContainer.TabIndex = 0;
            // 
            // directoryTree
            // 
            this.directoryTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.directoryTree.HideSelection = false;
            this.directoryTree.Location = new System.Drawing.Point(0, 0);
            this.directoryTree.Name = "directoryTree";
            this.directoryTree.Size = new System.Drawing.Size(200, 256);
            this.directoryTree.TabIndex = 0;
            // 
            // fileList
            // 
            this.fileList.AutoResizeColumnsOnUpdate = false;
            this.fileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            nameColumn,
            lastModifiedColumn,
            this.typeColumn,
            this.sizeColumn});
            this.fileList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileList.FullRowSelect = true;
            this.fileList.HideSelection = false;
            this.fileList.Location = new System.Drawing.Point(0, 0);
            this.fileList.Name = "fileList";
            this.fileList.OwnerDraw = true;
            this.fileList.SelectedModelItem = null;
            this.fileList.Size = new System.Drawing.Size(390, 256);
            this.fileList.TabIndex = 0;
            this.fileList.UseCompatibleStateImageBehavior = false;
            this.fileList.View = System.Windows.Forms.View.Details;
            this.fileList.DoubleClick += new System.EventHandler(this.fileList_DoubleClick);
            // 
            // typeColumn
            // 
            this.typeColumn.Text = "Type";
            this.typeColumn.Width = 100;
            // 
            // sizeColumn
            // 
            this.sizeColumn.Text = "Size";
            this.sizeColumn.Width = 32;
            // 
            // fileIconsList
            // 
            this.fileIconsList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.fileIconsList.ImageSize = new System.Drawing.Size(16, 16);
            this.fileIconsList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // FileBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer);
            this.Name = "FileBrowser";
            this.Size = new System.Drawing.Size(592, 256);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private DirectoryTreeView directoryTree;
        private FileListView fileList;
        private System.Windows.Forms.ImageList fileIconsList;
        private System.Windows.Forms.ColumnHeader typeColumn;
        private System.Windows.Forms.ColumnHeader sizeColumn;

        internal class FileListView : BindableListView<IFileItem>
        { }

        internal class DirectoryTreeView : BindableTreeView<IFileItem>
        { }
    }
}
