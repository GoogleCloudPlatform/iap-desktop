using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser : UserControl
    {
        private readonly FileTypeCache fileTypeCache = new FileTypeCache();

        private Func<IFileItem, Task<ObservableCollection<IFileItem>>> listFilesFunc;
        private IDictionary<IFileItem, ObservableCollection<IFileItem>> listFilesCache =
            new Dictionary<IFileItem, ObservableCollection<IFileItem>>();

        //---------------------------------------------------------------------
        // Privates.
        //---------------------------------------------------------------------

        private int GetImageIndex(FileType fileType)
        {
            var imageIndex = this.fileIconsList.Images.IndexOfKey(fileType.TypeName);
            if (imageIndex == -1)
            {
                //
                // Image not added to list yet.
                //
                this.fileIconsList.Images.Add(fileType.TypeName, fileType.FileIcon);
                imageIndex = this.fileIconsList.Images.IndexOfKey(fileType.TypeName);

                Debug.Assert(imageIndex != -1);
            }

            return imageIndex;
        }

        public FileBrowser()
        {
            InitializeComponent();

            this.Disposed += (s, e) =>
            {
                this.fileTypeCache.Dispose();
            };
        }


        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        public EventHandler<ExceptionEventArgs> NavigationFailed;

        protected void OnNavigationFailed(Exception e)
            => this.NavigationFailed?.Invoke(this, new ExceptionEventArgs(e));

        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        //public IEnumerable<IFileItem> SelectedItems => this.fileList.SelectedModelItems;
        //public IFileItem SelectedItem => this.fileList.SelectedModelItem;

        /// <summary>
        /// Folder that is being viewed.
        /// </summary>
        public IFileItem Folder { get; private set; }

        //---------------------------------------------------------------------
        // Data Binding.
        //---------------------------------------------------------------------

        private async Task<ObservableCollection<IFileItem>> ListFilesAsync(IFileItem folder)
        {
            // TODO: Merge into Navigate
            Debug.Assert(this.listFilesFunc != null);

            //
            // NB. Both controls must use the same file items so that expansion
            // tracking works correctly. Instead of bining each control individually,
            // we therefore put a cache in between.
            //
            if (!this.listFilesCache.TryGetValue(folder, out var children))
            {
                children = await this.listFilesFunc(folder).ConfigureAwait(true);
                this.listFilesCache[folder] = children;
            }

            return children;
        }

        public void Bind(
            IFileItem root,
            Func<IFileItem, Task<ObservableCollection<IFileItem>>> listFiles)
        {
            root.ThrowIfNull(nameof(root));
            listFiles.ThrowIfNull(nameof(listFiles));

            if (this.listFilesFunc != null)
            {
                throw new InvalidOperationException("Control is already bound");
            }

            if (root.Type.IsFile)
            {
                throw new ArgumentException("The root item must be a folder");
            }

            this.listFilesFunc = listFiles;

            //
            // Bind directory tree.
            //
            this.directoryTree.BindIsLeaf(i => i.Type.IsFile);
            this.directoryTree.BindText(i => i.Name);
            this.directoryTree.BindIsExpanded(i => i.IsExpanded);

            // TODO: Project to filter out files
            this.directoryTree.BindChildren(ListFilesAsync);
            this.directoryTree.Bind(root);

            // TODO: Change to accept Func<> also
            //
            //this.directoryTree.BindImageIndex(i => i.ImageIndex);
            //this.directoryTree.BindSelectedImageIndex(i => i.SelectedImageIndex);

            this.Folder = root;

            //
            // Bind file list.
            //
            this.fileList.BindImageIndex(i => GetImageIndex(i.Type));
            this.fileList.BindColumn(0, i => i.Name);
            this.fileList.BindColumn(1, i => i.LastModified.ToString()); // TODO: Formatting?
            this.fileList.BindColumn(2, i => i.Type.TypeName);
            this.fileList.BindColumn(3, i => i.Size.ToString()); // TODO: Use ByteSizeFormatter

            this.directoryTree.SelectedModelNodeChanged += async (s, _) =>
            {
                await OpenFolder(this.directoryTree.SelectedModelNode).ConfigureAwait(true);
            };
            this.fileList.DoubleClick += async (s, _) =>
            {
                this.Folder.IsExpanded = true;
                await OpenFolder(this.fileList.SelectedModelItem).ConfigureAwait(true);
            };
            this.fileList.KeyDown += async (s, args) =>
            {
                if (args.KeyCode == Keys.Enter &&
                    this.fileList.SelectedModelItem is var item &&
                    item != null &&
                    !item.Type.IsFile)
                {
                    //
                    // Go down one level.
                    //
                    this.Folder.IsExpanded = true;
                    await OpenFolder(this.fileList.SelectedModelItem).ConfigureAwait(true);
                }
                else if (args.KeyCode == Keys.Up && args.Alt &&
                    this.directoryTree.SelectedNode?.Parent != null)
                {
                    //
                    // Go up one level.
                    //
                    this.directoryTree.SelectedNode = this.directoryTree.SelectedNode.Parent;
                }
            };

            this.directoryTree.LoadingChildrenFailed += (s, args) => OnNavigationFailed(args.Exception);
        }

        private async Task OpenFolder(IFileItem folder)
        {
            if (folder.Type.IsFile)
            {
                return;
            }

            try
            {

                var files = await ListFilesAsync(folder).ConfigureAwait(true);

                this.fileList.BindCollection(files);

                //
                // Try to select folder in tree. This only works if the nodes
                // have been loaded already, so it's on a best-effort basis.
                //
                if (this.directoryTree.SelectedModelNode == this.Folder)
                {
                    var treeNode = ((BindableTreeView<IFileItem>.Node)this.directoryTree.SelectedNode)
                        .FindTreeNodeByModelNode(folder);
                    if (treeNode != null)
                    {
                        this.directoryTree.SelectedNode = treeNode;
                    }
                }

                this.Folder = folder;
            }
            catch (Exception e)
            {
                OnNavigationFailed(e);
            }
        }
    }
}
