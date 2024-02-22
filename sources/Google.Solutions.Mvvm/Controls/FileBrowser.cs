//
// Copyright 2022 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Format;
using Google.Solutions.Mvvm.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser : UserControl
    {
        private readonly FileTypeCache fileTypeCache = new FileTypeCache();

        private IFileSystem? fileSystem = null;
        private readonly IDictionary<IFileItem, ObservableCollection<IFileItem>> listFilesCache =
            new Dictionary<IFileItem, ObservableCollection<IFileItem>>();

        internal DirectoryTreeView Directories => this.directoryTree;
        internal FileListView Files => this.fileList;

        private Breadcrumb? root;
        private Breadcrumb? navigationState;

        //---------------------------------------------------------------------
        // Privates.
        //---------------------------------------------------------------------

        private int GetImageIndex(FileType fileType)
        {
            Debug.Assert(!this.InvokeRequired, "Running on UI thread");

            if (this.IsDisposed)
            {
                //
                // Don't touch the image list if we're already disposing.
                //
                return 0;
            }

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

            //
            // The first icon in the image list is used for the "Loading..."
            // dummy node in the directory tree. Use the Find icon for that.
            //
            this.fileIconsList.Images.Add(
                StockIcons.GetIcon(StockIcons.IconId.Find,
                StockIcons.IconSize.Small));

            this.Disposed += (s, e) =>
            {
                this.fileTypeCache.Dispose();
            };
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        public event EventHandler<ExceptionEventArgs>? NavigationFailed;
        public event EventHandler? CurrentDirectoryChanged;
        public event EventHandler? SelectedFilesChanged;

        protected void OnNavigationFailed(Exception e)
            => this.NavigationFailed?.Invoke(this, new ExceptionEventArgs(e));

        protected void OnCurrentDirectoryChanged()
            => this.CurrentDirectoryChanged?.Invoke(this, EventArgs.Empty);
        protected void OnSelectedFilesChanged()
            => this.SelectedFilesChanged?.Invoke(this, EventArgs.Empty);

        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        public IEnumerable<IFileItem> SelectedFiles
        {
            get => this.fileList.SelectedModelItems;
            set => this.fileList.SelectedModelItems = value;
        }

        /// <summary>
        /// Directory that is currently being viewed.
        /// </summary>
        public IFileItem? CurrentDirectory
        {
            get => this.navigationState?.Directory;
        }

        public string? CurrentPath
        {
            get
            {
                if (this.navigationState == null)
                {
                    return null;
                }
                else
                {
                    return string.Join(
                        this.directoryTree.PathSeparator,
                        this.navigationState.Path);
                }
            }
        }

        //---------------------------------------------------------------------
        // Data Binding.
        //---------------------------------------------------------------------

        private async Task<ObservableCollection<IFileItem>> ListFilesAsync(IFileItem folder)
        {
            Debug.Assert(!this.InvokeRequired, "Running on UI thread");
            Debug.Assert(this.fileSystem != null);

            if (this.fileSystem == null)
            {
                throw new InvalidOperationException("Control is not bound");
            }

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

                this.listFilesCache[folder] = children!;
            }

            return children!;
        }

        private async Task ShowDirectoryContentsAsync(IFileItem folder)
        {
            Debug.Assert(!folder.Type.IsFile);

            //
            // Update list view.
            //
            var files = await ListFilesAsync(folder).ConfigureAwait(true);
            this.fileList.BindCollection(files);

            OnCurrentDirectoryChanged();
        }

        public void Bind(IFileSystem fileSystem, IBindingContext bindingContext)
        {
            fileSystem.ExpectNotNull(nameof(fileSystem));

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
            this.directoryTree.BindImageIndex(i => GetImageIndex(i.Type), true);
            this.directoryTree.BindSelectedImageIndex(i => GetImageIndex(i.Type), true);
            this.directoryTree.Bind(this.fileSystem.Root, bindingContext);

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

            this.directoryTree.LoadingChildrenFailed += (s, args) => OnNavigationFailed(args.Exception);
            this.fileList.ItemSelectionChanged += (s, args) => OnSelectedFilesChanged();
        }

        //---------------------------------------------------------------------
        // Event handlers.
        //---------------------------------------------------------------------

        private async void directoryTree_SelectedModelNodeChanged(object sender, EventArgs args)
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

                try
                {
                    await ShowDirectoryContentsAsync(this.navigationState.Directory)
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    OnNavigationFailed(e);
                }
            }
        }

        private async void fileList_DoubleClick(object sender, EventArgs args)
        {
            if (this.root == null || this.CurrentDirectory == null)
            {
                throw new InvalidOperationException("Control is not bound");
            }

            if (this.fileList.SelectedModelItem == null ||
                this.fileList.SelectedModelItem.Type.IsFile)
            {
                return;
            }

            try
            {
                await NavigateDownAsync(this.fileList.SelectedModelItem.Name)
                    .ConfigureAwait(true);

                this.CurrentDirectory.IsExpanded = true;
            }
            catch (Exception e)
            {
                OnNavigationFailed(e);
            }
        }

        private async void fileList_KeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter &&
                this.fileList.SelectedModelItem is var item &&
                item != null &&
                !item.Type.IsFile)
            {
                //
                // Go down one level, same as double-click.
                //
                fileList_DoubleClick(sender, EventArgs.Empty);
            }
            else if (args.KeyCode == Keys.Up && args.Alt)
            {
                //
                // Go up one level.
                //
                try
                {
                    await NavigateUpAsync();
                }
                catch (Exception e)
                {
                    OnNavigationFailed(e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public async Task NavigateAsync(IEnumerable<string> path)
        {
            Debug.Assert(!this.InvokeRequired, "Running on UI thread");

            if (this.root == null || this.navigationState == null)
            {
                throw new InvalidOperationException("Control is not bound");
            }

            //
            // Reset to root.
            //
            this.navigationState = this.root;

            if (path == null || !path.Any())
            {
                await ShowDirectoryContentsAsync(this.navigationState.Directory).ConfigureAwait(true);
            }
            else
            {
                foreach (var pathItem in path)
                {
                    await NavigateDownAsync(pathItem).ConfigureAwait(true);
                }
            }
        }

        public async Task NavigateDownAsync(string directoryName)
        {
            Debug.Assert(!this.InvokeRequired, "Running on UI thread");
            if (this.root == null ||
                this.navigationState == null)
            {
                throw new InvalidOperationException("Control is not bound");
            }

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

            this.navigationState = new Breadcrumb(this.navigationState, child);

            await ShowDirectoryContentsAsync(this.navigationState.Directory).ConfigureAwait(true);
        }

        public async Task NavigateUpAsync()
        {
            Debug.Assert(!this.InvokeRequired, "Running on UI thread");

            if (this.root == null || this.navigationState == null)
            {
                throw new InvalidOperationException("Control is not bound");
            }

            if (this.navigationState.Parent == null)
            {
                //
                // Already at root level.
                //
                return;
            }

            await NavigateAsync(this.navigationState.Parent.Path)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public class Breadcrumb
        {
            public Breadcrumb? Parent { get; }
            public IFileItem Directory => this.TreeNode.Model;
            internal DirectoryTreeView.Node TreeNode { get; }

            internal Breadcrumb(
                Breadcrumb? parent,
                BindableTreeView<IFileItem>.Node treeNode)
            {
                this.Parent = parent;
                this.TreeNode = treeNode.ExpectNotNull(nameof(treeNode));
            }

            public IEnumerable<string> Path
                => this.Parent == null
                    ? Enumerable.Empty<string>()
                    : this.Parent.Path.ConcatItem(this.Directory.Name);
        }
    }
}
