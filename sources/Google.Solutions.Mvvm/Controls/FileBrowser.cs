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

        private IFileSystem fileSystem = null;
        private readonly IDictionary<IFileItem, ObservableCollection<IFileItem>> listFilesCache =
            new Dictionary<IFileItem, ObservableCollection<IFileItem>>();

        internal DirectoryTreeView Directories => this.directoryTree;
        internal FileListView Files => this.fileList;

        private Breadcrumb root;

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

        public event EventHandler<ExceptionEventArgs> NavigationFailed;
        public event EventHandler CurrentDirectoryChanged;

        protected void OnNavigationFailed(Exception e)
            => this.NavigationFailed?.Invoke(this, new ExceptionEventArgs(e));

        protected void OnCurrentDirectoryChanged()
            => this.CurrentDirectoryChanged?.Invoke(this, EventArgs.Empty);

        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        private Breadcrumb navigationState;

        // TODO: Add obvservable property: SelectedFiles
        // TODO: Context menu

        /// <summary>
        /// Directory that is currently being viewed.
        /// </summary>
        public IFileItem CurrentDirectory => this.navigationState.Directory;

        public string CurrentPath => this.navigationState.Path;

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
                Debug.Assert(children != null);

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

            this.root = new Breadcrumb(
                null,
                this.directoryTree.Nodes.Cast<DirectoryTreeView.Node>().First());
            this.navigationState = this.root;

            //
            // Bind file list.
            //
            this.fileList.BindImageIndex(i => GetImageIndex(i.Type));
            this.fileList.BindColumn(0, i => i.Name);
            this.fileList.BindColumn(1, i => i.LastModified.ToString());
            this.fileList.BindColumn(2, i => i.Type.TypeName);
            this.fileList.BindColumn(3, i => ByteSizeFormatter.Format(i.Size));

            this.directoryTree.SelectedModelNodeChanged += async (s, _) => // TODO: Add catch handler to callbacks
            {
                if (this.directoryTree.SelectedNode is DirectoryTreeView.Node node &&
                    node != null)
                {
                    //
                    // The node could be anywhere, so build new breadcrumb path.
                    //
                    Breadcrumb CreatePathTo(DirectoryTreeView.Node n)
                    {
                        return new Breadcrumb(
                            n.Parent == null
                                ? null
                                : CreatePathTo((DirectoryTreeView.Node)n.Parent),
                            n);
                    }

                    this.navigationState = CreatePathTo(node);

                    await BrowseContentsAsync(this.navigationState.Directory).ConfigureAwait(true);
                }
            };
            this.fileList.DoubleClick += async (s, _) =>
            {
                this.CurrentDirectory.IsExpanded = true;
                await BrowseRelativeDirectoryAsync(this.fileList.SelectedModelItem.Name).ConfigureAwait(true);
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
                    this.CurrentDirectory.IsExpanded = true;
                    await BrowseRelativeDirectoryAsync(this.fileList.SelectedModelItem.Name).ConfigureAwait(true);
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

        public async Task BrowseDirectoryAsync(IEnumerable<string> path)
        {
            //
            // Reset to root.
            //
            this.navigationState = this.root;

            if (path == null)
            {
                await BrowseContentsAsync(this.navigationState.Directory).ConfigureAwait(true);
            }
            else
            {
                foreach (var pathItem in path)
                {
                    await BrowseRelativeDirectoryAsync(pathItem).ConfigureAwait(true);
                }
            }
        }

        //public Task BrowseAsync(IFileItem item)
        //{
        //    if (item.Type.IsFile)
        //    {
        //        return Task.CompletedTask;
        //    }

        //    return BrowseAsync(item.Name);
        //}

        public async Task BrowseRelativeDirectoryAsync(string directoryName)
        {
            //
            // Expand the node and wait for it to be populated.
            //
            await this.navigationState.TreeNode
                .ExpandAsync()
                .ConfigureAwait(true);

            //
            // Make it visible.
            //
            this.navigationState.TreeNode.EnsureVisible();

            //
            // Drill down.
            //
            var child = this.navigationState.TreeNode.Nodes
                .Cast<DirectoryTreeView.Node>()
                .FirstOrDefault(n => n.Model.Name == directoryName);

            if (child == null)
            {
                throw new ArgumentException($"The folder '{directoryName}' does not exist");
            }

            this.navigationState = new Breadcrumb(navigationState, child);

            await BrowseContentsAsync(this.navigationState.Directory).ConfigureAwait(true);
        }


        private async Task BrowseContentsAsync(IFileItem folder)
        {
            Debug.Assert(!folder.Type.IsFile);

            //
            // Update list view.
            //
            var files = await ListFilesAsync(folder).ConfigureAwait(true);
            this.fileList.BindCollection(files);

            OnCurrentDirectoryChanged();
        }

        public class Breadcrumb
        {
            public Breadcrumb Parent { get; }
            public IFileItem Directory => this.TreeNode.Model;
            internal DirectoryTreeView.Node TreeNode { get; }

            internal Breadcrumb(
                Breadcrumb parent,
                BindableTreeView<IFileItem>.Node treeNode)
            {
                this.Parent = parent;
                this.TreeNode = treeNode.ThrowIfNull(nameof(treeNode));
            }

            public string Path =>
                (this.Parent?.Path ?? string.Empty) + "/" + this.Directory.Name;
                
        }
    }
}
