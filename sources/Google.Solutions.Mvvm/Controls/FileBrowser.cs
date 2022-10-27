using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Mvvm.Shell.Util;
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

        private IFileItem currentFolder;

        private IFileSystem fileSystem = null;
        private readonly IDictionary<IFileItem, ObservableCollection<IFileItem>> listFilesCache =
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
        public EventHandler CurrentFolderChanged;

        protected void OnNavigationFailed(Exception e)
            => this.NavigationFailed?.Invoke(this, new ExceptionEventArgs(e));

        protected void OnCurrentFolderChanged()
            => this.CurrentFolderChanged?.Invoke(this, EventArgs.Empty);

        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        // TODO: Add obvservable property: SelectedFiles
        // TODO: Context menu

        /// <summary>
        /// Folder that is currently being viewed.
        /// </summary>
        public IFileItem CurrentFolder
        {
            get => this.currentFolder;
            private set
            {
                this.currentFolder = value;
                OnCurrentFolderChanged();
            }
        }

        //---------------------------------------------------------------------
        // Data Binding.
        //---------------------------------------------------------------------

        private async Task<ObservableCollection<IFileItem>> ListFilesAsync(IFileItem folder)
        {
            Debug.Assert(this.fileSystem != null);

            //
            // NB. Both controls must use the same file items so that expansion
            // tracking works correctly. Instead of bining each control individually,
            // we therefore put a cache in between.
            //
            if (!this.listFilesCache.TryGetValue(folder, out var children))
            {
                children = await this.fileSystem
                    .ListFilesAsync(folder)
                    .ConfigureAwait(true);
                this.listFilesCache[folder] = children;
            }

            return children;
        }

        public void Bind(IFileSystem fileSystem)
        {
            fileSystem.ThrowIfNull(nameof(fileSystem));

            if (this.fileSystem != null)
            {
                throw new InvalidOperationException("Control is already bound");
            }

            if (fileSystem.Root.Type.IsFile)
            {
                throw new ArgumentException("The root item must be a folder");
            }

            this.fileSystem = fileSystem;

            //
            // Bind directory tree.
            //
            this.directoryTree.BindIsLeaf(i => i.Type.IsFile);
            this.directoryTree.BindText(i => i.Name);
            this.directoryTree.BindIsExpanded(i => i.IsExpanded);

            // TODO: Project to filter out files
            //  change treeview to use ICollection and test for INotify*Changed

            this.directoryTree.BindChildren(async item =>
            {
                var files = await ListFilesAsync(item).ConfigureAwait(true);

                return new FilteredObservableCollection<IFileItem>(files)
                {
                    Predicate = f => !f.Type.IsFile
                };
            });
            this.directoryTree.Bind(this.fileSystem.Root);
            this.directoryTree.BindImageIndex(i => GetImageIndex(i.Type), true);
            this.directoryTree.BindSelectedImageIndex(i => GetImageIndex(i.Type), true);

            this.CurrentFolder = this.fileSystem.Root;

            //
            // Bind file list.
            //
            this.fileList.BindImageIndex(i => GetImageIndex(i.Type));
            this.fileList.BindColumn(0, i => i.Name);
            this.fileList.BindColumn(1, i => i.LastModified.ToString());
            this.fileList.BindColumn(2, i => i.Type.TypeName);
            this.fileList.BindColumn(3, i => ByteSizeFormatter.Format(i.Size));

            this.directoryTree.SelectedModelNodeChanged += async (s, _) =>
            {
                if (this.directoryTree.SelectedModelNode != null)
                {
                    await OpenFolder(this.directoryTree.SelectedModelNode).ConfigureAwait(true);
                }
            };
            this.fileList.DoubleClick += async (s, _) =>
            {
                this.CurrentFolder.IsExpanded = true;
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
                    this.CurrentFolder.IsExpanded = true;
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
                if (this.directoryTree.SelectedModelNode == this.CurrentFolder)
                {
                    var treeNode = ((BindableTreeView<IFileItem>.Node)this.directoryTree.SelectedNode)
                        .FindTreeNodeByModelNode(folder);
                    if (treeNode != null)
                    {
                        this.directoryTree.SelectedNode = treeNode;
                    }
                }

                this.CurrentFolder = folder;
            }
            catch (Exception e)
            {
                OnNavigationFailed(e);
            }
        }
    }
}
