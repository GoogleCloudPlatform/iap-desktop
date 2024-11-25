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

using Google.Solutions.Common.IO;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Format;
using Google.Solutions.Mvvm.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable VSTHRD100 // Avoid async void methods

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser : DpiAwareUserControl
    {
        private readonly FileTypeCache fileTypeCache = new FileTypeCache();

        private IFileSystem? fileSystem = null;
        private readonly Dictionary<IFileItem, ObservableCollection<IFileItem>> listFilesCache =
            new Dictionary<IFileItem, ObservableCollection<IFileItem>>();

        internal DirectoryTreeView Directories => this.directoryTree;
        internal FileListView Files => this.fileList;

        private Breadcrumb? root;
        private Breadcrumb? navigationState;

        private IList<IFileItem>? currentDirectoryContents;

        internal ITaskDialog TaskDialog { get; set; } = new TaskDialog();
        internal IOperationProgressDialog ProgressDialog { get; set; } = new OperationProgressDialog();

        /// <summary>
        /// Buffer size for stream copy operations.
        /// </summary>
        public int StreamCopyBufferSize { get; set; } = StreamExtensions.DefaultBufferSize;

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

            //
            // NB. fileIconsList.Images.IndexOfKey doesn't work reliably in
            //     high-DPI mode, so we use .Images.Keys.IndexOf instead.
            // 
            var imageIndex = this.fileIconsList.Images.Keys.IndexOf(fileType.TypeName);
            if (imageIndex == -1)
            {
                //
                // Image not added to list yet.
                //
                this.fileIconsList.Images.Add(fileType.TypeName, fileType.FileIcon);
                imageIndex = this.fileIconsList.Images.Keys.IndexOf(fileType.TypeName);

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
                this.fileSystem?.Dispose();
            };
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        public event EventHandler<ExceptionEventArgs>? NavigationFailed;
        public event EventHandler<ExceptionEventArgs>? FileCopyFailed;
        public event EventHandler? CurrentDirectoryChanged;
        public event EventHandler? SelectedFilesChanged;

        protected void OnNavigationFailed(Exception e)
        {
            this.NavigationFailed?.Invoke(this, new ExceptionEventArgs(e));
        }

        protected void OnFileCopyFailed(Exception e)
        {
            this.FileCopyFailed?.Invoke(this, new ExceptionEventArgs(e));
        }

        protected void OnCurrentDirectoryChanged()
        {
            this.CurrentDirectoryChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void OnSelectedFilesChanged()
        {
            this.SelectedFilesChanged?.Invoke(this, EventArgs.Empty);
        }

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

        private async Task<ObservableCollection<IFileItem>> ListFilesAsync(IFileItem directory)
        {
            Debug.Assert(!this.InvokeRequired, "Running on UI thread");
            Debug.Assert(this.fileSystem != null);

            if (this.fileSystem == null)
            {
                throw new InvalidOperationException("Control is not bound");
            }

            //
            // NB. Both controls must use the same file items so that expansion
            // tracking works correctly. Instead of binding each control individually,
            // we therefore put a cache in between.
            //
            if (!this.listFilesCache.TryGetValue(directory, out var children))
            {
                children = await this.fileSystem
                    .ListFilesAsync(directory)
                    .ConfigureAwait(true);
                Debug.Assert(children != null);

                this.listFilesCache[directory] = children!;
            }

            return children!;
        }

        private async Task RebindToNavigationStateAsync()
        {
            Debug.Assert(this.navigationState != null);

            var directory = this.navigationState!.Directory;
            Debug.Assert(!directory.Type.IsFile);

            //
            // Update list view.
            //
            var files = await ListFilesAsync(directory).ConfigureAwait(true);
            this.fileList.BindCollection(files);

            //
            // Update tree view.
            //
            this.directoryTree.SelectedNode = this.navigationState.TreeNode;

            this.currentDirectoryContents = files;
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
                throw new ArgumentException("The root item must be a directory");
            }

            this.fileSystem = fileSystem;

            //
            // The first icon in the image list is used for the "Loading..."
            // dummy node in the directory tree. Use the Find icon for that.
            //
            this.fileIconsList.Images.Add(
                StockIcons.GetIcon(StockIcons.IconId.Find,
                StockIcons.IconSize.Small));

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
            this.fileList.BindColumn(3, i => i.Access);
            this.fileList.BindColumn(4, i => ByteSizeFormatter.Format(i.Size));

            this.directoryTree.LoadingChildrenFailed += (s, args) => OnNavigationFailed(args.Exception);
            this.fileList.ItemSelectionChanged += (s, args) => OnSelectedFilesChanged();
        }

        //---------------------------------------------------------------------
        // Event handlers - navigation.
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
                    await RebindToNavigationStateAsync().ConfigureAwait(true);
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
            try
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
                    args.Handled = true;
                }
                else if (args.KeyCode == Keys.C && args.Control)
                {
                    //
                    // Copy files.
                    //
                    copyToolStripMenuItem_Click(sender, EventArgs.Empty);
                    args.Handled = true;
                }
                else if (args.KeyCode == Keys.V && args.Control)
                {
                    //
                    // Paste files.
                    //
                    pasteToolStripMenuItem_Click(sender, EventArgs.Empty);
                    args.Handled = true;
                }
                else if (args.KeyCode == Keys.Up && args.Alt)
                {
                    //
                    // Go up one level.
                    //
                    await NavigateUpAsync();
                    args.Handled = true;
                }
                else if (args.KeyCode == Keys.F5)
                {
                    await RefreshAsync();
                    args.Handled = true;
                }
            }
            catch (Exception e)
            {
                OnNavigationFailed(e);
            }
        }

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs args)
        {
            try
            {
                await RefreshAsync().ConfigureAwait(true);
            }
            catch (Exception e)
            {
                OnNavigationFailed(e);
            }
        }

        //---------------------------------------------------------------------
        // Event handlers - copy/drag.
        //---------------------------------------------------------------------

        private void copyToolStripMenuItem_Click(object sender, EventArgs args)
        {
            try
            {
                Clipboard.SetDataObject(
                    CopySelectedFiles(),
                    false);
            }
            catch (Exception e)
            {
                OnFileCopyFailed(e);
            }
        }

        private void fileList_MouseMove(object sender, MouseEventArgs args)
        {
            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            try
            {
                var dataObject = CopySelectedFiles();
                if (dataObject.Files.Any())
                {
                    //
                    // NB. Only begin a drag operation when there's actually
                    //     a file in the data object, otherwise the drop target
                    //     will indicate that there's something to drop, even
                    //     though there isn't.
                    //
                    DoDragDrop(dataObject, DragDropEffects.Copy);
                }
            }
            catch (Exception e)
            {
                OnFileCopyFailed(e);
            }
        }

        //---------------------------------------------------------------------
        // Event handlers - paste/drop.
        //---------------------------------------------------------------------

        private async void pasteToolStripMenuItem_Click(object sender, EventArgs args)
        {
            try
            {
                await PasteFilesAsync(Clipboard.GetDataObject()).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                OnFileCopyFailed(e);
            }
        }

        private void fileList_DragEnter(object sender, DragEventArgs args)
        {
            args.Effect = GetPastableFiles(args.Data, false).Any()
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private async void fileList_DragDrop(object sender, DragEventArgs args)
        {
            try
            {
                await PasteFilesAsync(args.Data).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                OnFileCopyFailed(e);
            }
        }

        //---------------------------------------------------------------------
        // Clipboard handling.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create an IDataObject with the contents of the selected files.
        /// </summary>
        [SuppressMessage("Usage",
            "VSTHRD002:Avoid problematic synchronous waits",
            Justification = "Blocking calls made from worker thread")]
        internal VirtualFileDataObject CopySelectedFiles()
        {
            Precondition.ExpectNotNull(this.fileSystem, nameof(this.fileSystem));

            //
            // Only consider files, ignore directories.
            //
            var files = this.SelectedFiles.Where(f => f.Type.IsFile);

            var dataObject = new VirtualFileDataObject(files
                .Select(f => new VirtualFileDataObject.Descriptor(
                    f.Name,
                    f.Size,
                    f.Attributes,
                    () =>
                    {
                        //
                        // NB. We're in a synchronous execution path here,
                        //     so we can't open the file asynchronously.
                        //
                        //     However, this isn't a problem because this
                        //     is an asynchronous data object, so the call
                        //     should come from a worker thread, not the UI
                        //     thread.
                        //
                        return this.fileSystem!
                            .OpenFileAsync(f, FileAccess.Read)
                            .Result;
                    }))
                .ToList())
            {
                //
                // Don't block the UI thread when the target reads
                // the data.
                //
                IsAsync = true
            };

            dataObject.AsyncOperationFailed += (_, args) =>
            {
                Invoke(new Action(() => OnFileCopyFailed(args.Exception)));
            };

            return dataObject;
        }

        internal static IEnumerable<FileInfo> GetPastableFiles(
            IDataObject dataObject)
        {
            //
            // Extract file paths, ignore directory paths.
            //
            return (dataObject.GetData(DataFormats.FileDrop) as IEnumerable<string>)
                .EnsureNotNull()
                .Where(path => File.Exists(path))
                .Select(path => new FileInfo(path));
        }

        /// <summary>
        /// Inspect the data object and extract the list of files
        /// that can be pasted to the current directory.
        /// </summary>
        internal IEnumerable<FileInfo> GetPastableFiles(
            IDataObject dataObject,
            bool promptForConflicts)
        {
            //
            // Extract file paths, ignore directory paths.
            //
            var files = GetPastableFiles(dataObject);

            if (!promptForConflicts)
            {
                return files;
            }

            //
            // Allow user to exclude files that would otherwise be overwritten.
            //
            Debug.Assert(this.currentDirectoryContents != null);
            Debug.Assert(this.currentDirectoryContents!.Count == this.fileList.Items.Count);

            var result = new List<FileInfo>();
            foreach (var file in files)
            {
                var conflictingItem = this.currentDirectoryContents
                    .FirstOrDefault(f => f.Name == file.Name);

                var dialogResult = DialogResult.OK;
                if (conflictingItem != null && !conflictingItem.Type.IsFile)
                {
                    //
                    // There is an existing directory with the same name
                    // as the file to be dropped.
                    //

                    var parameters = new TaskDialogParameters(
                        "Copy files",
                        $"The destination already has a directory named '{conflictingItem.Name}'",
                        string.Empty)
                    {
                        Icon = TaskDialogIcon.Error
                    };

                    parameters.Buttons.Add(new TaskDialogCommandLinkButton(
                        "Skip this file",
                        DialogResult.Ignore));
                    parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

                    dialogResult = this.TaskDialog.ShowDialog(this, parameters);
                }
                else if (conflictingItem != null)
                {
                    //
                    // There is an existing file with the same name
                    // as the file to be dropped.
                    //

                    var parameters = new TaskDialogParameters(
                        "Copy files",
                        $"The destination already has a file named '{conflictingItem.Name}'",
                        string.Empty)
                    {
                        Icon = TaskDialogIcon.Warning
                    };

                    parameters.Buttons.Add(new TaskDialogCommandLinkButton(
                        "Replace the file in the destination",
                        DialogResult.OK));
                    parameters.Buttons.Add(new TaskDialogCommandLinkButton(
                        "Skip this file",
                        DialogResult.Ignore));
                    parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

                    dialogResult = this.TaskDialog.ShowDialog(this, parameters);
                }

                switch (dialogResult)
                {
                    case DialogResult.OK:
                        result.Add(file);
                        break;

                    case DialogResult.Ignore:
                        //
                        // Skip this file.
                        //
                        break;

                    case DialogResult.Cancel:
                        return Array.Empty<FileInfo>();
                }
            }

            return result;
        }

        /// <summary>
        /// Check of the data object contains pastable files.
        /// </summary>
        public static bool CanPaste(IDataObject dataObject)
        {
            return GetPastableFiles(dataObject).Any();
        }

        /// <summary>
        /// Paste files to current directory.
        /// </summary>
        internal async Task PasteFilesAsync(IDataObject dataObject)
        {
            Precondition.ExpectNotNull(this.fileSystem, nameof(this.fileSystem));
            Precondition.ExpectNotNull(this.navigationState, nameof(this.navigationState));

            Debug.Assert(this.currentDirectoryContents != null);
            Debug.Assert(this.currentDirectoryContents!.Count == this.fileList.Items.Count);

            var filesToCopy = GetPastableFiles(dataObject, true);
            if (!filesToCopy.Any())
            {
                return;
            }

            //
            // Show a progress dialog to track overall progress.
            //
            using (var progressDialog = this.ProgressDialog.StartCopyOperation(
                this,
                (ulong)filesToCopy.Count(),
                (ulong)filesToCopy.Sum(f => f.Length)))
            {
                var copyProgress = new Progress<int>(
                    delta => progressDialog.OnBytesCompleted((ulong)delta));

                foreach (var file in filesToCopy)
                {
                    if (progressDialog.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        //
                        // Perform the file copy in the background.
                        //
                        // NB. If the underlying file system session is closed, 
                        //     we expect I/O operations to be cancelled.
                        //
                        await Task
                            .Run(async () =>
                                {
                                    using (var sourceStream = file.OpenRead())
                                    using (var targetStream = await this.fileSystem!
                                        .OpenFileAsync(
                                            this.navigationState!.Directory,
                                            file.Name,
                                            FileMode.Create,
                                            FileAccess.Write)
                                        .ConfigureAwait(false))
                                    {
                                        await sourceStream
                                            .CopyToAsync(
                                                targetStream,
                                                copyProgress,
                                                this.StreamCopyBufferSize,
                                                progressDialog.CancellationToken)
                                            .ConfigureAwait(false);
                                    }
                                })
                            .ConfigureAwait(true);
                    }
                    catch (Exception e) when (!e.IsCancellation())
                    {
                        var parameters = new TaskDialogParameters(
                            "Copy files",
                            $"Unable to copy {file.Name}",
                            e.Unwrap().Message)
                        {
                            Icon = TaskDialogIcon.Error
                        };

                        parameters.Buttons.Add(new TaskDialogCommandLinkButton(
                            "Skip this file",
                            DialogResult.Ignore));
                        parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

                        if (this.TaskDialog.ShowDialog(this, parameters) == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        progressDialog.OnItemCompleted();
                    }
                }
            }

            //
            // Refresh as there might be some new files now.
            //
            await RefreshAsync().ConfigureAwait(true);
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
                await RebindToNavigationStateAsync().ConfigureAwait(true);
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
                throw new ArgumentException($"The directory '{directoryName}' does not exist");
            }

            this.navigationState = new Breadcrumb(this.navigationState, child);
            await RebindToNavigationStateAsync().ConfigureAwait(true);
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

            this.navigationState = this.navigationState.Parent;
            await RebindToNavigationStateAsync().ConfigureAwait(true);
        }

        public async Task RefreshAsync()
        {
            if (this.navigationState != null)
            {
                //
                // Clear cache and reload.
                //
                this.listFilesCache.Remove(this.navigationState.Directory);
                await RebindToNavigationStateAsync().ConfigureAwait(true);

                //
                // Force tree view to reload nodes in case a child
                // directory was added.
                //
                if (this.directoryTree.SelectedNode
                    is BindableTreeView<IFileItem>.Node node)
                {
                    node.Reload();
                }
            }
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
            {
                get => this.Parent == null
                    ? Enumerable.Empty<string>()
                    : this.Parent.Path.ConcatItem(this.Directory.Name);
            }
        }
    }
}